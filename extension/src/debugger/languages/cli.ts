import { ChildProcessWithoutNullStreams, spawn } from "child_process";
import { EnvVar } from "../../dcp/types";
import { mergeEnvs } from "../../utils/environment";
import { createEnvironment } from "../../utils/terminal";
import DcpServer from "../../dcp/AspireDcpServer";

export interface SpawnProcessOptions {
    stdoutCallback?: (data: string) => void;
    stderrCallback?: (data: string) => void;
    exitCallback?: (code: number | null) => void;
    env?: EnvVar[];
    workingDirectory?: string;
    dcpServer?: DcpServer;
}

export function spawnCliProcess(command: string, args?: string[], options?: SpawnProcessOptions): ChildProcessWithoutNullStreams {
    const envVars = mergeEnvs(process.env, options?.env);
    const additionalEnv = createEnvironment(options?.dcpServer);

    const child = spawn(command, args ?? [], {
        cwd: options?.workingDirectory ?? process.cwd(),
        env: { ...envVars, ...additionalEnv },
        shell: true // Ensures Windows compatibility
    });

    child.stdout.on("data", (data) => {
        options?.stdoutCallback?.(data);
    });

    child.stderr.on("data", (data) => {
        options?.stderrCallback?.(data);
    });

    child.on("close", (code) => {
        options?.exitCallback?.(code);
    });

    return child;
}
