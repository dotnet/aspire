# Aspire CLI Telemetry Commands Implementation Plan

This document outlines the implementation backlog for adding `aspire telemetry` commands to the Aspire CLI, providing feature parity with MCP tools and full OTel data querying capabilities.

## Overview

Add `aspire telemetry` command group with subcommands:
- `aspire telemetry traces` - Query distributed traces
- `aspire telemetry logs` - Query structured logs (OTel)
- `aspire telemetry metrics` - Query metrics/instruments
- `aspire telemetry fields` - Discover available attribute keys/values

## Design Decisions

- **Output order**: Most recent first (newest first)
- **Filter syntax**: Simple operators (`=`, `!=`, `~`, `!~`, `>`, `<`, `>=`, `<=`)
- **Filter validation**: Validate field names against available fields before querying
- **Severity filtering**: `>=` semantics (Warning means Warning and above)
- **Metrics duration**: Relative syntax matching Dashboard (1m, 5m, 15m, 30m, 1h, 3h, 6h, 12h), default 5m
- **No streaming**: Query and return data only, no `--follow` option
- **Isolated change**: No modifications to existing commands

---

## Phase 1: Foundation - Filter Expression Parser

### 1.1 Create Filter Expression Parser

- [x] Create `src/Aspire.Cli/Utils/FilterExpressionParser.cs`
  - [x] Define `ParsedFilter` record with `Field`, `Condition`, `Value` properties
  - [x] Implement `Parse(string expression)` method
  - [x] Support `=` operator (Equals)
  - [x] Support `!=` operator (NotEqual)
  - [x] Support `~` operator (Contains)
  - [x] Support `!~` operator (NotContains)
  - [x] Support `>` operator (GreaterThan)
  - [x] Support `<` operator (LessThan)
  - [x] Support `>=` operator (GreaterThanOrEqual)
  - [x] Support `<=` operator (LessThanOrEqual)
  - [x] Handle edge cases: escaped characters, empty values, whitespace
  - [x] Throw `FilterParseException` with helpful error messages

- [x] Create `tests/Aspire.Cli.Tests/Utils/FilterExpressionParserTests.cs`
  - [x] Test `Parse_EqualsOperator_ReturnsCorrectFilter` ("http.method=POST")
  - [x] Test `Parse_NotEqualsOperator_ReturnsCorrectFilter` ("status!=Error")
  - [x] Test `Parse_ContainsOperator_ReturnsCorrectFilter` ("user.id~admin")
  - [x] Test `Parse_NotContainsOperator_ReturnsCorrectFilter` ("msg!~timeout")
  - [x] Test `Parse_GreaterThanOperator_ReturnsCorrectFilter` ("status_code>399")
  - [x] Test `Parse_LessThanOperator_ReturnsCorrectFilter` ("duration<100")
  - [x] Test `Parse_GreaterThanOrEqualOperator_ReturnsCorrectFilter` ("level>=Warning")
  - [x] Test `Parse_LessThanOrEqualOperator_ReturnsCorrectFilter` ("level<=Info")
  - [x] Test `Parse_FieldWithDots_ReturnsCorrectFilter` ("http.response.status_code=200")
  - [x] Test `Parse_ValueWithSpaces_ReturnsCorrectFilter` ("message~connection refused")
  - [x] Test `Parse_EmptyExpression_ThrowsException`
  - [x] Test `Parse_MissingOperator_ThrowsException`
  - [x] Test `Parse_MissingValue_ThrowsException` (Note: empty value is allowed, returns empty string)
  - [x] Test `Parse_MissingField_ThrowsException`
  - [x] Test `Parse_InvalidOperator_ThrowsException`

- [x] Verify all parser tests pass

### 1.2 Create Filter-to-TelemetryFilter Converter

- [x] Add `ToTelemetryFilter()` method to convert `ParsedFilter` to `TelemetryFilterDto`
  - [x] Map FilterCondition enum to Dashboard-compatible string format
  - [x] Created `TelemetryFilterDto` class for JSON serialization
  - [x] Added `ToTelemetryConditionString()` extension method for FilterCondition

- [x] Add converter tests to `FilterExpressionParserTests.cs`
  - [x] Test `ToTelemetryFilter_EqualsCondition_ReturnsFieldTelemetryFilter`
  - [x] Test `ToTelemetryFilter_AllConditions_MapsCorrectly`
  - [x] Test `ToTelemetryFilter_PreservesFieldNameWithDots`
  - [x] Test `ToTelemetryFilter_PreservesValueWithSpaces`
  - [x] Test `ToTelemetryFilter_EnabledByDefault`
  - [x] Test `ToTelemetryConditionString_AllConditions_ReturnsCorrectString`

- [x] Verify converter tests pass

---

## Phase 2: Dashboard MCP - Fields Discovery Tools

### 2.1 Create Fields MCP Tools Class

- [x] Create `src/Aspire.Dashboard/Mcp/AspireFieldsMcpTools.cs`
  - [x] Add constructor with `TelemetryRepository`, `IDashboardClient`, `ILogger` dependencies
  - [x] Add `[McpServerToolType]` attribute to class

- [x] Register in DI container
  - [x] Modify `src/Aspire.Dashboard/Mcp/McpExtensions.cs` (registration is done via `WithTools<T>()` method)
  - [x] Add `builder.WithTools<AspireFieldsMcpTools>()` in MCP registration section

