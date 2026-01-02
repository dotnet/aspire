// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Tests.Utils;

public class TelemetryOutputFormatterTests
{
    private static (TelemetryOutputFormatter formatter, StringWriter writer) CreateFormatter(bool enableColor = false)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer)
        });
        var formatter = new TelemetryOutputFormatter(console, enableColor);
        return (formatter, writer);
    }

    [Fact]
    public void FormatTraces_EmptyJson_ShowsEmptyMessage()
    {
        var (formatter, writer) = CreateFormatter();

        formatter.FormatTraces("");

        var output = writer.ToString();
        Assert.Contains("No traces found", output);
    }

    [Fact]
    public void FormatTraces_NullJson_ShowsEmptyMessage()
    {
        var (formatter, writer) = CreateFormatter();

        formatter.FormatTraces(null!);

        var output = writer.ToString();
        Assert.Contains("No traces found", output);
    }

    [Fact]
    public void FormatTraces_EmptyArray_ShowsEmptyMessage()
    {
        var (formatter, writer) = CreateFormatter();

        formatter.FormatTraces("[]");

        var output = writer.ToString();
        Assert.Contains("No traces found", output);
    }

    [Fact]
    public void FormatTraces_SingleTrace_FormatsCorrectly()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            [
                {
                    "trace_id": "abc123",
                    "title": "GET /api/users",
                    "duration_ms": 150.5,
                    "has_error": false,
                    "timestamp": "2024-01-15T10:30:00Z",
                    "spans": [
                        {
                            "span_id": "span1",
                            "name": "HTTP GET",
                            "source": "webfrontend",
                            "duration_ms": 150.5
                        }
                    ]
                }
            ]
            """;

        formatter.FormatTraces(json);

        var output = writer.ToString();
        Assert.Contains("TRACES", output);
        Assert.Contains("1 total", output);
        Assert.Contains("abc123", output);
        Assert.Contains("GET /api/users", output);
        Assert.Contains("webfrontend", output);
    }

    [Fact]
    public void FormatTraces_MultipleSpans_ShowsResourceFlow()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            [
                {
                    "trace_id": "abc123",
                    "title": "User Request",
                    "duration_ms": 250,
                    "has_error": false,
                    "spans": [
                        {
                            "span_id": "span1",
                            "name": "HTTP GET",
                            "source": "webfrontend",
                            "duration_ms": 250
                        },
                        {
                            "span_id": "span2",
                            "parent_span_id": "span1",
                            "name": "Database Query",
                            "source": "apiservice",
                            "duration_ms": 50
                        }
                    ]
                }
            ]
            """;

        formatter.FormatTraces(json);

        var output = writer.ToString();
        Assert.Contains("webfrontend", output);
        Assert.Contains("apiservice", output);
        Assert.Contains("Spans: 2", output);
    }

    [Fact]
    public void FormatTraces_WithError_ShowsErrorSymbol()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            [
                {
                    "trace_id": "error123",
                    "title": "Failed Request",
                    "duration_ms": 50,
                    "has_error": true,
                    "spans": []
                }
            ]
            """;

        formatter.FormatTraces(json);

        var output = writer.ToString();
        Assert.Contains("✗", output); // Error symbol
        Assert.Contains("error123", output);
    }

    [Fact]
    public void FormatTraces_ShowsNewestFirst()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            [
                {
                    "trace_id": "old",
                    "title": "Old Trace",
                    "duration_ms": 100,
                    "has_error": false,
                    "timestamp": "2024-01-15T08:00:00Z",
                    "spans": []
                },
                {
                    "trace_id": "new",
                    "title": "New Trace",
                    "duration_ms": 100,
                    "has_error": false,
                    "timestamp": "2024-01-15T12:00:00Z",
                    "spans": []
                }
            ]
            """;

        formatter.FormatTraces(json);

        var output = writer.ToString();
        var newIndex = output.IndexOf("new");
        var oldIndex = output.IndexOf("old");
        Assert.True(newIndex < oldIndex, "New trace should appear before old trace");
    }

    [Fact]
    public void FormatSingleTrace_ShowsDetailedSpanHierarchy()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            {
                "trace_id": "detailed123",
                "title": "Detailed Request",
                "duration_ms": 300,
                "has_error": false,
                "timestamp": "2024-01-15T10:30:00Z",
                "spans": [
                    {
                        "span_id": "root",
                        "name": "HTTP Request",
                        "source": "frontend",
                        "duration_ms": 300,
                        "attributes": {
                            "http.method": "GET",
                            "http.url": "/api/data"
                        }
                    },
                    {
                        "span_id": "child1",
                        "parent_span_id": "root",
                        "name": "API Call",
                        "source": "backend",
                        "duration_ms": 200,
                        "attributes": {
                            "api.endpoint": "users"
                        }
                    },
                    {
                        "span_id": "child2",
                        "parent_span_id": "child1",
                        "name": "Database Query",
                        "source": "database",
                        "duration_ms": 50,
                        "status": "Error",
                        "attributes": {
                            "db.system": "postgresql"
                        }
                    }
                ]
            }
            """;

        formatter.FormatSingleTrace(json);

        var output = writer.ToString();
        Assert.Contains("detailed123", output);
        Assert.Contains("HTTP Request", output);
        Assert.Contains("API Call", output);
        Assert.Contains("Database Query", output);
        Assert.Contains("http.method", output);
    }

    [Fact]
    public void FormatSingleTrace_EmptyJson_ShowsEmptyMessage()
    {
        var (formatter, writer) = CreateFormatter();

        formatter.FormatSingleTrace("");

        var output = writer.ToString();
        Assert.Contains("No trace found", output);
    }

    [Fact]
    public void FormatTraces_WithDashboardLink_ShowsLink()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            [
                {
                    "trace_id": "linked123",
                    "title": "Request with Link",
                    "duration_ms": 100,
                    "has_error": false,
                    "spans": [],
                    "dashboard_link": {
                        "url": "http://localhost:18888/traces/linked123",
                        "text": "linked123"
                    }
                }
            ]
            """;

        formatter.FormatTraces(json);

        var output = writer.ToString();
        Assert.Contains("Dashboard:", output);
        Assert.Contains("http://localhost:18888", output);
    }

    [Fact]
    public void FormatTraces_InvalidJson_HandlesGracefully()
    {
        var (formatter, writer) = CreateFormatter();

        formatter.FormatTraces("not valid json");

        var output = writer.ToString();
        Assert.Contains("Unable to parse", output);
    }

    [Fact]
    public void FormatTraces_NonArrayJson_ShowsEmptyMessage()
    {
        var (formatter, writer) = CreateFormatter();

        formatter.FormatTraces("""{"key": "value"}""");

        var output = writer.ToString();
        Assert.Contains("No traces found", output);
    }

    [Fact]
    public void FormatTraces_DurationFormatting_ShowsReadableFormat()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            [
                {
                    "trace_id": "duration1",
                    "title": "Fast Request",
                    "duration_ms": 5,
                    "has_error": false,
                    "spans": []
                },
                {
                    "trace_id": "duration2",
                    "title": "Slow Request",
                    "duration_ms": 1500,
                    "has_error": false,
                    "spans": []
                }
            ]
            """;

        formatter.FormatTraces(json);

        var output = writer.ToString();
        // Should show formatted durations (ms for fast, s for slow)
        Assert.Contains("duration1", output);
        Assert.Contains("duration2", output);
    }

    [Fact]
    public void FormatSingleTrace_SpanWithDestination_ShowsArrow()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            {
                "trace_id": "dest123",
                "title": "Request with Destination",
                "duration_ms": 100,
                "has_error": false,
                "spans": [
                    {
                        "span_id": "span1",
                        "name": "External Call",
                        "source": "frontend",
                        "destination": "external-api.com",
                        "duration_ms": 100
                    }
                ]
            }
            """;

        formatter.FormatSingleTrace(json);

        var output = writer.ToString();
        Assert.Contains("frontend", output);
        Assert.Contains("→", output);
        Assert.Contains("external-api.com", output);
    }

    [Fact]
    public void FormatSingleTrace_AttributesTruncated_ShowsMoreIndicator()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            {
                "trace_id": "attr123",
                "title": "Trace with Many Attributes",
                "duration_ms": 100,
                "has_error": false,
                "spans": [
                    {
                        "span_id": "span1",
                        "name": "Span with Attributes",
                        "source": "service",
                        "duration_ms": 100,
                        "attributes": {
                            "attr1": "value1",
                            "attr2": "value2",
                            "attr3": "value3",
                            "attr4": "value4",
                            "attr5": "value5"
                        }
                    }
                ]
            }
            """;

        formatter.FormatSingleTrace(json);

        var output = writer.ToString();
        Assert.Contains("+", output); // Shows "+N more" indicator
    }

    [Fact]
    public void FormatTraces_SuccessfulTrace_ShowsCheckMark()
    {
        var (formatter, writer) = CreateFormatter();
        var json = """
            [
                {
                    "trace_id": "success123",
                    "title": "Successful Request",
                    "duration_ms": 50,
                    "has_error": false,
                    "spans": []
                }
            ]
            """;

        formatter.FormatTraces(json);

        var output = writer.ToString();
        Assert.Contains("✓", output); // Success symbol
    }
}
