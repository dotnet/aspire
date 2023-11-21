using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Frontend.Components;
using Frontend.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// TODO: This logic can be simplified to always use the example credentials once I'm able to adjust the
// COMMAND used to startup the container. Then I can set the DynamoDB local database to not include the
// Access Key ID as part of the file name.
AWSCredentials credentials;
try
{
    credentials = FallbackCredentialsFactory.GetCredentials();
}
catch
{
    // DynamoDB local just needs something that looks like credentials. These are not real credentials as you call with the "EXAMPLE" suffix.
    credentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
}

builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(credentials));
builder.Services.AddSingleton<ZipCodeRepository>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
