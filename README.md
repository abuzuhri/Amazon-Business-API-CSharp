# ☕Amazon Business API C# 🚀 [![.NET](https://github.com/abuzuhri/Amazon-Business-API-CSharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/abuzuhri/Amazon-Business-API-CSharp/actions/workflows/dotnet.yml) [![NuGet](https://img.shields.io/nuget/v/CSharpAmazonBusinessAPI.svg)](https://www.nuget.org/packages/CSharpAmazonBusinessAPI/) 

.Net C# library for the Amazon Business API

This is an API Binding in .Net C# for the [Amazon Business API](https://developer-docs.amazon.com/amazon-business/docs).

This library is based on the output of [swagger-codegen](https://app.swaggerhub.com/home) with the [OpenAPI files provided by Amazon (Models)](https://developer-docs.amazon.com/amazon-business/docs/document-api-v1-model) and has been modified by the contributors.

The purpose of this package is to have an easy way of getting started with the Amazon Business API using C#




---
## Installation [![NuGet](https://img.shields.io/nuget/v/CSharpAmazonBusinessAPI.svg)](https://www.nuget.org/packages/CSharpAmazonBusinessAPI/)

```powershell
Install-Package CSharpAmazonBusinessAPI
```


### Tasks

- [] [Document API v1](https://developer-docs.amazon.com/amazon-business/docs/document-api-v1-reference-1)
- [] [Product Search API v1](https://developer-docs.amazon.com/amazon-business/docs/product-search-api-v1-reference)
- [] [Reconciliation API v1](https://developer-docs.amazon.com/amazon-business/docs/reconciliation-api-v1-reference)
- [] [Reporting API v1](https://developer-docs.amazon.com/amazon-business/docs/reporting-api-v1-reference-1)

---
## Keys
To get all keys needed you need to follow this step [Creating and configuring IAM policies and entities](https://developer-docs.amazon.com/amazon-business/docs/authorization-workflow) and then you need to [Registering your Application](https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer) then [Authorizing applications
](https://developer-docs.amazon.com/amazon-business/docs/create-app-client)
> :warning: **Use role ARN and dont use IAM user**


| Name | Description |
| --- | --- |
| AccessKey | AWS USER ACCESS KEY |
| SecretKey | AWS USER SECRET KEY |
| RoleArn | AWS IAM Role ARN (needs permission to “Assume Role” STS) |
| Region | Marketplace region [List of Marketplaces](https://developer-docs.amazon.com/amazon-business/docs/marketplace-ids)|
| ClientId | Your amazon app id |
| ClientSecret | Your amazon app secret |
| RefreshToken | Check how to get [RefreshToken](https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow) |


For more information about keys please check [New Amazon doc for create keys Developer ](https://developer-docs.amazon.com/) , If you are not registered as developer please [Register](https://developercentral.amazon.com/) to be able to create application. 


### Configuration
```CSharp
AmazonConnection amazonConnection = new AmazonConnection(new AmazonCredential()
{
     AccessKey = "AKIAXXXXXXXXXXXXXXX",
     SecretKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     RoleArn = "arn:aws:iam::XXXXXXXXXXXXX:role/XXXXXXXXXXXX",
     ClientId = "amzn1.application-XXX-client.XXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     ClientSecret = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     RefreshToken= "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     MarketPlace = MarketPlace.UnitedArabEmirates, //MarketPlace.GetMarketPlaceByID("A2VIGQ35RCS4UG") 
});

or 

AmazonConnection amazonConnection = new AmazonConnection(new AmazonCredential()
{
     AccessKey = "AKIAXXXXXXXXXXXXXXX",
     SecretKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     RoleArn = "arn:aws:iam::XXXXXXXXXXXXX:role/XXXXXXXXXXXX",
     ClientId = "amzn1.application-XXX-client.XXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     ClientSecret = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     RefreshToken= "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
     MarketPlaceID = "A2VIGQ35RCS4UG"
});

```


### Document API v1 , Amazon Business API for Reports [Here](..).
```CSharp
.....

```




 
