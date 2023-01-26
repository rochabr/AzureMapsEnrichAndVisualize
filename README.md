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