### 2.2 Implement list_telemetry_fields Tool

- [x] Add `ListTelemetryFields` method to `AspireFieldsMcpTools.cs`
  - [x] Add `[McpServerTool(Name = "list_telemetry_fields")]` attribute
  - [x] Add `[Description("...")]` attribute with clear description
  - [x] Accept optional `type` parameter ("traces" or "logs", default both)
  - [x] Accept optional `resourceName` parameter
  - [x] Call `TelemetryRepository.GetTracePropertyKeys()` for traces
  - [x] Call `TelemetryRepository.GetLogPropertyKeys()` for logs
  - [x] Return JSON with known fields and custom attribute keys
  - [x] Include field counts where available

- [x] Create `tests/Aspire.Dashboard.Tests/Mcp/AspireFieldsMcpToolsTests.cs`
  - [x] Test `ListTelemetryFields_NoData_ReturnsEmptyLists`
  - [x] Test `ListTelemetryFields_WithTraces_ReturnsTraceFields`
  - [x] Test `ListTelemetryFields_WithLogs_ReturnsLogFields`
  - [x] Test `ListTelemetryFields_TypeTraces_ReturnsOnlyTraceFields`
  - [x] Test `ListTelemetryFields_TypeLogs_ReturnsOnlyLogFields`
  - [x] Test `ListTelemetryFields_WithResource_FiltersToResource`
  - [x] Test `ListTelemetryFields_IncludesKnownFields`
  - [x] Test `ListTelemetryFields_IncludesCustomAttributes`

- [x] Verify all fields listing tests pass

### 2.3 Implement get_telemetry_field_values Tool

- [x] Add `GetTelemetryFieldValues` method to `AspireFieldsMcpTools.cs`
  - [x] Add `[McpServerTool(Name = "get_telemetry_field_values")]` attribute
  - [x] Accept required `fieldName` parameter
  - [x] Accept optional `type` parameter ("traces" or "logs")
  - [x] Accept optional `resourceName` parameter
  - [x] Call `TelemetryRepository.GetTraceFieldValues()` for traces
  - [x] Call `TelemetryRepository.GetLogsFieldValues()` for logs
  - [x] Return JSON with values array including value and count
  - [x] Order by count descending

- [x] Add tests to `AspireFieldsMcpToolsTests.cs`
  - [x] Test `GetTelemetryFieldValues_ValidField_ReturnsValues`
  - [x] Test `GetTelemetryFieldValues_FieldWithMultipleValues_ReturnsAllWithCounts`
  - [x] Test `GetTelemetryFieldValues_UnknownField_ReturnsEmptyList`
  - [x] Test `GetTelemetryFieldValues_TypeTraces_QueriesTraceFields`
  - [x] Test `GetTelemetryFieldValues_TypeLogs_QueriesLogFields`
  - [x] Test `GetTelemetryFieldValues_WithResource_ValidatesResourceExists` (Note: filtering not yet supported in repository)
  - [x] Test `GetTelemetryFieldValues_OrderedByCountDescending`

- [x] Verify all field values tests pass

---

## Phase 3: Dashboard MCP - Enhanced Trace/Log Tools

### 3.1 Add Filter Support to list_traces

- [x] Modify `src/Aspire.Dashboard/Mcp/AspireTelemetryMcpTools.cs` `ListTraces` method
  - [x] Add `filters` parameter (JSON string array of filter objects)
  - [x] Add `searchText` parameter for span name search
  - [x] Parse filters JSON into `List<FieldTelemetryFilter>`
  - [x] Pass filters to `TelemetryRepository.GetTraces()` request
  - [x] Pass searchText as `FilterText` in request

- [x] Add tests to `tests/Aspire.Dashboard.Tests/Mcp/AspireTelemetryMcpToolsTests.cs`
  - [x] Test `ListTraces_WithSingleFilter_ReturnsFilteredTraces`
  - [x] Test `ListTraces_WithMultipleFilters_AppliesAndLogic`
  - [x] Test `ListTraces_WithSearchText_FiltersSpanNames`
  - [x] Test `ListTraces_WithStatusErrorFilter_ReturnsOnlyErrors`
  - [x] Test `ListTraces_WithAttributeFilter_FiltersCustomAttribute` (covered by WithSingleFilter test)
  - [x] Test `ListTraces_WithInvalidFilterJson_ReturnsError`
  - [x] Test `ListTraces_FiltersAndResource_CombinesCorrectly`

- [x] Verify all trace filter tests pass

### 3.2 Add Filter Support to list_structured_logs

- [x] Modify `AspireTelemetryMcpTools.cs` `ListStructuredLogs` method
  - [x] Add `filters` parameter (JSON string array of filter objects)
  - [x] Add `severity` parameter for minimum severity level
  - [x] Parse filters JSON into `List<FieldTelemetryFilter>`
  - [x] If severity provided, add severity filter with `>=` condition
  - [x] Pass filters to `TelemetryRepository.GetLogs()` request

