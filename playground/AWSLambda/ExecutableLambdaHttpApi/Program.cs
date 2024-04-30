// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using ExecutableLambdaHttpApi;

IDatabaseRepository repo = new DatabaseRepository();

await LambdaBootstrapBuilder.Create<APIGatewayHttpApiV2ProxyRequest>(Handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();

async Task<APIGatewayHttpApiV2ProxyResponse> Handler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
{
    if (!request.PathParameters.TryGetValue("id", out var id))
    {
        id = "5";
    }

    var databaseRecord = await repo.GetById(id);

    return new APIGatewayHttpApiV2ProxyResponse
    {
        StatusCode = (int)HttpStatusCode.OK,
        Body = JsonSerializer.Serialize(databaseRecord)
    };
}
