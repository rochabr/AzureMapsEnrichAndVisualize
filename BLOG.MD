# Enrich your location data with Azure Maps

## Introduction

Location information can be very simple or very detailed. For a person delivering a package to certain house, an address with street number, name, city, and state could be more than enough information for them to find the right place. For a vehicle tracking system where you have a map with moving dots representing trucks in real-time, these pieces of data may not be enough to provide clear information to a dispatcher. Adding the latitude and longitude of the vehicle to present its current and historical positions is more important. 

A very common scenario is for a company to inherit or purchase a database of addresses to improve a certain process of their business. A retail chain may need to know where to put their next store to serve an undeserved population, a transportation company may need to find the best "green" route to create their truck schedule, or a sales compamy may need to define territories so that their sales people can maximize their coverage to achieve better results. Independent of the scenario, having a database of addresses may not be enough to achieve certain goals, because important details could be missing.

In this blog post, I will show you how to create a database of simple addresses, how to enrich it and how to visualize the enriched dataset using Azure Maps.

## Architecture

In the following architecture we have two separate actors securely connecting to Azure via a web application hosted in Azure App Services and an Azure Function, triggered on demand. 

The Azure Function named _Enrich local data_ is responsible for searching for addresses without geolocation(latitude, longitude) in the Azure SQL Server database. For each addreess, a call is made to Azure Maps' Search API, passing the address information as a parameter and retrieving the geolocation to be stored in the database.

There is also a user accessing a web applicaiton that collects the locations from Azure SQL Server using the Azure Function _Get locations_ and generates a visualization layer with the Azure Maps SDK to present the addresses as points in a map. All traffic is handled via https and is encrypted with TLS.

<br>

<p align="center">
  <img src="https://user-images.githubusercontent.com/1051195/218619577-55b2f7f4-4a8d-4d45-8fa5-d35d94fa1515.png" alt="Architecture diagram"/>
</p>

<br>

### Address enrichment logical flow

To enrich the database of addresses, the application makes a request to the database, querying for all addresses without geolocations. In return, it receives a list of addresses:

<br>

<p align="center">
  <img src="https://user-images.githubusercontent.com/1051195/218586495-e703a43a-241d-45e4-81a5-fbc56d53b7a3.png" alt="Logical flow for request addresses"/>
</p>

<br>

With the full list of addresses without geolocation, the application makes single calls with every single address to Azure Maps, calling the _Search API_. Azure Maps in response, provides the geolocation for that particular address. The response adds that inforamtion to the item and saves it locally. The _Search API_ also provides  extra properties in the response that are ignored by the application in this example.

<br>

<p align="center">
  <img src="https://user-images.githubusercontent.com/1051195/218586517-72504705-20e2-4607-b82b-0af9258f8d51.png" alt="Logical flow for searching for geolocation"/>
</p>

<br>

Lastly, the application saves the updated addresses with their corresponding geolocations to the database. 

<br>

<p align="center">
  <img src="https://user-images.githubusercontent.com/1051195/218586533-92406c5d-73c2-46c5-bf62-2a422234ac88.png" alt="Logical flow for searching for geolocation"/>
</p>

<br>

### Address visualization- Logical flow

The visualization layer contains a static web app, hosted in Azure, leveraging an Azure function to query the database for all addresses with geolocations. It then connects to Azure Maps to get the map tiles with dots representing the address.

<br>

<p align="center">
  <img src="https://user-images.githubusercontent.com/1051195/218613101-e7834cdf-2dee-4f77-aabc-94b6b4c4f083.png" alt="Map view flow"/>
</p>

<br>

<br>

This is the end result:

<p align="center">
  <img src="https://user-images.githubusercontent.com/1051195/218615805-40729f76-2e3f-4bb0-a96f-2854b64651bf.png" alt="Web app with a map showing the addresses"/>
</p>

<br>

<br>


## Walkthrough

For a detailed step-by-step on how to create this solution, clone [TODO] this repository and follow its instructions.

## Geocode storage considerations

In general, Azure Maps has specific [terms](https://www.microsoft.com/licensing/terms/productoffering/MicrosoftAzure/MOSA#ServiceSpecificTerms) of usage that prevent customers from caching or storing information delivered by the Azure Maps API including but not limited to geocodes and reverse geocodes for the purpose of scaling such results to serve multiple users, or to circumvent any functionality in Azure Maps.

However, caching and storing results is permitted where the purpose of caching is to reduce latency times of Customer’s application. Results may not be stored for longer than: the validity period indicated in returned headers; or 6 months, whichever is the shorter. Notwithstanding the foregoing, Customer may retain continual access to geocodes as long as Customer maintains an active Azure account.

## Conclusion

Location data is powerful. When added to your analytics, it can provide valuable insight to corporations when making important decisions. Azure Maps provides a modern, secure and reliable cloud platform for any geospatial challenge that you may face. 

In this blog post you learned how to enrich a database of addresses with [Azure Maps](https://azure.microsoft.com/en-us/products/azure-maps) by using the C# SDK within an Azure Function. You have also created a web page to visualize all results queries from the enriched database. For more practical and real-world examples, follow the [Azure Maps Blog](https://techcommunity.microsoft.com/t5/azure-maps-blog/bg-p/AzureMapsBlog).



