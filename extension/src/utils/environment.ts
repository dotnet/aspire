import { EnvVar } from "../dcp/types";

export function mergeEnvs(base: NodeJS.ProcessEnv, envVars?: EnvVar[]): Record<string, string | undefined> {
    const merged: Record<string, string | undefined> = { ...base };
    if (envVars) {
        for (const e of envVars) {
            merged[e.name] = e.value;
        }
    }
    return merged;
}

export const enum EnvironmentVariables {
    ASPIRE_CLI_STOP_ON_ENTRY = "ASPIRE_CLI_STOP_ON_ENTRY"
}
