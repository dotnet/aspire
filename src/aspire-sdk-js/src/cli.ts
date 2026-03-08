import { exec, spawn, type ChildProcess } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

/** Build CLI argument array with common flags. */
function buildArgs(command: string[], options: { appHost?: string; format?: 'Json' | 'Table' }): string[] {
  const args = [...command, '--non-interactive', '--nologo'];
  if (options.appHost) {
    args.push('--apphost', options.appHost);
  }
  if (options.format) {
    args.push('--format', options.format);
  }
  return args;
}

/** Run an aspire CLI command and return stdout. */
export async function aspireExec(command: string[], options: { appHost?: string; format?: 'Json' | 'Table'; cwd?: string } = {}): Promise<string> {
  const args = buildArgs(command, options);
  const cmd = `aspire ${args.join(' ')}`;

  const { stdout, stderr } = await execAsync(cmd, {
    cwd: options.cwd,
    maxBuffer: 10 * 1024 * 1024,
  });

  return stdout.trim();
}

/** Run an aspire CLI command and parse the JSON output. */
export async function aspireJson<T>(command: string[], options: { appHost?: string; cwd?: string } = {}): Promise<T> {
  const stdout = await aspireExec(command, { ...options, format: 'Json' });
  return JSON.parse(stdout) as T;
}

/**
 * Spawn an aspire CLI command that streams NDJSON and yield parsed objects.
 * Used for `--follow` commands (describe --follow, logs --follow).
 */
export async function* aspireStream<T>(command: string[], options: { appHost?: string; cwd?: string } = {}): AsyncGenerator<T> {
  const args = buildArgs(command, { ...options, format: 'Json' });

  const child = spawn('aspire', args, {
    stdio: ['ignore', 'pipe', 'pipe'],
    cwd: options.cwd,
  });

  let buffer = '';

  try {
    for await (const chunk of child.stdout) {
      buffer += chunk.toString();
      const lines = buffer.split('\n');
      buffer = lines.pop()!; // keep incomplete line in buffer

      for (const line of lines) {
        const trimmed = line.trim();
        if (trimmed) {
          yield JSON.parse(trimmed) as T;
        }
      }
    }

    // flush remaining
    if (buffer.trim()) {
      yield JSON.parse(buffer.trim()) as T;
    }
  } finally {
    if (!child.killed) {
      child.kill('SIGTERM');
    }
  }
}

/** Spawn an aspire CLI follow command, returning the child process and an async iterable. */
export function aspireFollow<T>(command: string[], options: { appHost?: string; cwd?: string } = {}): { process: ChildProcess; stream: AsyncGenerator<T> } {
  const args = buildArgs(command, { ...options, format: 'Json' });

  const child = spawn('aspire', args, {
    stdio: ['ignore', 'pipe', 'pipe'],
    cwd: options.cwd,
  });

  async function* iterate(): AsyncGenerator<T> {
    let buffer = '';
    try {
      for await (const chunk of child.stdout) {
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

      if (buffer.trim()) {
        yield JSON.parse(buffer.trim()) as T;
      }
    } finally {
      if (!child.killed) {
        child.kill('SIGTERM');
      }
    }
  }

  return { process: child, stream: iterate() };
}
