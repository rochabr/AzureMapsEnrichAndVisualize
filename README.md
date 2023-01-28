# Enrich your location data with Azure Maps

## Introduction

Location information can be very simple or very detailed. For a person delivering a package to certain house, an address with street number, name, city, and state could be more than enough information for them to find the right place. For a vehicle tracking system where you have a map with moving dots representing trucks in real-time, these pieces of data may not be enough to provide clear information to a dispatcher. Adding the latitude and longitude of the vehicle to present its current and historical positions is more important. 

A very common scenario is for a company to inherit or purchase a database of addresses to improve a certain process of their business. A retail chain may need to know where to put their next store to serve an undeserved population, a transportation company may need to find the best "green" route to create their truck schedule, or a sales compamy may need to define territories so that their sales people can maximize their coverage to achieve better results. Independent of the scenario, having a database of addresses may not be enough to achieve certain goals, because important details could be missing.

In this blog post, I will show you how to create a database of simple addresses, how to enrich it and how to visualize the enriched dataset using Azure Maps.

## Architecture

In the following architecture we have two separate actors securely connecting to Azure via a web application hosted in Azure App Services and an Azure Function, triggered on demand. 

The Azure Function named _Enrich local data_ is responsible for searching for addresses without geolocation(latitude, longitude) in the Azure SQL Server database. For each addreess, a call is made to Azure Maps' Search API, passing the address information as a parameter and retrieving the geolocation to be stored in the database.

There is also a user accessing a web applicaiton that collects the locations from Azure SQL Server using the Azure Function _Get locations_ and generates a visualization layer with the Azure Maps SDK to present the addresses as points in a map. All traffic is handled via https and is encrypted with TLS.

![Architecture diagram](https://user-images.githubusercontent.com/1051195/214759203-1ea95346-68b1-418b-a82d-454cf5084437.png)

## Walkthrough

### Pre-requisites

To build this solution you will need:

1. An Azure Subscription
2. An Azure Maps subscription
3. Visual Studio Code and Git installed on your local machine

### Creating a resource group

As a best practice and to facilitate tearing down the environment, follow [these instructions](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups) to create a Resource Group in your Azure Subscription.

### Creating the database

Follow the instructions [here](https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart?view=azuresql&tabs=azure-portal), to create an Azure SQL Database Server and a database, you can use any name you want to create your resources. In this tutorial we will call the database _azuremapsdb_.

When all stabase resources have been provisioned, in the Azure Portal navigate to your database server and update the network configurations to allow public network access from selected networks, add your IP address in the firewall rules as an allowed inbound traffic rule for local testing and select the box at the end to allow Azure Services to access the resource. We will need this to allow access from our Azure Functions to the database.

![networking_config_db](https://user-images.githubusercontent.com/1051195/214764219-c2f837ca-ffc3-47b1-8fda-ea75bf184914.png)

After that use the query editor to populate the database with the following commands:

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
The script above creates a table called _Locations_ and populates it with address parameters. After that, we populate the database with addresses representing Microsoft offices in Canada. Note that we are keeping the _latitude_ and _longitude_ parameters empty, on purpose.

### Enriching the database and querying for addresses

We are going to be creating two functions: 

1. **_EnrichDatabase_** queries every address in the database that has an a\empty geolocation, makes a _Search API_ call to Azure Maps and populates the database with the latitude and longitude gathered from the response. We will use this first to enrich our address database.
2. **_GetLocations_** simply queries for all enriched adddresses in the database. We will use this function in our front-end web application.

#### Create the Function App

We will be coding the functions in C# to take advantage of [Azure Functions SQL Extensions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-azure-sql), which are easier to achieve in C# than other coding languages, as of now. However, you can also take advantage of them in Java, JavaScript, Powershell and Python. Overall, they expedite the development time to build the connectivity between the Azure Function and the Azure SQL database by creating a simpler process to generate inputs, outputs and triggers.

First, install [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)

Then, create a function app for .NET by following [this guide](https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-csharp?tabs=in-process) to create your functions on Visual Studio code and publish them to your Azure Subscription. Create one called _EnrichDatabase_ and another called _GetLocations_. 

After that, enable SQL bindings on the function app. More information can be found in the [Azure SQL bindings for Azure Functions docs](https://aka.ms/sqlbindings).

    Install the extension:

    ```powershell
    dotnet add package Microsoft.Azure.WebJobs.Extensions.Sql --prerelease
    ```
    
Follow [this guide](https://github.com/Azure/azure-functions-sql-extension/blob/main/docs/SetupGuide_Dotnet.md) for a deeper lesson on SQL bindings.

#### Configure Function App

Once you have your Function App you need to configure it for use with Azure SQL bindings for Azure Functions.

1. Ensure you have Azure Storage Emulator running. This is specific to the sample functions in this repository with a non-HTTP trigger. For information on the Azure Storage Emulator, refer to the docs on its use in [functions local development](https://docs.microsoft.com/azure/azure-functions/functions-app-settings#azurewebjobsstorage) and [installation](https://docs.microsoft.com/azure/storage/common/storage-use-emulator#get-the-storage-emulator).

2. Get your SQL connection string

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


#### GetGeolocationsNull


