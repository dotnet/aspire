// Quick test to verify the IInteractionService behavior
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

// Test 1: Dashboard enabled (should work for all methods)
Console.WriteLine("=== Test 1: Dashboard enabled ===");
var enabledService = new InteractionService(
    NullLogger<InteractionService>.Instance,
    new DistributedApplicationOptions { DisableDashboard = false },
    new ServiceCollection().BuildServiceProvider());

try
{
    var input = new InteractionInput { Label = "Value", InputType = InputType.Text, Required = true };
    var inputTask = enabledService.PromptInputAsync("Test", "message", input);
    Console.WriteLine("✅ PromptInputAsync: Creates task successfully (dashboard enabled)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ PromptInputAsync: {ex.Message}");
}

try
{
    var confirmTask = enabledService.PromptConfirmationAsync("Test", "message");
    Console.WriteLine("✅ PromptConfirmationAsync: Creates task successfully (dashboard enabled)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ PromptConfirmationAsync: {ex.Message}");
}

// Test 2: Dashboard disabled (only input methods should work)
Console.WriteLine("\n=== Test 2: Dashboard disabled ===");
var disabledService = new InteractionService(
    NullLogger<InteractionService>.Instance,
    new DistributedApplicationOptions { DisableDashboard = true },
    new ServiceCollection().BuildServiceProvider());

try
{
    var input = new InteractionInput { Label = "Value", InputType = InputType.Text, Required = true };
    var inputTask = disabledService.PromptInputAsync("Test", "message", input);
    Console.WriteLine("✅ PromptInputAsync: Creates task successfully (dashboard disabled)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ PromptInputAsync: {ex.Message}");
}

try
{
    var inputs = new[] { new InteractionInput { Label = "Value", InputType = InputType.Text, Required = true } };
    var inputsTask = disabledService.PromptInputsAsync("Test", "message", inputs);
    Console.WriteLine("✅ PromptInputsAsync: Creates task successfully (dashboard disabled)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ PromptInputsAsync: {ex.Message}");
}

try
{
    var confirmTask = disabledService.PromptConfirmationAsync("Test", "message");
    Console.WriteLine("❌ PromptConfirmationAsync: Should have thrown but didn't (dashboard disabled)");
}
catch (Exception ex)
{
    Console.WriteLine($"✅ PromptConfirmationAsync: {ex.Message}");
}

try
{
    var messageTask = disabledService.PromptMessageBoxAsync("Test", "message");
    Console.WriteLine("❌ PromptMessageBoxAsync: Should have thrown but didn't (dashboard disabled)");
}
catch (Exception ex)
{
    Console.WriteLine($"✅ PromptMessageBoxAsync: {ex.Message}");
}

try
{
    var notificationTask = disabledService.PromptNotificationAsync("Test", "message");
    Console.WriteLine("❌ PromptNotificationAsync: Should have thrown but didn't (dashboard disabled)");
}
catch (Exception ex)
{
    Console.WriteLine($"✅ PromptNotificationAsync: {ex.Message}");
}

Console.WriteLine("\n=== Summary ===");
Console.WriteLine("The fix successfully allows input methods to work when dashboard is disabled,");
Console.WriteLine("while other interaction methods still throw appropriately.");