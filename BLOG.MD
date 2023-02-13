# Enrich your location data with Azure Maps

## Introduction

Location information can be very simple or very detailed. For a person delivering a package to certain house, an address with street number, name, city, and state could be more than enough information for them to find the right place. For a vehicle tracking system where you have a map with moving dots representing trucks in real-time, these pieces of data may not be enough to provide clear information to a dispatcher. Adding the latitude and longitude of the vehicle to present its current and historical positions is more important. 

A very common scenario is for a company to inherit or purchase a database of addresses to improve a certain process of their business. A retail chain may need to know where to put their next store to serve an undeserved population, a transportation company may need to find the best "green" route to create their truck schedule, or a sales compamy may need to define territories so that their sales people can maximize their coverage to achieve better results. Independent of the scenario, having a database of addresses may not be enough to achieve certain goals, because important details could be missing.

In this blog post, I will show you how to create a database of simple addresses, how to enrich it and how to visualize the enriched dataset using Azure Maps.

## Architecture

In the following architecture we have two separate actors securely connecting to Azure via a web application hosted in Azure App Services and an Azure Function, triggered on demand. 

The Azure Function named _Enrich local data_ is responsible for searching for addresses without geolocation(latitude, longitude) in the Azure SQL Server database. For each addreess, a call is made to Azure Maps' Search API, passing the address information as a parameter and retrieving the geolocation to be stored in the database.

There is also a user accessing a web applicaiton that collects the locations from Azure SQL Server using the Azure Function _Get locations_ and generates a visualization layer with the Azure Maps SDK to present the addresses as points in a map. All traffic is handled via https and is encrypted with TLS.

<p align="center">
  <img src="https://user-images.githubusercontent.com/1051195/214759203-1ea95346-68b1-418b-a82d-454cf5084437.png" alt="Architecture diagram"/>
</p>

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

## Walkthrough

For a complete walkthrough on how to deploy the solution clone [TODO] this repository and follow the guide.

### Geocode storage considerations

In general, Azure Maps has specific terms of usage that prevent customers from caching or storing information delivered by the Azure Maps API including but not limited to geocodes and reverse geocodes for the purpose of scaling such results to serve multiple users, or to circumvent any functionality in Azure Maps.

However, caching and storing results is permitted where the purpose of caching is to reduce latency times of Customer’s application. Results may not be stored for longer than: the validity period indicated in returned headers; or 6 months, whichever is the shorter. Notwithstanding the foregoing, Customer may retain continual access to geocodes as long as Customer maintains an active Azure account.

## Conclusion