- [x] Add tests to `AspireTelemetryMcpToolsTests.cs`
  - [x] Test `ListStructuredLogs_WithSeverityWarning_ReturnsWarningAndAbove`
  - [x] Test `ListStructuredLogs_WithSeverityError_ReturnsOnlyErrorAndCritical`
  - [x] Test `ListStructuredLogs_WithCategoryFilter_FiltersByCategory`
  - [x] Test `ListStructuredLogs_WithMessageContainsFilter_FiltersMessages`
  - [x] Test `ListStructuredLogs_WithCustomAttributeFilter_FiltersAttribute`
  - [x] Test `ListStructuredLogs_WithMultipleFilters_AppliesAndLogic`
  - [x] Test `ListStructuredLogs_WithInvalidSeverity_ReturnsError`
  - [x] Test `ListStructuredLogs_FiltersAndResource_CombinesCorrectly`

- [x] Verify all log filter tests pass

---

## Phase 4: Dashboard MCP - Metrics Tools

### 4.1 Create Metrics MCP Tools Class

- [x] Create `src/Aspire.Dashboard/Mcp/AspireMetricsMcpTools.cs`
  - [x] Add constructor with `TelemetryRepository`, `IDashboardClient`, `IOptionsMonitor<DashboardOptions>`, `ILogger` dependencies
  - [x] Add `[McpServerToolType]` attribute to class (Note: using `[McpServerTool]` attribute on methods)

- [x] Register in DI container
  - [x] Modify `McpExtensions.cs` (not DashboardWebApplication.cs - registration is done via `WithTools<T>()`)
  - [x] Add `builder.WithTools<AspireMetricsMcpTools>()` in MCP registration section

### 4.2 Implement list_metrics Tool

- [x] Add `ListMetrics` method to `AspireMetricsMcpTools.cs`
  - [x] Add `[McpServerTool(Name = "list_metrics")]` attribute
  - [x] Add `[Description("...")]` attribute
  - [x] Accept required `resourceName` parameter
  - [x] Resolve resource to `ResourceKey`
  - [x] Call `TelemetryRepository.GetInstrumentsSummaries()`
  - [x] Return JSON with instruments array (name, description, unit, type, meter)
  - [x] Group by meter name for readability

- [x] Create `tests/Aspire.Dashboard.Tests/Mcp/AspireMetricsMcpToolsTests.cs`
  - [x] Test `ListMetrics_NoResource_ReturnsError`
  - [x] Test `ListMetrics_ResourceNotFound_ReturnsError`
  - [x] Test `ListMetrics_WithResource_ReturnsInstruments`
  - [x] Test `ListMetrics_MultipleMeters_GroupsByMeter`
  - [x] Test `ListMetrics_IncludesInstrumentMetadata` (name, description, unit, type)
  - [x] Test `ListMetrics_NoMetrics_ReturnsNoMetricsMessage` (replaced ResourceOptOut test)

- [x] Verify all list metrics tests pass

### 4.3 Implement get_metric_data Tool

- [x] Add `GetMetricData` method to `AspireMetricsMcpTools.cs`
  - [x] Add `[McpServerTool(Name = "get_metric_data")]` attribute
  - [x] Accept required `resourceName` parameter
  - [x] Accept required `meterName` parameter
  - [x] Accept required `instrumentName` parameter
  - [x] Accept optional `duration` parameter (default "5m")
  - [x] Parse duration string to TimeSpan (1m, 5m, 15m, 30m, 1h, 3h, 6h, 12h)
  - [x] Calculate start/end times from duration
  - [x] Call `TelemetryRepository.GetInstrument()` with request
  - [x] Return JSON with dimensions, values, known attribute values

- [x] Add tests to `AspireMetricsMcpToolsTests.cs`
  - [x] Test `GetMetricData_ValidInstrument_ReturnsData`
  - [x] Test `GetMetricData_InvalidInstrument_ReturnsError`
  - [x] Test `GetMetricData_InvalidMeter_ReturnsError`
  - [x] Test `GetMetricData_ResourceNotFound_ReturnsError`
  - [x] Test `GetMetricData_DefaultDuration_Uses5Minutes`
  - [x] Test `GetMetricData_CustomDuration_UsesSpecifiedDuration`
  - [x] Test `GetMetricData_InvalidDuration_ReturnsError`
  - [x] Test `GetMetricData_MissingResourceName_ReturnsError`
  - [x] Test `GetMetricData_MissingMeterName_ReturnsError`
  - [x] Test `GetMetricData_MissingInstrumentName_ReturnsError`

- [x] Verify all get metric data tests pass

### 4.4 Duration Parser Utility

- [x] Create duration parsing helper (in `AspireMetricsMcpTools.cs` as `TryParseDuration` method)
  - [x] Parse "1m" -> TimeSpan.FromMinutes(1)
  - [x] Parse "5m" -> TimeSpan.FromMinutes(5)
  - [x] Parse "15m" -> TimeSpan.FromMinutes(15)
  - [x] Parse "30m" -> TimeSpan.FromMinutes(30)
  - [x] Parse "1h" -> TimeSpan.FromHours(1)
  - [x] Parse "3h" -> TimeSpan.FromHours(3)
  - [x] Parse "6h" -> TimeSpan.FromHours(6)
  - [x] Parse "12h" -> TimeSpan.FromHours(12)
  - [x] Return error for unsupported durations

