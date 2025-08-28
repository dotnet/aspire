using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Testing;

// Test SetScopeAsync with just resource group (original behavior)
var builder1 = TestDistributedApplicationBuilder.Create();
var bicep1 = builder1.AddBicepTemplateString("test", "param name string").Resource;
bicep1.Scope = new("test-rg");
var scope1 = new JsonObject();
await BicepUtilities.SetScopeAsync(scope1, bicep1);
Console.WriteLine($"Test 1 - Count: {scope1.Count}");
Console.WriteLine($"Test 1 - ResourceGroup: {scope1["resourceGroup"]}");
Console.WriteLine($"Test 1 - Has Subscription: {scope1.ContainsKey("subscription")}");

// Test SetScopeAsync with no scope (original behavior)  
var builder2 = TestDistributedApplicationBuilder.Create();
var bicep2 = builder2.AddBicepTemplateString("test", "param name string").Resource;
var scope2 = new JsonObject();
await BicepUtilities.SetScopeAsync(scope2, bicep2);
Console.WriteLine($"Test 2 - Count: {scope2.Count}");
Console.WriteLine($"Test 2 - ResourceGroup: {scope2["resourceGroup"]}");
Console.WriteLine($"Test 2 - Has Subscription: {scope2.ContainsKey("subscription")}");

// Test SetScopeAsync with resource group and subscription (new behavior)
var builder3 = TestDistributedApplicationBuilder.Create();
var bicep3 = builder3.AddBicepTemplateString("test", "param name string").Resource;
bicep3.Scope = new("test-rg", "test-sub");
var scope3 = new JsonObject();
await BicepUtilities.SetScopeAsync(scope3, bicep3);
Console.WriteLine($"Test 3 - Count: {scope3.Count}");
Console.WriteLine($"Test 3 - ResourceGroup: {scope3["resourceGroup"]}");
Console.WriteLine($"Test 3 - Subscription: {scope3["subscription"]}");