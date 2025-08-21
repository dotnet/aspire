import { ChildProcessWithoutNullStreams, spawn } from "child_process";
import { EnvVar } from "../../dcp/types";
import { mergeEnvs } from "../../utils/environment";
import { createEnvironment } from "../../utils/terminal";
import AspireDcpServer from "../../dcp/AspireDcpServer";
import { extensionLogOutputChannel } from "../../utils/logging";

export interface SpawnProcessOptions {
    stdoutCallback?: (data: string) => void;
    stderrCallback?: (data: string) => void;
    exitCallback?: (code: number | null) => void;
    env?: EnvVar[];
    workingDirectory?: string;
    dcpServer?: AspireDcpServer;
    excludeExtensionEnvironment?: boolean;
}

export function spawnCliProcess(command: string, args?: string[], options?: SpawnProcessOptions): ChildProcessWithoutNullStreams {
    const envVars = mergeEnvs(process.env, options?.env);
    const additionalEnv = options?.excludeExtensionEnvironment ? { } : createEnvironment(options?.dcpServer);
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

    child.on("close", (code) => {
        options?.exitCallback?.(code);
    });

    return child;
}
