import { spawn, type ChildProcess } from 'child_process';
import { aspireExec, aspireJson, aspireFollow, aspireStream } from './cli.js';
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
} from './types.js';

/**
 * A high-level API for automating the Aspire CLI.
 *
 * Use `AspireHost.start()` to launch an AppHost, then interact with its
 * resources, wait for health, collect logs, and stop it when done.
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
   * Returns an async iterable of resource snapshots.
   * Call `watcher.process.kill()` to stop watching.
   */
  watchResources(): { process: ChildProcess; stream: AsyncGenerator<ResourceSnapshot> } {
    return aspireFollow<ResourceSnapshot>(['describe', '--follow'], {
      appHost: this.appHost,
    });
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
   * Returns an async iterable of log entries.
   * Call `follower.process.kill()` to stop streaming.
   */
  streamLogs(resourceName?: string): { process: ChildProcess; stream: AsyncGenerator<LogEntry> } {
    const args = ['logs', '--follow'];
    if (resourceName) {
      args.splice(1, 0, resourceName); // insert after 'logs'
    }

    return aspireFollow<LogEntry>(args, {
      appHost: this.appHost,
    });
  }

  /**
   * Pipe live logs to a writable stream (e.g. `process.stdout`).
   * Runs `aspire logs --follow` in the background as plain text.
   * Returns the child process — call `.kill()` to stop.
   */
  followLogs(resourceName?: string, output: NodeJS.WritableStream = process.stdout): ChildProcess {
    const args = ['logs', '--follow', '--non-interactive', '--nologo'];
    if (resourceName) {
      args.splice(1, 0, resourceName);
    }
    args.push('--apphost', this.appHost);

    const child = spawn('aspire', args, {
      stdio: ['ignore', 'pipe', 'pipe'],
    });

    child.stdout.pipe(output, { end: false });
    child.stderr.pipe(output, { end: false });

    return child;
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
}
