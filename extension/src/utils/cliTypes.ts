export interface AspireSettingsFile {
    appHostPath?: string;
    [key: string]: any;
}

/**
 * Represents the new aspire.config.json configuration file format.
 */
export interface AspireConfigFile {
    appHost?: {
        path?: string;
        language?: string;
    };
    sdk?: {
        version?: string;
    };
    channel?: string;
    features?: Record<string, boolean>;
    profiles?: Record<string, {
        applicationUrl?: string;
        environmentVariables?: Record<string, string>;
    }>;
    packages?: Record<string, string>;
}
