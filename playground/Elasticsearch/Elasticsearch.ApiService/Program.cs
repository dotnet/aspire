// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Elastic.Clients.Elasticsearch;
using Elasticsearch.ApiService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddElasticsearchClient("elasticsearch");

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/get", async (ElasticsearchClient elasticClient) =>
{
    var response = await elasticClient.GetAsync<Person>("people", "1");
    return response;
});

app.MapGet("/create", async (ElasticsearchClient elasticClient) =>
{
    var exist = await elasticClient.Indices.ExistsAsync("people");
    if (exist.Exists)
    {
        await elasticClient.Indices.DeleteAsync("people");
    }

    var person = new Person
    {
        FirstName = "Alireza",
        LastName = "Baloochi"
    };

    var response = await elasticClient.IndexAsync<Person>(person, "people", "1");
    return response;
});

app.Run();
