// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Mvc;

namespace Aspire.Dashboard.ConsoleLogs;

[ApiController]
[Route("/api/logs")]
public class LogsController(IDashboardClient dashboardClient) : ControllerBase
{
    [HttpGet("{resourceName}/download")]
    public async Task<IActionResult> DownloadLogsForResource([FromRoute] string resourceName)
    {
        var logsText = await GetAllLogsAsync(resourceName).ConfigureAwait(false);
        return File(Encoding.Default.GetBytes(logsText), "text/plain", $"{resourceName}.log");
    }

    [HttpGet("{resourceName}")]
    public async Task<ActionResult<string>> GetLogsForResource([FromRoute] string resourceName)
    {
        return new ActionResult<string>(await GetAllLogsAsync(resourceName).ConfigureAwait(false));
    }

    private async Task<string> GetAllLogsAsync(string resourceName)
    {
        var rawLogs = new List<string>();
        await foreach (var logLine in dashboardClient.GetConsoleLogsAsync(resourceName, CancellationToken.None).ConfigureAwait(false))
        {
            rawLogs.Add(logLine.Content);
        }

        return string.Join(Environment.NewLine, rawLogs);
    }
}