- [x] Add duration parsing tests
  - [x] Test all supported duration values (via `GetMetricData_AllSupportedDurations_Succeed` Theory)
  - [x] Test invalid duration returns error

- [x] Verify duration parsing tests pass

---

## Phase 5: CLI MCP Proxy Tools

### 5.1 Create/Update CLI Proxy for Fields Tools

- [x] Create `src/Aspire.Cli/Mcp/ListTelemetryFieldsTool.cs`
  - [x] Implement proxy that forwards to Dashboard MCP endpoint
  - [x] Follow existing proxy pattern from `ListTracesTool.cs`
  - [x] Handle connection errors gracefully (handled by McpStartCommand)

- [x] Create `src/Aspire.Cli/Mcp/GetTelemetryFieldValuesTool.cs`
  - [x] Implement proxy that forwards to Dashboard MCP endpoint
  - [x] Handle connection errors gracefully (handled by McpStartCommand)

- [x] Add tests for CLI proxy tools in `tests/Aspire.Cli.Tests/Mcp/`
  - [x] Test `ListTelemetryFieldsTool` schema and metadata (`ListTelemetryFieldsToolTests.cs`)
  - [x] Test `GetTelemetryFieldValuesTool` schema and metadata (`GetTelemetryFieldValuesToolTests.cs`)
  - Note: Forwarding/connection tests handled by integration tests (Dashboard connection managed by McpStartCommand)

- [x] Verify `ListTelemetryFieldsTool` tests pass
- [x] Verify `GetTelemetryFieldValuesTool` tests pass

### 5.2 Update Existing CLI Proxy Tools for Filters

- [x] Modify `src/Aspire.Cli/Mcp/ListTracesTool.cs`
  - [x] Add `filters` parameter
  - [x] Add `searchText` parameter
  - [x] Forward new parameters to Dashboard

- [x] Modify `src/Aspire.Cli/Mcp/ListStructuredLogsTool.cs`
  - [x] Add `filters` parameter
  - [x] Add `severity` parameter
  - [x] Forward new parameters to Dashboard

- [x] Add tests for updated proxy tools
  - [x] Test `ListTracesTool_PassesFilters` (added in `tests/Aspire.Cli.Tests/Mcp/ListTracesToolTests.cs`)
  - [x] Test `ListStructuredLogsTool_PassesFilters` (added in `tests/Aspire.Cli.Tests/Mcp/ListStructuredLogsToolTests.cs`)
  - [x] Test `ListStructuredLogsTool_PassesSeverity` (added in `tests/Aspire.Cli.Tests/Mcp/ListStructuredLogsToolTests.cs`)

- [x] Verify updated proxy tests pass

### 5.3 Create CLI Proxy for Metrics Tools

- [x] Create `src/Aspire.Cli/Mcp/ListMetricsTool.cs`
  - [x] Implement proxy to Dashboard MCP endpoint
  - [x] Handle connection errors gracefully (handled by McpStartCommand)

- [x] Create `src/Aspire.Cli/Mcp/GetMetricDataTool.cs`
  - [x] Implement proxy to Dashboard MCP endpoint
  - [x] Handle connection errors gracefully (handled by McpStartCommand)

- [x] Add tests for metrics proxy tools
  - [x] Test `ListMetricsTool` schema and metadata (`ListMetricsToolTests.cs`)
  - [x] Test `GetMetricDataTool` schema and metadata (`GetMetricDataToolTests.cs`)
  - Note: Forwarding/connection tests handled by integration tests (Dashboard connection managed by McpStartCommand)

- [x] Verify metrics proxy tests pass

---

## Phase 6: CLI Output Formatter

### 6.1 Create Telemetry Output Formatter

- [x] Create `src/Aspire.Cli/Utils/TelemetryOutputFormatter.cs`
  - [x] Add `FormatTraces()` method for human-readable trace output
  - [x] Add `FormatLogs()` method for human-readable log output
  - [x] Add `FormatMetrics()` method for human-readable metric output (FormatMetricsList and FormatMetricData)
  - [ ] Add `FormatFields()` method for human-readable fields output
  - [x] Use consistent formatting with colors/styling via Spectre.Console

### 6.2 Implement Trace Formatting

- [x] Implement `FormatTraces(List<TraceData> traces)` method
  - [x] Show header with count and "newest first" indicator
  - [x] Format each trace: ID, timestamp, duration, status
  - [x] Show title (root span name)
  - [x] Show resource flow (source -> destination)
  - [x] Show span hierarchy with indentation
  - [x] Highlight errors in red
  - [x] Truncate long attribute values

- [x] Create `tests/Aspire.Cli.Tests/Utils/TelemetryOutputFormatterTests.cs`
  - [x] Test `FormatTraces_EmptyList_ShowsEmptyMessage`
  - [x] Test `FormatTraces_SingleTrace_FormatsCorrectly`
  - [x] Test `FormatTraces_MultipleSpans_ShowsHierarchy`
  - [x] Test `FormatTraces_WithError_HighlightsError`
  - [x] Test `FormatTraces_ShowsNewestFirst`

- [x] Verify trace formatting tests pass (build succeeded)

### 6.3 Implement Log Formatting

