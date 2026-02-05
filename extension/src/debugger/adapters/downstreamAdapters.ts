import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { debugAdapterNotFound, unsupportedDebugLanguage } from '../../loc/strings';
import { extensionLogOutputChannel } from '../../utils/logging';

/**
 * Supported debug languages for the Aspire DAP middleware.
 */
export type DebugLanguage = 'dotnet' | 'python' | 'nodejs';

/**
 * Configuration for a downstream debug adapter.
 */
export interface DownstreamAdapterConfig {
    /** Path to the debug adapter executable */
    executablePath: string;
    /** Arguments to pass to the debug adapter */
    args: string[];
    /** Connection mode: stdio (default) or callback for future use */
    mode: 'stdio' | 'callback';
    /** The adapter ID to use when forwarding initialize requests (e.g., coreclr, python, node) */
    adapterId: string;
    /** Optional environment variables to set when launching the adapter */
    env?: { [key: string]: string };
}

/**
 * Metadata about a downstream debug adapter.
 */
interface AdapterMetadata {
    /** 
     * Relative path from extension adapters directory to the adapter executable.
     * If null, uses 'executable' as an absolute/PATH command.
     */
    relativePath: string | null;
    /**
     * The executable command to run. Used when relativePath is null.
     * For example, 'python3' or 'node'.
     */
    executable?: string;
    /** Arguments to pass when launching the adapter */
    args: string[];
    /** Connection mode */
    mode: 'stdio' | 'callback';
    /** The adapter ID expected by the downstream debugger */
    adapterId: string;
}

/**
 * Mapping of language to adapter metadata.
 * The adapter executables are expected to be in extension/adapters/{language}/
 */
const ADAPTER_METADATA: Record<DebugLanguage, AdapterMetadata> = {
    'dotnet': {
        // vsdbg - .NET Core debugger
        relativePath: process.platform === 'win32' ? 'dotnet/vsdbg.exe' : 'dotnet/vsdbg',
        args: ['--interpreter=vscode'],
        mode: 'stdio',
        adapterId: 'coreclr'
    },
    'python': {
        // debugpy - Python debugger (launched via Python interpreter with bundled debugpy)
        // The adapters/python folder contains the debugpy module, so we set PYTHONPATH to it
        relativePath: null,
        executable: process.platform === 'win32' ? 'python' : 'python3',
        args: ['-m', 'debugpy.adapter'],
        mode: 'stdio',
        adapterId: 'debugpy'
    },
    'nodejs': {
        // vscode-js-debug - JavaScript/Node.js debugger
        relativePath: process.platform === 'win32' ? 'nodejs/js-debug/src/dapDebugServer.js' : 'nodejs/js-debug/src/dapDebugServer.js',
        args: [],
        mode: 'stdio',
        adapterId: 'pwa-node'
    }
};

/**
 * Gets the path to the adapters directory within the Aspire extension.
 */
function getAdaptersDirectory(): string {
    const extension = vscode.extensions.getExtension('microsoft-aspire.aspire-vscode');
    if (!extension) {
        throw new Error('Aspire extension not found');
    }
    return path.join(extension.extensionPath, 'adapters');
}

/**
 * Gets the configured Python interpreter path from the Python extension.
 * Falls back to 'python3' (or 'python' on Windows) if not available.
 */
