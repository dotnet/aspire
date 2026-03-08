import { spawn, type ChildProcess } from 'child_process';
import { aspireExec, aspireJson } from './cli.js';
import type {
  StartOptions,
  StartOutput,
  DescribeOutput,
  ResourceSnapshot,
  Endpoint,
  LogEntry,
  LogsOutput,
  LogOptions,
  WaitOptions,
  OtelQueryOptions,
  OtelLogQueryOptions,
  TelemetryResponse,
  AppHostInfo,
  ExportOptions,
} from './types.js';

/**
 * A high-level API for automating the Aspire CLI.
 *
 * Use `AspireHost.start()` to launch an AppHost, then interact with its
 * resources, wait for health, collect logs, and stop it when done.
 *
 * @example
 * ```ts
 * const host = await AspireHost.start({ appHost: './apphost.ts' });
 * await host.waitForResource('api', { status: 'healthy' });
 *
 * const endpoint = await host.getEndpoint('api', 'http');
 * const res = await fetch(`${endpoint.url}/healthz`);
 *
 * await host.stop();
 * ```
 */
export class AspireHost {
  private appHost: string;

  /** Information returned by `aspire start --format json`. */
  readonly info: StartOutput;

  private constructor(appHost: string, info: StartOutput) {
    this.appHost = appHost;
    this.info = info;
  }

  // ---------------------------------------------------------------------------
  // Lifecycle
  // ---------------------------------------------------------------------------

  /** Start an AppHost in the background and return an `AspireHost` handle. */
  static async start(options: StartOptions): Promise<AspireHost> {
    const args = ['start'];
    if (options.noBuild) {
      args.push('--no-build');
    }
    if (options.isolated) {
      args.push('--isolated');
    }
    if (options.args?.length) {
      args.push('--', ...options.args);
    }

    const info = await aspireJson<StartOutput>(args, {
      appHost: options.appHost,
    });

    return new AspireHost(options.appHost, info);
  }

  /** Stop the running AppHost. */
  async stop(): Promise<void> {
    await aspireExec(['stop'], { appHost: this.appHost });
  }

  // ---------------------------------------------------------------------------
  // Resources
  // ---------------------------------------------------------------------------

  /** Get a snapshot of all resources. */
  async getResources(): Promise<ResourceSnapshot[]> {
    const output = await aspireJson<DescribeOutput>(['describe'], {
      appHost: this.appHost,
    });
    return output.resources;
  }

  /** Get a single resource by name. Throws if not found. */
  async getResource(name: string): Promise<ResourceSnapshot> {
    const output = await aspireJson<DescribeOutput>(['describe', name], {
      appHost: this.appHost,
    });

    if (!output.resources.length) {
      throw new Error(`Resource '${name}' not found`);
    }

    return output.resources[0];
  }

  /**
   * Get an endpoint URL for a resource.
   * If `endpointName` is omitted, returns the first endpoint.
   */
  async getEndpoint(resourceName: string, endpointName?: string): Promise<Endpoint> {
    const resource = await this.getResource(resourceName);
    const endpoints = resource.urls;

    if (!endpoints.length) {
      throw new Error(`Resource '${resourceName}' has no endpoints`);
    }

    if (endpointName) {
      const endpoint = endpoints.find(e => e.name === endpointName);
      if (!endpoint) {
        const available = endpoints.map(e => e.name).join(', ');
        throw new Error(
          `Endpoint '${endpointName}' not found on '${resourceName}'. Available: ${available}`
        );
      }
      return endpoint;
    }

    return endpoints[0];
  }

  // ---------------------------------------------------------------------------
  // Waiting
  // ---------------------------------------------------------------------------

  /**
   * Wait for a resource to reach a target status using `aspire wait`.
   * Defaults to waiting for "healthy" with a 120s timeout.
   */
  async waitForResource(name: string, options: WaitOptions = {}): Promise<void> {
    const args = ['wait', name];
    if (options.status) {
      args.push('--status', options.status);
    }
    if (options.timeout !== undefined) {
      args.push('--timeout', String(options.timeout));
    }

    await aspireExec(args, { appHost: this.appHost });
  }

  /**
   * Watch resource changes in real-time via `aspire describe --follow`.
   * Pass an `AbortSignal` to stop watching.
   *
   * @example
   * ```ts
   * const ac = new AbortController();
   * for await (const snapshot of host.watchResources({ signal: ac.signal })) {
   *   if (snapshot.state === 'Running') ac.abort();
   * }
   * ```
   */
  async *watchResources(options: { signal?: AbortSignal } = {}): AsyncGenerator<ResourceSnapshot> {
    yield* this.streamCommand<ResourceSnapshot>(['describe', '--follow'], options.signal);
  }

  // ---------------------------------------------------------------------------
  // Logs
  // ---------------------------------------------------------------------------

  /** Get logs for a resource (or all resources if name omitted). */
  async getLogs(resourceName?: string, options: LogOptions = {}): Promise<LogEntry[]> {
    const args = ['logs'];
    if (resourceName) {
      args.push(resourceName);
    }
    if (options.tail !== undefined) {
      args.push('--tail', String(options.tail));
    }
    if (options.timestamps) {
      args.push('--timestamps');
    }

    const output = await aspireJson<LogsOutput>(args, {
      appHost: this.appHost,
    });
    return output.logs;
  }