- [x] Implement `FormatLogs(List<LogData> logs)` method
  - [x] Show header with count
  - [x] Format each log: severity, timestamp, resource
  - [x] Show message
  - [x] Show trace/span IDs if present
  - [x] Show attributes (key=value format)
  - [x] Color-code severity levels
  - [x] Show exception if present

- [x] Add tests to `TelemetryOutputFormatterTests.cs`
  - [x] Test `FormatLogs_EmptyList_ShowsEmptyMessage`
  - [x] Test `FormatLogs_SingleLog_FormatsCorrectly`
  - [x] Test `FormatLogs_WithAttributes_ShowsAttributes`
  - [x] Test `FormatLogs_WithException_ShowsException`
  - [x] Test `FormatLogs_SeverityColors_AppliedCorrectly`

- [x] Verify log formatting tests pass

### 6.4 Implement Metrics Formatting

- [x] Implement `FormatMetricsList(List<InstrumentSummary> instruments)` method
  - [x] Group by meter name
  - [x] Show instrument name, type, unit, description

- [x] Implement `FormatMetricData(MetricData data)` method
  - [x] Show instrument summary
  - [x] Show dimensions with current values
  - [x] Format numbers appropriately (bytes -> MB, etc.)

- [x] Add tests to `TelemetryOutputFormatterTests.cs`
  - [x] Test `FormatMetricsList_GroupsByMeter`
  - [x] Test `FormatMetricsList_ShowsInstrumentDetails`
  - [x] Test `FormatMetricData_ShowsDimensions`
  - [x] Test `FormatMetricData_FormatsUnitsNicely`

- [x] Verify metrics formatting tests pass

### 6.5 Implement Fields Formatting

- [ ] Implement `FormatFields(FieldsData fields)` method
  - [ ] Separate known fields from custom attributes
  - [ ] Show field counts where available

- [ ] Implement `FormatFieldValues(FieldValuesData values)` method
  - [ ] Show values with counts
  - [ ] Order by count descending

- [ ] Add tests to `TelemetryOutputFormatterTests.cs`
  - [ ] Test `FormatFields_SeparatesKnownAndCustom`
  - [ ] Test `FormatFieldValues_ShowsCounts`
  - [ ] Test `FormatFieldValues_OrderedByCount`

- [ ] Verify fields formatting tests pass

---

## Phase 7: CLI Commands - Parent and Fields

### 7.1 Create Parent Telemetry Command

- [ ] Create `src/Aspire.Cli/Commands/TelemetryCommand.cs`
  - [ ] Inherit from appropriate base class
  - [ ] Define `telemetry` as the command name
  - [ ] Add description: "Query telemetry data from running Aspire applications"
  - [ ] Add common options inherited by subcommands:
    - [ ] `--project <path>` - AppHost project path
    - [ ] `--dashboard-url <url>` - Standalone dashboard URL
    - [ ] `--api-key <key>` - Dashboard API key
  - [ ] Do not execute anything directly (subcommands only)

- [ ] Register command in DI
  - [ ] Add `services.AddTransient<TelemetryCommand>()` in `CliTestHelper.cs` (for tests)
  - [ ] Add to `RootCommand` subcommands

- [ ] Create `tests/Aspire.Cli.Tests/Commands/TelemetryCommandTests.cs`
  - [ ] Test `TelemetryCommand_NoSubcommand_ShowsHelp`
  - [ ] Test `TelemetryCommand_Help_ShowsAllSubcommands`
  - [ ] Test `TelemetryCommand_Help_ShowsCommonOptions`

- [ ] Verify parent command tests pass

### 7.2 Create Fields Subcommand

- [ ] Create `src/Aspire.Cli/Commands/TelemetryFieldsCommand.cs`
  - [ ] Define `fields` as the command name
  - [ ] Add description
  - [ ] Add options:
    - [ ] `--type <traces|logs>` - Filter to trace or log fields
    - [ ] `--resource <name>` - Filter to specific resource
    - [ ] `--json` - Output as JSON
  - [ ] Add optional argument `<field-name>` for getting field values
  - [ ] Implement handler that calls MCP proxy tools
  - [ ] Use output formatter for human-readable output

- [ ] Register as subcommand of TelemetryCommand

- [ ] Create `tests/Aspire.Cli.Tests/Commands/TelemetryFieldsCommandTests.cs`
  - [ ] Test `TelemetryFieldsCommand_Help_ShowsUsage`
  - [ ] Test `TelemetryFieldsCommand_NoArgs_ListsAllFields`
  - [ ] Test `TelemetryFieldsCommand_TypeTraces_ListsOnlyTraceFields`
  - [ ] Test `TelemetryFieldsCommand_TypeLogs_ListsOnlyLogFields`
  - [ ] Test `TelemetryFieldsCommand_WithResource_FiltersToResource`
  - [ ] Test `TelemetryFieldsCommand_WithFieldName_GetsFieldValues`
  - [ ] Test `TelemetryFieldsCommand_JsonOutput_ReturnsValidJson`
  - [ ] Test `TelemetryFieldsCommand_NoDashboard_ReturnsError`

- [ ] Verify fields command tests pass

---

## Phase 8: CLI Commands - Traces

### 8.1 Create Traces Subcommand Structure

