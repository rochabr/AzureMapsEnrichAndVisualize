# Enrich your location data with Azure Maps

There are many situations where you have a collection of addresses but they are not really useful unless you can know exactly what and where they represent. Enriching those addresses with the ability to be understood geographically opens up infinite use cases to leverage that data for proximity calculation and visualization experiences that can not be accomplished with addresses alone. The following steps show how you might easily prepare a set of address data for such scenarios.

### Pre-requisites

To build this solution you will need:

1. An Azure Subscription (sign up [here](https://azure.com/free) for free)
2. An [Azure Maps](https://azure.com/maps) account
3. [Visual Studio Code](https://code.visualstudio.com/) and [Git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git) installed on your local machine

### Create a resource group

As a best practice, and to facilitate tearing down the environment when you are done, use these [instructions](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups) to create a Resource Group in your Azure Subscription.

### Create an Azure Maps account

Create a new Azure Maps account with the following steps:

1. Select **Create a resource** in the upper left-hand corner of the [Azure portal](https://portal.azure.com).
2. Type **Azure Maps** in the *Search services and Marketplace* box.
3. Select **Azure Maps** in the drop-down list that appears, then select the **Create** button.
4. On the **Create an Azure Maps Account resource** page, enter the following values then select the **Create** button:
    * The *Subscription* that you want to use for this account.
    * The *Resource group* name for this account. You may choose to *Create new* or *Select existing* resource group.
    * The *Name* of your new Azure Maps account.
    * The *Pricing tier* for this account. Select **Gen2**.
    * Read the *License* and *Privacy Statement*, then select the checkbox to accept the terms.

    ![azure-maps-account](https://user-images.githubusercontent.com/1051195/221387170-fa6888a0-c8ad-4f26-b6d6-dc420bdea64f.png)

<a id="getkey"></a>

### Create the database

1. Follow these [instructions](https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart?view=azuresql&tabs=azure-portal), to create an Azure SQL Database Server and a database, you can use any name you want to create your resources. In this tutorial we will call the database _azuremapsdb_.

2. When all of the resources have been provisioned, in the Azure Portal navigate to your database server and update the network configurations to allow public network access from selected networks, add your IP address to the firewall rules as an allowed inbound traffic rule for local testing and select the box at the end to allow Azure Services to access the resource. We need this setting to allow access from our Azure Functions to our database.

   ![networking_config_db](https://user-images.githubusercontent.com/1051195/214764219-c2f837ca-ffc3-47b1-8fda-ea75bf184914.png)

3. Next use the query editor to populate the database with the following commands:

   ```sql
   use azuremapsdb;

   GO

   drop table Locations

   GO 
   create table Locations(
    id int identity not null primary key,
    street_number varchar(100) not null,
    street_name varchar(100) not null,
    details  varchar(100),
    city varchar(100) not null,
    province varchar(2) not null,
    postal_code varchar(10) not null,
    country_code varchar(2) not null,
    latitude Decimal(8,6),
    longitude Decimal(9,6))

   GO 

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('81', 'Bay St', 'Suite 4400','Toronto', 'ON', 'M5J 0E7', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('6795', 'Marconi Street', 'Suite 401', 'Montreal', 'QC', 'H2S 3J9', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('2000', 'Avenue McGill College', 'Suite 1400', 'Montreal', 'QC', 'H3A 3H3', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('100', 'Queen Street', 'Suite 500', 'Ottawa', 'ON', 'K1P 1J9', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('360', 'Main Street', 'Suite 1150', 'Winnipeg', 'MB', 'R3C 3Z3', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('110', '9th Avenue SW', '7th Floor Suite 710', 'Calgary', 'AB', 'T2P 0T1', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('10155', '102 Street', 'Suite 2100 Commerce Place', 'Edmonton', 'AB', 'T5J 4G8', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('155', 'Water Street', '7th Floor', 'Vancouver', 'BC', 'V6B 5C6', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('725', 'Granville Street', 'Suite 700', 'Vancouver', 'BC', 'V7Y 1G5', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('858', 'Beatty Street', '6th Floor', 'Vancouver', 'BC', 'V6B 1C1', 'CA');

   insert into Locations(street_number, street_name, details, city, province, postal_code, country_code)
   values ('375', 'Water Street', 'Suite 710', 'Vancouver', 'BC', 'V6B 5C6', 'CA');

   GO

   select * from Locations;

  ```
  The script above creates a table called _Locations_ and populates it with address parameters. You might notice that our sample is populating the database with public addresses for Microsoft offices in Canada. Note that we are keeping the _latitude_ and _longitude_ parameters empty, on purpose.

### Enrich the database and query for addresses

To fill those parameters we will create two functions: 

1. **_EnrichDatabase_** queries every address in the database that has an a\empty geolocation, makes a _Search API_ call to Azure Maps and populates the database with the latitude and longitude gathered from the response. We will use this first to enrich our address database.
2. **_GetLocations_** simply queries for all enriched adddresses in the database. We will use this function in our front-end web application.

#### Create the Function Apps

Our sample functions are coded in C# to take advantage of [Azure Functions SQL Extensions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-azure-sql), which are easier to use in C# than in other coding languages. Should you wish to use another language, you can use them in Java, JavaScript, Powershell and Python. Overall, they expedite the development time to build the connectivity between the Azure Function and the Azure SQL database by creating a simpler process to generate inputs, outputs and triggers.

1. Install [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)

2. Using Visual Studio Code, create two function apps for .NET by following [this guide](https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-csharp?tabs=in-process). We can test this locally but feel free to publish them to your Azure Subscription. Create one called _EnrichDatabase_ and another called _GetLocations_. 

3. Clone [this repository](TODO: add link to repository) containing the source code for both functions and the Azure Maps handler.

#### Build and configure the connections to the SQL Server

To create our connection we need to enable SQL bindings on the function app as follows: 

1. Install the extension:

   ```powershell
   dotnet add package Microsoft.Azure.WebJobs.Extensions.Sql --prerelease
   ```

2. Get the SQL connection string from your database.

  <details>
    <summary>Local SQL Server</summary>
    - Use this connection string, replacing the placeholder values for the database and password.</br>
     </br>
     <code>Server=localhost;Initial Catalog={db_name};Persist Security Info=False;User ID=sa;Password={your_password};</code>
  </details>

  <details>
    <summary>Azure SQL Server</summary>
    - Browse to the SQL Database resource in the <a href="https://ms.portal.azure.com/">Azure portal</a></br>
    - In the left blade click on the <b>Connection Strings</b> tab</br>
    - Copy the <b>SQL Authentication</b> connection string</br>
    </br>
   (<i>Note: when pasting in the connection string, you will need to replace part of the connection string where it says '{your_password}' with your Azure SQL Server password</i>)
  </details>
    
3. Open the generated `local.settings.json` file and in the `Values` section verify you have the below. If not, add the below and replace `{connection_string}` with the your connection string from the previous step:

   ```json
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
    "SqlConnectionString": "{connection_string}"
   ```

Follow this [guide](https://github.com/Azure/azure-functions-sql-extension/blob/main/docs/SetupGuide_Dotnet.md) for a deeper lesson on SQL bindings.

#### Configure Azure Maps

1. To enable this project to use the Azure Maps Search API, install the client library for .NET with NuGet:

   ```powershell
   dotnet add package Azure.Maps.Search --prerelease
   ```
2. Then get the _Azure Maps primary key_ from the Azure Portal. Navigate to your Azure Maps resource and copy the _Primary key_ content from the **Authentication** tab.

   ![image](https://user-images.githubusercontent.com/1051195/221387224-bf76cb06-280a-43eb-a53b-a41a59d84d14.png)

3. Open your _local.settings.json_ file and add the following line at the end:

   ```powershell
   "AzureMapsKey": "{Your key copied in the previous step}"
   ```

#### Run the solution

In Visual Studio Code, open the terminal and press F5 to start debugging the backend application. You should see the two functions deployed locally.

   ![image](https://user-images.githubusercontent.com/1051195/221387050-25b135b9-4b45-4cdf-8db1-f36f32a57cb8.png)

First, run the _GetLocations_ function. You should see a list of addresses with no latitude and longitude like this:

```json
[
  {
    "id": 1,
    "street_number": "81",
    "street_name": "Bay St",
    "details": "Suite 4400",
    "city": "Toronto",
    "province": "ON",
    "postal_code": "M5J 0E7",
    "country_code": "CA",
    "latitude": null,
    "longitude": null
  },
  {
    "id": 2,
    "street_number": "6795",
    "street_name": "Marconi Street",
    "details": "Suite 401",
    "city": "Montreal",
    "province": "QC",
    "postal_code": "H2S 3J9",
    "country_code": "CA",
    "latitude": null,
    "longitude": null
  }
  ...
  {
    "id": 11,
    "street_number": "375",
    "street_name": "Water Street",
    "details": "Suite 710",
    "city": "Vancouver",
    "province": "BC",
    "postal_code": "V6B 5C6",
    "country_code": "CA",
    "latitude": null,
    "longitude": null
  }
]
```

Now, run the _EnrichDatabase_ function. If no problems appear in the console, you should see the a **204** response code. 

Finally, run _GetLocations_ for a second time. The new response will now contain the geolocations for the addresses"

```json
[
  {
    "id": 1,
    "street_number": "81",
    "street_name": "Bay St",
    "details": "Suite 4400",
    "city": "Toronto",
    "province": "ON",
    "postal_code": "M5J 0E7",
    "country_code": "CA",
    "latitude": 43.64423,
    "longitude": -79.37808
  },
  {
    "id": 2,
    "street_number": "6795",
    "street_name": "Marconi Street",
    "details": "Suite 401",
    "city": "Montreal",
    "province": "QC",
    "postal_code": "H2S 3J9",
    "country_code": "CA",
    "latitude": 45.53052,
    "longitude": -73.61581
  },
  ...
  {
    "id": 11,
    "street_number": "375",
    "street_name": "Water Street",
    "details": "Suite 710",
    "city": "Vancouver",
    "province": "BC",
    "postal_code": "V6B 5C6",
    "country_code": "CA",
    "latitude": 49.28484,
    "longitude": -123.11023
  }
]
```

### Review the code

Inside the _dotnet_ folder, you'll find the all the backend code that we will use to fetch the database locations and enrich the addresses. Let's breakdown file by file.

1. Locations.cs

   This model represents the Location table in our database. Note that the _latitude_ and _longitude_ values are nullable, because when we query for the addresses for the first time, they won't exist.

2. GetLocations.cs

   This is the function that will be used in our front-end web application to search for the address geolocation and populate the map.

3. EnrichDatabase.cs

   This function searches for addresses without geolocations, calls the SearchForAddress API from Azure Maps to collect the latitude and longitude for all locations and store them in the database.

#### About Azure Maps

At this point, let's talk about how we are leveraging [Azure Maps](https://azuremaps.com/) to achieve the database enrichment process.

Azure Maps provides multiple APIs for you to geocode(generate a geolocation from an address) and reverse-geocode(generate an address from a geolocation) addresses. In this particular example, we will use the API **_SearchStructuredAddressAsync_** for our task. This API is perfect for our use case, as we have a broken down address structured that we can pass as a parameter to retrieve the geolocation as a response.

Let's look at the _AzureMapsHandler.cs_ file

```csharp
 public class AzureMapsHandler
{
    private MapsSearchClient searchClient;
 
    public AzureMapsHandler()
    {
        //Get azure maps key from configuration file
        string azureMapsKey = Environment.GetEnvironmentVariable("AzureMapsKey", EnvironmentVariableTarget.Process);

        // Create a SearchClient that will authenticate through Subscription Key (Shared key)
        AzureKeyCredential credential = new AzureKeyCredential(azureMapsKey);
        this.searchClient = new MapsSearchClient(credential);
    }

    public async Task<Location> SearchForAddress(Location location)
    {
        //Create Structured address object from database query
        var address = new StructuredAddress
        {
            CountryCode = location.country_code,
            StreetNumber = location.street_number,
            StreetName = location.street_name,
            Municipality = location.city,
            CountrySubdivision = location.province,
            PostalCode = location.postal_code
        };

        //Call the SearchStructuredAddressAsync API to query for the address geolocation
        Response<SearchAddressResult> searchResult = await this.searchClient.SearchStructuredAddressAsync(address);

        SearchAddressResultItem resultItem = searchResult.Value.Results[0];
        location.latitude = resultItem.Position.Latitude;
        location.longitude = resultItem.Position.Longitude;
        
        return location;
    }
}
```

When the class is instantiated, a credential was created using the Azure Maps key that was stored as an environment variable. After that, this credential was used to create the _MapsSearchClient_. 

```csharp
AzureKeyCredential credential = new AzureKeyCredential(azureMapsKey);
this.searchClient = new MapsSearchClient(credential);
```
Managed identities can also be used as a more secure solution to generate your credentials. Follow this [guide](https://techcommunity.microsoft.com/t5/azure-maps-blog/managed-identities-for-azure-maps/ba-p/3666312) if you would like to know more about this approach. 
 
Next a _StructuredAddress_ object was created from the location and passed as a parameter. 
 
 ```csharp
var address = new StructuredAddress
{
    CountryCode = location.country_code,
    StreetNumber = location.street_number,
    StreetName = location.street_name,
    Municipality = location.city,
    CountrySubdivision = location.province,
    PostalCode = location.postal_code
};
```

Finally, the _SearchStructureAddressAsync_ API was called passing the structured address as a parameter and updated the location with the latitude and longitude from the response.

```csharp
Response<SearchAddressResult> searchResult = await this.searchClient.SearchStructuredAddressAsync(address);
SearchAddressResultItem resultItem = searchResult.Value.Results[0];
location.latitude = resultItem.Position.Latitude;
location.longitude = resultItem.Position.Longitude;
```

If you want to see more **Search** samples, follow [this resource](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/maps/Azure.Maps.Search/samples/SearchAddressSamples.md). To learn more about the **Azure Maps C# SDK**, read [this](https://learn.microsoft.com/en-us/azure/azure-maps/how-to-dev-guide-csharp-sdk).

### Geocode storage considerations

In general, Azure Maps has specific [terms](https://www.microsoft.com/licensing/terms/productoffering/MicrosoftAzure/MOSA#ServiceSpecificTerms) of usage that prevent customers from caching or storing information delivered by the Azure Maps API including but not limited to geocodes and reverse geocodes for the purpose of scaling such results to serve multiple users, or to circumvent any functionality in Azure Maps.

However, caching and storing results is permitted where the purpose of caching is to reduce latency times of Customer’s application. Results may not be stored for longer than: the validity period indicated in returned headers; or 6 months, whichever is the shorter. Notwithstanding the foregoing, Customer may retain continual access to geocodes as long as Customer maintains an active Azure account.

## Visualize the results 

Inside the folder _frontend_ open _MapView.html_. We need to replace two lines to get this web page working.

1. In line 24, replace the value of getLocationURL with your local or remote function URL, pointing to the API we created in the previous step:

   ```javascript
   const getLocationURL = '<Your API URL>';
   ```

2. In line 36, inside the map initialization function _GetMap()_, replace _<Your subscription key>_ with your Azure Maps subscription key to authorize access to your resource:

   ```javascript
   //Initialize a map instance.
   map = new atlas.Map('myMap', {
       center: [-110.000880, 56.043483],
       zoom: 2,
       view: 'Auto',

       //Add authentication details for connecting to Azure Maps.
       authOptions: {
           authType: 'subscriptionKey',
           subscriptionKey: '<Your subscription key>'
       }
   });
   ```
   
3. Open _MapView.html_, you should see the following result:

   ![image](https://user-images.githubusercontent.com/1051195/221454289-1b5201fb-9225-433a-91b9-88e1c4d5191e.png)

## Tear down the environment when done

1. Open the resource group you created for this project.
2. Select **Delete resource group**.

    ![delete azure resource group](https://user-images.githubusercontent.com/1051195/221454826-1176eadc-69ac-4d80-aef8-3bdceff02a6c.png)
    
While this was a small sample, it should be a great staring point for any set of address data you may need to process. To find out more about Azure maps check out the following links:

Azure Maps Marketing Site: [https://azure.microsoft.com/en-us/products/azure-maps](https://azure.microsoft.com/en-us/products/azure-maps)

Azure Maps Blogs: [https://techcommunity.microsoft.com/t5/azure-maps-blog/bg-p/AzureMapsBlog](https://techcommunity.microsoft.com/t5/azure-maps-blog/bg-p/AzureMapsBlog)

Azure Maps Samples: [https://samples.azuremaps.com/](https://samples.azuremaps.com/)
