import * as path from 'path';
import * as fs from 'fs';
import { LaunchConfiguration, EnvVar } from '../dcp/types';
import { extensionLogOutputChannel } from '../utils/logging';

/*
 * Represents a launchSettings.json profile.
 * Only a property that is available both in the C# vscode debugger (https://code.visualstudio.com/docs/csharp/debugger-settings)
 * *and* in the launchSettings.json is available here.
*/
export interface LaunchProfile {
    executablePath?: string;
    workingDirectory?: string;
    // args in debug configuration
    commandLineArgs?: string;
    // Both these properties must be set to launch the browser. See
    // https://code.visualstudio.com/docs/csharp/debugger-settings#_starting-a-web-browser
    launchBrowser?: boolean;
    applicationUrl?: string;
    // env in debug configuration
    environmentVariables?: { [key: string]: string };
    // checkForDevCert in debug configuration
    useSSL?: boolean;
}

export interface LaunchSettings {
    profiles: { [key: string]: LaunchProfile };
}

export interface LaunchProfileResult {
    profile: LaunchProfile | null;
    profileName: string | null;
}

/**
 * Reads and parses the launchSettings.json file for a given project
 */
export async function readLaunchSettings(projectPath: string): Promise<LaunchSettings | null> {
    try {
        const projectDir = path.dirname(projectPath);
        const launchSettingsPath = path.join(projectDir, 'Properties', 'launchSettings.json');

        if (!fs.existsSync(launchSettingsPath)) {
            extensionLogOutputChannel.debug(`Launch settings file not found at: ${launchSettingsPath}`);
            return null;
        }

        const content = fs.readFileSync(launchSettingsPath, 'utf8');
        const launchSettings = JSON.parse(content) as LaunchSettings;

        extensionLogOutputChannel.debug(`Successfully read launch settings from: ${launchSettingsPath}`);
        return launchSettings;
    } catch (error) {
        extensionLogOutputChannel.error(`Failed to read launch settings for project ${projectPath}: ${error}`);
        return null;
    }
}

/**
 * Determines the base launch profile according to the Aspire launch profile rules
 */
export function determineBaseLaunchProfile(
    launchConfig: LaunchConfiguration,
    launchSettings: LaunchSettings | null
): LaunchProfileResult {
    // If disable_launch_profile property is set to true in project launch configuration, there is no base profile, regardless of the value of launch_profile property.
    if (launchConfig.disable_launch_profile === true) {
        extensionLogOutputChannel.debug('Launch profile disabled via disable_launch_profile=true');
        return { profile: null, profileName: null };
    }

    if (!launchSettings || !launchSettings.profiles) {
        extensionLogOutputChannel.debug('No launch settings or profiles available');
        return { profile: null, profileName: null };
    }

    // If launch_profile property is set, check if that profile exists
    if (launchConfig.launch_profile) {
        const profileName = launchConfig.launch_profile;
        const profile = launchSettings.profiles[profileName];

        if (profile) {
            extensionLogOutputChannel.debug(`Using explicit launch profile: ${profileName}`);
            return { profile, profileName };
        } else {
            extensionLogOutputChannel.debug(`Explicit launch profile '${profileName}' not found in launch settings`);
            return { profile: null, profileName: null };
        }
    }

    // TODO: If launch_profile is absent, check for a ServiceDefaults project in the workspace
    // and look for a launch profile with that ServiceDefaults project name in the current project's launch settings
    extensionLogOutputChannel.debug('No base launch profile determined');
    return { profile: null, profileName: null };
}

/**
 * Merges environment variables from launch profile with run session environment variables
 * Run session variables take precedence over launch profile variables
 */
export function mergeEnvironmentVariables(
    baseProfileEnv: { [key: string]: string } | undefined,
    runSessionEnv: EnvVar[]
): [string, string][] {
    const merged: { [key: string]: string } = {};

    // Start with base profile environment variables
    if (baseProfileEnv) {
        Object.assign(merged, baseProfileEnv);
    }

    // Override with run session environment variables (these take precedence)
    for (const envVar of runSessionEnv) {
        merged[envVar.name] = envVar.value;
    }

    return Object.entries(merged);
}

/**
 * Determines the final arguments array according to launch profile rules
 * If run session args are present (including empty array), they completely replace launch profile args
 * If run session args are absent/null, launch profile args are used if available
 */
export function determineArguments(
    baseProfileArgs: string | undefined,
    runSessionArgs: string[] | undefined | null
): string | undefined {
    // If run session args are explicitly provided (including empty array), use them
    if (runSessionArgs !== undefined && runSessionArgs !== null) {
        extensionLogOutputChannel.debug(`Using run session arguments: ${JSON.stringify(runSessionArgs)}`);
        return runSessionArgs.join(' ');
    }

    // If run session args are absent/null, use launch profile args if available
    if (baseProfileArgs) {
        extensionLogOutputChannel.debug(`Using launch profile arguments: ${baseProfileArgs}`);
        return baseProfileArgs;
    }

    extensionLogOutputChannel.debug('No arguments determined');
    return undefined;
}

/**
 * Determines the working directory for project execution
 * Uses launch profile WorkingDirectory if specified, otherwise uses project directory
 */
export function determineWorkingDirectory(
    projectPath: string,
    baseProfile: LaunchProfile | null
): string {
    if (baseProfile?.workingDirectory) {
        // If working directory is relative, resolve it relative to project directory
        if (path.isAbsolute(baseProfile.workingDirectory)) {
            extensionLogOutputChannel.debug(`Using absolute working directory from launch profile: ${baseProfile.workingDirectory}`);
            return baseProfile.workingDirectory;
        } else {
            const projectDir = path.dirname(projectPath);
            const workingDir = path.resolve(projectDir, baseProfile.workingDirectory);
            extensionLogOutputChannel.debug(`Using relative working directory from launch profile: ${workingDir}`);
            return workingDir;
        }
    }

    // Default to project directory
    const projectDir = path.dirname(projectPath);
    extensionLogOutputChannel.debug(`Using default working directory (project directory): ${projectDir}`);
    return projectDir;
}

interface ServerReadyAction {
    action: "openExternally";
    pattern: "\\bNow listening on:\\s+https?://\\S+";
    uriFormat: string;
}

export function determineServerReadyAction(launchBrowser?: boolean, applicationUrl?: string): ServerReadyAction | undefined {
    if (!launchBrowser || !applicationUrl) {
        return undefined;
    }

    return {
        action: "openExternally",
        pattern: "\\bNow listening on:\\s+https?://\\S+",
        uriFormat: applicationUrl
    };
}