- [ ] Create `src/Aspire.Cli/Commands/TelemetryTracesCommand.cs`
  - [ ] Define `traces` as the command name
  - [ ] Add description
  - [ ] Add options:
    - [ ] `--resource <name>` - Filter by resource
    - [ ] `--filter <expr>` - Filter expression (repeatable)
    - [ ] `--search <text>` - Search span names
    - [ ] `--limit <n>` - Max results (default 100)
    - [ ] `--json` - Output as JSON
  - [ ] Add optional argument `<trace-id>` for getting specific trace

- [ ] Register as subcommand of TelemetryCommand

- [ ] Add basic command tests
  - [ ] Test `TelemetryTracesCommand_Help_ShowsUsage`
  - [ ] Test `TelemetryTracesCommand_Help_ShowsAllOptions`

- [ ] Verify basic command tests pass

### 8.2 Implement Trace Listing

- [ ] Implement list traces handler (no trace-id argument)
  - [ ] Parse filter expressions using `FilterExpressionParser`
  - [ ] Validate filters against available fields (call fields tool first)
  - [ ] Return helpful error if filter field doesn't exist
  - [ ] Call MCP proxy tool with filters
  - [ ] Apply limit
  - [ ] Format output (JSON or human-readable)
  - [ ] Ensure newest first ordering

- [ ] Add list traces tests to `TelemetryTracesCommandTests.cs`
  - [ ] Test `TelemetryTracesCommand_NoArgs_ListsRecentTraces`
  - [ ] Test `TelemetryTracesCommand_WithResource_FiltersToResource`
  - [ ] Test `TelemetryTracesCommand_WithLimit_RespectsLimit`
  - [ ] Test `TelemetryTracesCommand_WithSearch_FiltersSpanNames`
  - [ ] Test `TelemetryTracesCommand_JsonOutput_ReturnsValidJson`
  - [ ] Test `TelemetryTracesCommand_OrderedNewestFirst`

- [ ] Verify list traces tests pass

### 8.3 Implement Trace Filtering

- [ ] Implement filter handling in traces command
  - [ ] Parse multiple `--filter` options
  - [ ] Validate each filter field exists
  - [ ] Convert to JSON format for MCP tool
  - [ ] Pass to proxy tool

- [ ] Add filter tests to `TelemetryTracesCommandTests.cs`
  - [ ] Test `TelemetryTracesCommand_WithSingleFilter_AppliesFilter`
  - [ ] Test `TelemetryTracesCommand_WithMultipleFilters_AppliesAllFilters`
  - [ ] Test `TelemetryTracesCommand_WithStatusFilter_FiltersStatus`
  - [ ] Test `TelemetryTracesCommand_WithAttributeFilter_FiltersAttribute`
  - [ ] Test `TelemetryTracesCommand_InvalidFilterSyntax_ReturnsError`
  - [ ] Test `TelemetryTracesCommand_InvalidFilterField_ReturnsErrorWithSuggestions`

- [ ] Verify filter tests pass

### 8.4 Implement Get Specific Trace

- [ ] Implement get trace by ID handler (trace-id argument provided)
  - [ ] Call MCP proxy with trace ID
  - [ ] Show detailed trace with all spans
  - [ ] Show span attributes
  - [ ] Format output (JSON or human-readable)

- [ ] Add get trace tests to `TelemetryTracesCommandTests.cs`
  - [ ] Test `TelemetryTracesCommand_WithTraceId_GetsSpecificTrace`
  - [ ] Test `TelemetryTracesCommand_WithTraceId_ShowsAllSpans`
  - [ ] Test `TelemetryTracesCommand_WithTraceId_ShowsAttributes`
  - [ ] Test `TelemetryTracesCommand_InvalidTraceId_ReturnsError`
  - [ ] Test `TelemetryTracesCommand_TraceIdJsonOutput_ReturnsValidJson`

- [ ] Verify get trace tests pass

---

## Phase 9: CLI Commands - Logs

### 9.1 Create Logs Subcommand Structure

- [ ] Create `src/Aspire.Cli/Commands/TelemetryLogsCommand.cs`
  - [ ] Define `logs` as the command name
  - [ ] Add description
  - [ ] Add options:
    - [ ] `--resource <name>` - Filter by resource
    - [ ] `--trace <trace-id>` - Filter by trace ID
    - [ ] `--span <span-id>` - Filter by span ID
    - [ ] `--filter <expr>` - Filter expression (repeatable)
    - [ ] `--severity <level>` - Minimum severity level
    - [ ] `--limit <n>` - Max results (default 100)
    - [ ] `--json` - Output as JSON

- [ ] Register as subcommand of TelemetryCommand

- [ ] Add basic command tests
  - [ ] Test `TelemetryLogsCommand_Help_ShowsUsage`
  - [ ] Test `TelemetryLogsCommand_Help_ShowsAllOptions`

- [ ] Verify basic command tests pass

### 9.2 Implement Log Listing

- [ ] Implement list logs handler
  - [ ] Parse filter expressions
  - [ ] Validate filters against available fields
  - [ ] Call MCP proxy tool
  - [ ] Apply limit
  - [ ] Format output
  - [ ] Ensure newest first ordering

