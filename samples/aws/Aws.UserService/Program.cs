using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Aws.UserService.Contracts;
using Aws.UserService.Models;
using Aws.UserService.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IProfileService, ProfileService>();
builder.Services.AddTransient<IS3UrlService, S3UrlService>();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/profile", async (Profile profile, IProfileService profileService) =>
    {
        var profileId = await profileService.AddProfileAsync(profile);

        return Results.Created($"/profile/{profileId}", profileId);
    })
    .WithName("PostProfile");

app.Run();