async function getPythonInterpreterPath(): Promise<string> {
    const fallback = process.platform === 'win32' ? 'python' : 'python3';
    
    try {
        const pythonExtension = vscode.extensions.getExtension('ms-python.python');
        if (!pythonExtension) {
            extensionLogOutputChannel.info('Python extension not found, using fallback interpreter');
            return fallback;
        }

        if (!pythonExtension.isActive) {
            await pythonExtension.activate();
        }

        const pythonApi = pythonExtension.exports;
        
        // The Python extension exports an API to get the active interpreter
        // See: https://github.com/microsoft/vscode-python/wiki/AB-Experiments
        if (pythonApi?.environments) {
            // New API (Python extension 2023+)
            const activeEnv = await pythonApi.environments.resolveEnvironment(
                pythonApi.environments.getActiveEnvironmentPath()
            );
            if (activeEnv?.executable?.uri) {
                const interpreterPath = activeEnv.executable.uri.fsPath;
                extensionLogOutputChannel.info(`Using Python interpreter from extension: ${interpreterPath}`);
                return interpreterPath;
            }
        }
        
        // Fallback: try the older API
        if (pythonApi?.settings) {
            const interpreterPath = pythonApi.settings.getExecutionDetails()?.execCommand?.[0];
            if (interpreterPath) {
                extensionLogOutputChannel.info(`Using Python interpreter from extension (legacy API): ${interpreterPath}`);
                return interpreterPath;
            }
        }

        extensionLogOutputChannel.info('Could not get Python interpreter from extension, using fallback');
        return fallback;
    } catch (err) {
        extensionLogOutputChannel.warn(`Error getting Python interpreter: ${err}`);
        return fallback;
    }
}

/**
 * Gets the path to the bundled debugpy from the ms-python.debugpy extension.
 * Returns undefined if the extension is not installed.
 */
function getDebugpyBundledPath(): string | undefined {
    const debugpyExtension = vscode.extensions.getExtension('ms-python.debugpy');
    if (!debugpyExtension) {
        extensionLogOutputChannel.warn('Debugpy extension (ms-python.debugpy) not found');
        return undefined;
    }
    
    const bundledLibsPath = path.join(debugpyExtension.extensionPath, 'bundled', 'libs');
    if (fs.existsSync(bundledLibsPath)) {
        extensionLogOutputChannel.info(`Found bundled debugpy at: ${bundledLibsPath}`);
        return bundledLibsPath;
    }
    
    extensionLogOutputChannel.warn(`Bundled debugpy libs not found at: ${bundledLibsPath}`);
    return undefined;
}

/**
 * Checks if the given language is a supported debug language.
 */
export function isSupportedLanguage(language: string): language is DebugLanguage {
    return language === 'dotnet' || language === 'python' || language === 'nodejs';
}

/**
 * Gets the downstream debug adapter configuration for the specified language.
 *
 * @param language The target debug language (dotnet, python, nodejs)
 * @returns The adapter configuration, or undefined if the adapter is not available
 */
export async function getDownstreamAdapterConfig(language: string): Promise<DownstreamAdapterConfig | undefined> {
    if (!isSupportedLanguage(language)) {
        const message = unsupportedDebugLanguage(language);
        extensionLogOutputChannel.error(message);
        void vscode.window.showErrorMessage(message);
        return undefined;
    }

    const metadata = ADAPTER_METADATA[language];
    const adaptersDir = getAdaptersDirectory();
    
    let executablePath: string;
    let env: { [key: string]: string } | undefined;
    
    if (metadata.relativePath === null) {
        // Special handling for Python - use interpreter with bundled debugpy
        if (language === 'python') {
            executablePath = await getPythonInterpreterPath();
            // Set PYTHONPATH to include the bundled debugpy module
            const pythonAdaptersPath = path.join(adaptersDir, 'python');
            env = { PYTHONPATH: pythonAdaptersPath };
            extensionLogOutputChannel.info(`Setting PYTHONPATH for debugpy: ${pythonAdaptersPath}`);
        } else if (metadata.executable) {
            executablePath = metadata.executable;
        } else {
            extensionLogOutputChannel.error(`No executable specified for language: ${language}`);
            return undefined;
        }
        extensionLogOutputChannel.info(`Using executable for ${language}: ${executablePath}`);
    } else {
        // Use bundled adapter
        executablePath = path.join(adaptersDir, metadata.relativePath);

        // Check if the adapter executable exists
        if (!fs.existsSync(executablePath)) {
            const message = debugAdapterNotFound(language, executablePath);
            extensionLogOutputChannel.error(message);
            void vscode.window.showWarningMessage(message);
            return undefined;
        }
    }

    extensionLogOutputChannel.info(`Resolved ${language} debug adapter: ${executablePath}`);

    return {
        executablePath,
        args: metadata.args,
        mode: metadata.mode,
        adapterId: metadata.adapterId,
        env
    };
}
