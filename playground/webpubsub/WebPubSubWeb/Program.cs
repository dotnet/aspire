using Azure.Messaging.WebPubSub;

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureWebPubSubHub("wps1", "chatForAspire");

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// return the Client Access URL with negotiate endpoint
app.MapGet("/negotiate", (WebPubSubServiceClient service) =>
new
{
    url = service.GetClientAccessUri(roles: ["webpubsub.sendToGroup.group1", "webpubsub.joinLeaveGroup.group1"]).AbsoluteUri
});
app.Run();
