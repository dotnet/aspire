# Azure Web PubSub Aspire sample

This is a simple ASP.NETCore Web application:
* [The server side](Program.cs) shows how to use `AddAzureWebPubSubHub` to add `WebPubSubServiceClient` and provides a `negotiate` endpoint for the clients to call.
* [The client side](./Pages/Index.cshtml) shows how to use a simple native WebSocket API to chat in a group 
