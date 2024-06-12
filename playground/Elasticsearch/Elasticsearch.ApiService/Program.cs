// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Elasticsearch.Net;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<ElasticLowLevelClient>((sp) =>
{
    var settings = new ConnectionConfiguration(new Uri(builder.Configuration.GetConnectionString("elasticsearch")!))
    .RequestTimeout(TimeSpan.FromMinutes(2));

    var lowlevelClient = new ElasticLowLevelClient(settings);
    return lowlevelClient;
});

var app = builder.Build();

app.MapGet("/get", async (ElasticLowLevelClient elasticClient) =>
{
    var response = await elasticClient.GetAsync<StringResponse>("people", "1");
    return response.Body;
});

app.MapGet("/create", async (ElasticLowLevelClient elasticClient) =>
{
    var exist = await elasticClient.Indices.ExistsAsync<StringResponse>(index: "people");
    if (exist.Success)
    {
        elasticClient.Indices.Delete<StringResponse>("people");
    }

    var person = new
    {
        FirstName = "Alireza",
        LastName = "Baloochi"
    };

    var response = await elasticClient.IndexAsync<StringResponse>("people", "1", PostData.Serializable(person));
    return response.Body;
});

app.Run();
