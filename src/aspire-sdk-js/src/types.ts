// Types matching the `aspire describe --format json` output schema.

/** Endpoint exposed by a resource. */
export interface Endpoint {
  name: string;
  displayName?: string;
  url: string;
  isInternal?: boolean;
}

/** Volume mounted by a resource. */
export interface Volume {
  source: string;
  target: string;
  mountType: string;
  isReadOnly?: boolean;
}

/** Health report for a resource. */
export interface HealthReport {
  status: string;
  description?: string;
  exceptionMessage?: string;
}

/** Relationship between resources. */
export interface Relationship {
  type: string;
  resourceName: string;
}

/** Command available on a resource. */
export interface ResourceCommand {
  description: string;
}

/** A resource snapshot from `aspire describe --format json`. */
export interface ResourceSnapshot {
  name: string;
  displayName?: string;
  uid?: string;
  resourceType: string;
  state: string | null;
  stateStyle?: string;
  creationTimestamp?: string;
  startTimestamp?: string;
  stopTimestamp?: string;
  source?: string;
  exitCode?: number;
  healthStatus?: string | null;
  dashboardUrl?: string;
  relationships: Relationship[];
  urls: Endpoint[];
  volumes: Volume[];
  properties: Record<string, string | null>;
  environment: Record<string, string | null>;
  healthReports: Record<string, HealthReport>;
  commands: Record<string, ResourceCommand>;
}

/** Wrapper for `aspire describe --format json` output. */
export interface DescribeOutput {
  resources: ResourceSnapshot[];
}

/** Output from `aspire start --format json`. */
export interface StartOutput {
  appHostPath: string;
  appHostPid: number;
  cliPid: number;
  dashboardUrl: string | null;
  logFile: string;
}

/** A single log line from `aspire logs --format json`. */
export interface LogEntry {
  resourceName: string;
  timestamp?: string;
  content: string;
  isError: boolean;
}

/** Wrapper for `aspire logs --format json` snapshot output. */
export interface LogsOutput {
  logs: LogEntry[];
}

/** Options for starting an AppHost. */
export interface StartOptions {
  /** Path to the AppHost project file or directory. */
  appHost: string;
  /** Additional arguments to pass to the AppHost. */
  args?: string[];
  /** Don't build before starting. */
  noBuild?: boolean;
  /** Run with randomized ports and isolated user secrets, allowing multiple instances simultaneously. */
  isolated?: boolean;
}

/** Options for waiting on a resource. */
export interface WaitOptions {
  /** Target status: "healthy", "up", or "down". Default: "healthy". */
  status?: 'healthy' | 'up' | 'down';
  /** Timeout in seconds. Default: 120. */
  timeout?: number;
}

/** Options for retrieving logs. */
export interface LogOptions {
  /** Number of lines from the end. */
  tail?: number;
  /** Include timestamps. */
  timestamps?: boolean;
}

// ---------------------------------------------------------------------------
// OpenTelemetry types (OTLP JSON format)
// ---------------------------------------------------------------------------

/** Options for querying OTEL data. */
export interface OtelQueryOptions {
  /** Filter by resource name. */
  resource?: string;
  /** Maximum number of items to return. */
  limit?: number;
  /** Filter by trace ID. */
  traceId?: string;
  /** Filter by error status. */
  hasError?: boolean;
}

/** Options for querying OTEL structured logs. */
export interface OtelLogQueryOptions extends OtelQueryOptions {
  /** Filter by minimum severity. */
  severity?: 'Trace' | 'Debug' | 'Information' | 'Warning' | 'Error' | 'Critical';
}

/** OTLP key-value attribute. */
export interface OtlpKeyValue {
  key: string;
  value: OtlpAnyValue;
}

/** OTLP polymorphic value. */
export interface OtlpAnyValue {
  stringValue?: string;
  boolValue?: boolean;
  intValue?: string;
  doubleValue?: number;
  arrayValue?: { values: OtlpAnyValue[] };
  kvlistValue?: { values: OtlpKeyValue[] };
  bytesValue?: string;
}

/** OTLP resource metadata. */
export interface OtlpResource {
  attributes?: OtlpKeyValue[];
}

/** OTLP instrumentation scope. */
export interface OtlpInstrumentationScope {
  name?: string;
  version?: string;
}

/** OTLP span. */
export interface OtlpSpan {
  traceId: string;
  spanId: string;
  parentSpanId?: string;
  name: string;
  kind?: number;
  startTimeUnixNano: string;
  endTimeUnixNano: string;
  attributes?: OtlpKeyValue[];
  status?: { message?: string; code?: number };
  events?: Array<{ timeUnixNano: string; name: string; attributes?: OtlpKeyValue[] }>;
  links?: Array<{ traceId: string; spanId: string; attributes?: OtlpKeyValue[] }>;
}

/** OTLP log record. */
export interface OtlpLogRecord {
  timeUnixNano?: string;
  severityNumber?: number;
  severityText?: string;
  body?: OtlpAnyValue;
  attributes?: OtlpKeyValue[];
  traceId?: string;
  spanId?: string;
}

/** Telemetry API response wrapper. */
export interface TelemetryResponse {
  data: {
    resourceSpans?: Array<{
      resource?: OtlpResource;
      scopeSpans?: Array<{
        scope?: OtlpInstrumentationScope;
        spans?: OtlpSpan[];
      }>;
    }>;
    resourceLogs?: Array<{
      resource?: OtlpResource;
      scopeLogs?: Array<{
        scope?: OtlpInstrumentationScope;
        logRecords?: OtlpLogRecord[];
      }>;
    }>;
  };
  totalCount: number;
  returnedCount: number;
}

// ---------------------------------------------------------------------------
// Process list types
// ---------------------------------------------------------------------------

/** A running AppHost from `aspire ps --format json`. */
export interface AppHostInfo {
  appHostPath: string;
  appHostPid: number;
  cliPid?: number;
  dashboardUrl?: string;
  resources?: ResourceSnapshot[];
}

// ---------------------------------------------------------------------------
// Export types
// ---------------------------------------------------------------------------

/** Options for exporting telemetry data. */
export interface ExportOptions {
  /** Export data only for the specified resource. */
  resource?: string;
  /** Output file path for the zip. */
  output?: string;
}
