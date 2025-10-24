using System.Collections;
using System.Collections.ObjectModel;

namespace AspireWithMaui.MauiClient;

public partial class EnvironmentPage : ContentPage
{
    public ObservableCollection<KeyValuePair<string, string>> AspireEnvironmentVariables { get; } = new();

    public EnvironmentPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAspireEnvironmentVariables();
    }

    private void LoadAspireEnvironmentVariables()
    {
        AspireEnvironmentVariables.Clear();

        var variables = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Select(entry => new KeyValuePair<string, string>(entry.Key?.ToString() ?? string.Empty, DecodeValue(entry.Value?.ToString())))
            .Where(item => IsAspireVariable(item.Key))
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var variable in variables)
        {
            AspireEnvironmentVariables.Add(variable);
        }
    }

    private static string DecodeValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            var decoded = Uri.UnescapeDataString(value);
            
            // Validate that the decoded string doesn't contain control characters that could indicate malicious content
            // Allow only printable characters, tabs, and newlines
            if (decoded.Any(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r'))
            {
                // If suspicious control characters found, return the original encoded value for safety
                return value;
            }
            
            return decoded;
        }
        catch (UriFormatException)
        {
            // If decoding fails, return the original value
            return value;
        }
    }

    private static bool IsAspireVariable(string key)
        => key.StartsWith("services__", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("connectionstrings__", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("ASPIRE_", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("AppHost__", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("OTEL_", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("LOGGING__CONSOLE", StringComparison.OrdinalIgnoreCase)
           || key.Equals("ASPNETCORE_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
           || key.Equals("ASPNETCORE_URLS", StringComparison.OrdinalIgnoreCase)
           || key.Equals("DOTNET_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
           || key.Equals("DOTNET_URLS", StringComparison.OrdinalIgnoreCase)
           || key.Equals("DOTNET_LAUNCH_PROFILE", StringComparison.OrdinalIgnoreCase)
           || key.Equals("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", StringComparison.OrdinalIgnoreCase);
}
