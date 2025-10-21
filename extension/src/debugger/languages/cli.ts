import { ChildProcessWithoutNullStreams, spawn } from "child_process";
import { EnvVar } from "../../dcp/types";
import { mergeEnvs } from "../../utils/environment";
import { extensionLogOutputChannel } from "../../utils/logging";
import { AspireTerminalProvider } from "../../utils/AspireTerminalProvider";

export interface SpawnProcessOptions {
    stdoutCallback?: (data: string) => void;
    stderrCallback?: (data: string) => void;
    exitCallback?: (code: number | null) => void;
    errorCallback?: (error: Error) => void;
    env?: EnvVar[];
    workingDirectory?: string;
    debugSessionId?: string,
    noDebug?: boolean;
}

export function spawnCliProcess(terminalProvider: AspireTerminalProvider, command: string, args?: string[], options?: SpawnProcessOptions): ChildProcessWithoutNullStreams {
    const envVars = mergeEnvs(process.env, options?.env);
    const additionalEnv = terminalProvider.createEnvironment(options?.debugSessionId, options?.noDebug);
    const workingDirectory = options?.workingDirectory ?? process.cwd();

    extensionLogOutputChannel.info(`Spawning CLI process: ${command} ${args?.join(" ")} (working directory: ${workingDirectory})`);

    const child = spawn(command, args ?? [], {
        cwd: workingDirectory,
        env: { ...envVars, ...additionalEnv },
        shell: false
    });

    child.stdout.on("data", (data) => {
        options?.stdoutCallback?.(new String(data).toString());
    });

    child.stderr.on("data", (data) => {
        options?.stderrCallback?.(new String(data).toString());
    });

    child.on('error', (error) => {
        options?.errorCallback?.(error);
    });

    child.on("close", (code) => {
        options?.exitCallback?.(code);
    });

    return child;
}
