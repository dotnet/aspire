import { exec } from 'child_process';
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

  const { stdout } = await execAsync(cmd, {
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
