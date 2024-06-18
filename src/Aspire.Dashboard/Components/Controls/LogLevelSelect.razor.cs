using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class LogLevelSelect : ComponentBase
{
    private async Task HandleSelectedLogLevelChangedInternalAsync()
    {
        await LogLevelChanged.InvokeAsync(LogLevel);
        await HandleSelectedLogLevelChangedAsync();
    }
}

