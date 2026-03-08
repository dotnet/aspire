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
