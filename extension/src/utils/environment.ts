import { EnvVar } from "../dcp/types";

export function mergeEnvs(base: NodeJS.ProcessEnv, envVars?: EnvVar[]): Record<string, string | undefined> {
    const merged: Record<string, string | undefined> = base ? { ...base } : {};
    if (envVars) {
        for (const e of envVars) {
            merged[e.name] = e.value;
        }
    }
    return merged;
}