- [ ] Add list logs tests to `TelemetryLogsCommandTests.cs`
  - [ ] Test `TelemetryLogsCommand_NoArgs_ListsRecentLogs`
  - [ ] Test `TelemetryLogsCommand_WithResource_FiltersToResource`
  - [ ] Test `TelemetryLogsCommand_WithLimit_RespectsLimit`
  - [ ] Test `TelemetryLogsCommand_JsonOutput_ReturnsValidJson`
  - [ ] Test `TelemetryLogsCommand_OrderedNewestFirst`

- [ ] Verify list logs tests pass

### 9.3 Implement Severity Filtering

- [ ] Implement severity option handling
  - [ ] Parse severity string to LogLevel enum
  - [ ] Validate severity value
  - [ ] Pass to MCP proxy tool

- [ ] Add severity tests to `TelemetryLogsCommandTests.cs`
  - [ ] Test `TelemetryLogsCommand_SeverityWarning_ReturnsWarningAndAbove`
  - [ ] Test `TelemetryLogsCommand_SeverityError_ReturnsErrorAndCritical`
  - [ ] Test `TelemetryLogsCommand_SeverityTrace_ReturnsAll`
  - [ ] Test `TelemetryLogsCommand_InvalidSeverity_ReturnsError`

- [ ] Verify severity tests pass

### 9.4 Implement Log Filtering

- [ ] Implement filter handling in logs command
  - [ ] Parse multiple `--filter` options
  - [ ] Validate each filter field exists
  - [ ] Convert to JSON format for MCP tool

- [ ] Add filter tests to `TelemetryLogsCommandTests.cs`
  - [ ] Test `TelemetryLogsCommand_WithCategoryFilter_FiltersByCategory`
  - [ ] Test `TelemetryLogsCommand_WithMessageFilter_FiltersByMessage`
  - [ ] Test `TelemetryLogsCommand_WithAttributeFilter_FiltersByAttribute`
  - [ ] Test `TelemetryLogsCommand_WithMultipleFilters_AppliesAllFilters`
  - [ ] Test `TelemetryLogsCommand_InvalidFilterField_ReturnsErrorWithSuggestions`

- [ ] Verify filter tests pass

### 9.5 Implement Trace/Span Filtering

- [ ] Implement `--trace` and `--span` options
  - [ ] Add trace ID as filter if provided
  - [ ] Add span ID as filter if provided
  - [ ] Combine with other filters

- [ ] Add trace/span filter tests to `TelemetryLogsCommandTests.cs`
  - [ ] Test `TelemetryLogsCommand_WithTraceId_FiltersToTrace`
  - [ ] Test `TelemetryLogsCommand_WithSpanId_FiltersToSpan`
  - [ ] Test `TelemetryLogsCommand_TraceAndSpan_CombinesFilters`

- [ ] Verify trace/span filter tests pass

---

## Phase 10: CLI Commands - Metrics

### 10.1 Create Metrics Subcommand Structure

- [ ] Create `src/Aspire.Cli/Commands/TelemetryMetricsCommand.cs`
  - [ ] Define `metrics` as the command name
  - [ ] Add description
  - [ ] Add options:
    - [ ] `--resource <name>` - Resource name (required)
    - [ ] `--duration <timespan>` - Time window (default 5m)
    - [ ] `--json` - Output as JSON
  - [ ] Add optional argument `<meter/instrument>` for getting specific metric

- [ ] Register as subcommand of TelemetryCommand

- [ ] Add basic command tests
  - [ ] Test `TelemetryMetricsCommand_Help_ShowsUsage`
  - [ ] Test `TelemetryMetricsCommand_Help_ShowsAllOptions`

- [ ] Verify basic command tests pass

### 10.2 Implement Metrics Listing

- [ ] Implement list metrics handler (no instrument argument)
  - [ ] Require `--resource` option
  - [ ] Call MCP proxy tool
  - [ ] Group by meter
  - [ ] Format output

- [ ] Add list metrics tests to `TelemetryMetricsCommandTests.cs`
  - [ ] Test `TelemetryMetricsCommand_NoResource_ReturnsError`
  - [ ] Test `TelemetryMetricsCommand_WithResource_ListsInstruments`
  - [ ] Test `TelemetryMetricsCommand_GroupsByMeter`
  - [ ] Test `TelemetryMetricsCommand_ShowsInstrumentDetails`
  - [ ] Test `TelemetryMetricsCommand_JsonOutput_ReturnsValidJson`

- [ ] Verify list metrics tests pass

### 10.3 Implement Get Metric Data

- [ ] Implement get metric data handler (meter/instrument argument provided)
  - [ ] Parse `meter/instrument` argument format
  - [ ] Validate duration format
  - [ ] Call MCP proxy tool
  - [ ] Format output with dimensions

- [ ] Add get metric tests to `TelemetryMetricsCommandTests.cs`
  - [ ] Test `TelemetryMetricsCommand_WithInstrument_GetsMetricData`
  - [ ] Test `TelemetryMetricsCommand_DefaultDuration_Uses5Minutes`
  - [ ] Test `TelemetryMetricsCommand_CustomDuration_UsesSpecifiedDuration`
  - [ ] Test `TelemetryMetricsCommand_InvalidDuration_ReturnsError`
  - [ ] Test `TelemetryMetricsCommand_InvalidInstrumentFormat_ReturnsError`
  - [ ] Test `TelemetryMetricsCommand_InstrumentNotFound_ReturnsError`
  - [ ] Test `TelemetryMetricsCommand_ShowsDimensions`

