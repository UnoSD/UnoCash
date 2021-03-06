# UnoCash

#### UnoSD/UnoCash is just for C# end-to-end demo purposes
#### UnoSD/UnoCash.Fable is F# end-to-end under active development

This tool is an attempt to consolidate and improve several other tools I use for personal finance:

* A desktop application I use to store transactions and get reports with graphs
* A bill splitting SaaS tool called Splitwise which works with a web app or mobile app to share and keep track of shared expenses both in groups in between individuals
* A tool to automatically recognise receipts from pictures and populate a transaction (this is not something I use, but I do manually at the moment)
* Potentially, using mobile payment methods to feed directly data into UnoCash without using receipts at all (when you're not paying cash)

# Architecture

![Outdated infrastructure diagram](https://github.com/UnoSD/UnoCash/raw/master/Architecture.png)

# Technologies

I am using the latest modern (and cool) technologies available; several are in preview, but, for now, this is not something I mean to publish as a stable production system; by the time (and if) is completed it they will likely be GA.

It is also something I will probably use in my tech talks as a prototype of a modern serverless SaaS application.

The project uses:

* Blazor client side to create a SPA in WebAssembly and to have full end-to-end .NET, it also allows sharing of libraries between front end and back end making my life easier and it's awsome compared to text-searching JavaScript and getting version conflicts between DTOs updated on either end (Now most of the front end work is in Fable + Elmish)
* Azure Functions as a serverless microservice back end, deployed in a consumption plan so extremely cheap
* Azure Storage blobs to host the static website in Blazor so no need for any compute for the front end
* Azure Tables for storage, because it's way cheaper than Cosmos DB and it is behind the same SDK API so it can be easily migrated
* Azure Form Recognizer to analyse receipt photos and extract data (still in private preview, I had to request access to Microsoft)
* Azure API Management on a consumption plan to manage calls from the front end to back end, adding security and quotas
* Azure AD for authentication
* Pulumi for deployments (using my Pulumi.FSharp.Azure DSL library for idiomatic F# feel) (and ARM and Azure CLI for missing features in Pulumi)

It will use soon-ish:

* Azure AD B2C for identity
* Not that soon: Azure Front Door for WAF, CDN and global load balancing
* Not that soon: AKS to host analytics services