  /**
   * Stream logs in real-time via `aspire logs --follow`.
   * Pass an `AbortSignal` to stop streaming.
   *
   * @example
   * ```ts
   * const ac = new AbortController();
   * setTimeout(() => ac.abort(), 10_000);
   *
   * for await (const entry of host.streamLogs('api', { signal: ac.signal })) {
   *   console.log(`[${entry.resourceName}] ${entry.content}`);
   * }
   * ```
   */
  async *streamLogs(resourceName?: string, options: { signal?: AbortSignal } = {}): AsyncGenerator<LogEntry> {
    const args = ['logs', '--follow'];
    if (resourceName) {
      args.splice(1, 0, resourceName);
    }

    yield* this.streamCommand<LogEntry>(args, options.signal);
  }

  // ---------------------------------------------------------------------------
  // Resource Commands
  // ---------------------------------------------------------------------------

  /** Execute a command on a resource (e.g. "restart", "stop", "start"). */
  async executeCommand(resourceName: string, command: string): Promise<void> {
    await aspireExec(['resource', resourceName, command], {
      appHost: this.appHost,
    });
  }

  // ---------------------------------------------------------------------------
  // OpenTelemetry
  // ---------------------------------------------------------------------------

  /** Get OTEL traces from the dashboard telemetry API. */
  async getTraces(options: OtelQueryOptions = {}): Promise<TelemetryResponse> {
    return aspireJson<TelemetryResponse>(
      this.buildOtelArgs('traces', options),
      { appHost: this.appHost },
    );
  }

  /** Get OTEL spans from the dashboard telemetry API. */
  async getSpans(options: OtelQueryOptions = {}): Promise<TelemetryResponse> {
    return aspireJson<TelemetryResponse>(
      this.buildOtelArgs('spans', options),
      { appHost: this.appHost },
    );
  }

  /**
   * Stream OTEL spans in real-time via `aspire otel spans --follow`.
   * Pass an `AbortSignal` to stop streaming.
   */
  async *streamSpans(options: OtelQueryOptions & { signal?: AbortSignal } = {}): AsyncGenerator<TelemetryResponse> {
    const { signal, ...queryOptions } = options;
    const args = [...this.buildOtelArgs('spans', queryOptions), '--follow'];
    yield* this.streamCommand<TelemetryResponse>(args, signal);
  }

  /** Get OTEL structured logs from the dashboard telemetry API. */
  async getStructuredLogs(options: OtelLogQueryOptions = {}): Promise<TelemetryResponse> {
    const args = this.buildOtelArgs('logs', options);
    if (options.severity) {
      args.push('--severity', options.severity);
    }
    return aspireJson<TelemetryResponse>(args, { appHost: this.appHost });
  }

  /**
   * Stream OTEL structured logs in real-time via `aspire otel logs --follow`.
   * Pass an `AbortSignal` to stop streaming.
   */
  async *streamStructuredLogs(options: OtelLogQueryOptions & { signal?: AbortSignal } = {}): AsyncGenerator<TelemetryResponse> {
    const { signal, ...queryOptions } = options;
    const args = [...this.buildOtelArgs('logs', queryOptions), '--follow'];
    if (queryOptions.severity) {
      args.push('--severity', queryOptions.severity);
    }
    yield* this.streamCommand<TelemetryResponse>(args, signal);
  }

  private buildOtelArgs(subcommand: string, options: OtelQueryOptions): string[] {
    const args = ['otel', subcommand];
    if (options.resource) {
      args.push(options.resource);
    }
    if (options.limit !== undefined) {
      args.push('--limit', String(options.limit));
    }
    if (options.traceId) {
      args.push('--trace-id', options.traceId);
    }
    if (options.hasError !== undefined) {
      args.push('--has-error');
    }
    return args;
  }

  // ---------------------------------------------------------------------------
  // Export
  // ---------------------------------------------------------------------------

  /** Export telemetry and resource data to a zip file. */
  async export(options: ExportOptions = {}): Promise<string> {
    const args = ['export'];
    if (options.resource) {
      args.push(options.resource);
    }
    if (options.output) {
      args.push('--output', options.output);
    }
    return aspireExec(args, { appHost: this.appHost });
  }

  // ---------------------------------------------------------------------------
  // Static utilities (no running AppHost needed)
  // ---------------------------------------------------------------------------

  /** List all running AppHosts. */
  static async list(options: { resources?: boolean } = {}): Promise<AppHostInfo[]> {
    const args = ['ps'];
    if (options.resources) {
      args.push('--resources');
    }
    return aspireJson<AppHostInfo[]>(args);
  }

  // ---------------------------------------------------------------------------
  // Private helpers
  // ---------------------------------------------------------------------------

  /**
   * Spawn an aspire CLI command that streams NDJSON, yielding parsed objects.
   * Cleans up the child process when the signal aborts or the stream ends.
   */
  private async *streamCommand<T>(command: string[], signal?: AbortSignal): AsyncGenerator<T> {
    const args = [
      ...command,
      '--non-interactive', '--nologo',
      '--format', 'Json',
      '--apphost', this.appHost,
    ];

    const child = spawn('aspire', args, {
      stdio: ['ignore', 'pipe', 'pipe'],
    });

    const cleanup = () => {
      if (!child.killed) {
        child.kill('SIGTERM');
      }
    };

    signal?.addEventListener('abort', cleanup, { once: true });

    let buffer = '';

    try {
      for await (const chunk of child.stdout) {
        if (signal?.aborted) break;

        buffer += chunk.toString();
        const lines = buffer.split('\n');
        buffer = lines.pop()!;

        for (const line of lines) {
          const trimmed = line.trim();
          if (trimmed) {
            yield JSON.parse(trimmed) as T;
          }
        }
      }

      if (buffer.trim() && !signal?.aborted) {
        yield JSON.parse(buffer.trim()) as T;
      }
    } finally {
      signal?.removeEventListener('abort', cleanup);
      cleanup();
    }
  }
}