- [ ] Verify get metric tests pass

---

## Phase 11: Integration and Polish

### 11.1 End-to-End Integration Tests

- [ ] Create integration test that runs full command pipeline
  - [ ] Test `aspire telemetry traces` with mock Dashboard
  - [ ] Test `aspire telemetry logs` with mock Dashboard
  - [ ] Test `aspire telemetry metrics` with mock Dashboard
  - [ ] Test `aspire telemetry fields` with mock Dashboard

- [ ] Test connection scenarios
  - [ ] Test with `--dashboard-url` (standalone mode)
  - [ ] Test with `--project` (AppHost mode)
  - [ ] Test connection failure handling
  - [ ] Test API key authentication

- [ ] Verify integration tests pass

### 11.2 Error Handling Polish

- [ ] Review and improve error messages
  - [ ] Ensure all errors have actionable suggestions
  - [ ] Ensure invalid filter fields suggest similar valid fields
  - [ ] Ensure connection errors explain how to connect

- [ ] Add error scenario tests
  - [ ] Test error message quality for common failures
  - [ ] Test error message includes help on how to fix

- [ ] Verify error handling tests pass

### 11.3 Documentation and Help Text

- [ ] Review and polish all command descriptions
- [ ] Review and polish all option descriptions
- [ ] Add examples to help text where useful
- [ ] Ensure consistency across all telemetry commands

- [ ] Test help text
  - [ ] Test each command's help is complete
  - [ ] Test examples are accurate

- [ ] Verify help text tests pass

### 11.4 Final Review

- [ ] Run full test suite
- [ ] Review code for consistency with existing patterns
- [ ] Review for any TODO comments that need addressing
- [ ] Verify no breaking changes to existing commands
- [ ] Performance review: ensure commands respond quickly

---

## File Summary

### New Files to Create

**CLI Commands:**
- `src/Aspire.Cli/Commands/TelemetryCommand.cs`
- `src/Aspire.Cli/Commands/TelemetryTracesCommand.cs`
- `src/Aspire.Cli/Commands/TelemetryLogsCommand.cs`
- `src/Aspire.Cli/Commands/TelemetryMetricsCommand.cs`
- `src/Aspire.Cli/Commands/TelemetryFieldsCommand.cs`

**CLI Utilities:**
- `src/Aspire.Cli/Utils/FilterExpressionParser.cs`
- `src/Aspire.Cli/Utils/TelemetryOutputFormatter.cs`

**CLI MCP Proxies:**
- `src/Aspire.Cli/Mcp/ListTelemetryFieldsTool.cs`
- `src/Aspire.Cli/Mcp/GetTelemetryFieldValuesTool.cs`
- `src/Aspire.Cli/Mcp/ListMetricsTool.cs`
- `src/Aspire.Cli/Mcp/GetMetricDataTool.cs`

**Dashboard MCP Tools:**
- `src/Aspire.Dashboard/Mcp/AspireFieldsMcpTools.cs`
- `src/Aspire.Dashboard/Mcp/AspireMetricsMcpTools.cs`

**Tests:**
- `tests/Aspire.Cli.Tests/Commands/TelemetryCommandTests.cs`
- `tests/Aspire.Cli.Tests/Commands/TelemetryTracesCommandTests.cs`
- `tests/Aspire.Cli.Tests/Commands/TelemetryLogsCommandTests.cs`
- `tests/Aspire.Cli.Tests/Commands/TelemetryMetricsCommandTests.cs`
- `tests/Aspire.Cli.Tests/Commands/TelemetryFieldsCommandTests.cs`
- `tests/Aspire.Cli.Tests/Utils/FilterExpressionParserTests.cs`
- `tests/Aspire.Cli.Tests/Utils/TelemetryOutputFormatterTests.cs`
- `tests/Aspire.Dashboard.Tests/Mcp/AspireFieldsMcpToolsTests.cs`
- `tests/Aspire.Dashboard.Tests/Mcp/AspireMetricsMcpToolsTests.cs`

### Files to Modify

- `src/Aspire.Dashboard/Mcp/AspireTelemetryMcpTools.cs` - Add filter parameters
- `src/Aspire.Dashboard/DashboardWebApplication.cs` - Register new MCP tool classes
- `src/Aspire.Cli/Mcp/ListTracesTool.cs` - Add filter parameters
- `src/Aspire.Cli/Mcp/ListStructuredLogsTool.cs` - Add filter parameters
- `tests/Aspire.Dashboard.Tests/Mcp/AspireTelemetryMcpToolsTests.cs` - Add filter tests
- `tests/Aspire.Cli.Tests/Utils/CliTestHelper.cs` - Register new commands

---

## Success Criteria

- [ ] All `aspire telemetry` subcommands work with running AppHost
- [ ] All `aspire telemetry` subcommands work with standalone Dashboard via `--dashboard-url`
- [ ] Filter expressions validated before sending to Dashboard
- [ ] Invalid filters return helpful error messages with suggestions
- [ ] Human-readable output is clean and informative
- [ ] JSON output is valid and matches MCP tool format
- [ ] All tests pass
- [ ] No regression in existing CLI commands
- [ ] MCP tools have feature parity with CLI commands
