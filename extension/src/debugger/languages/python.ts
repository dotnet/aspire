import * as vscode from 'vscode';
import { EnvVar, LaunchOptions, AspireResourceDebugSession, AspireExtendedDebugConfiguration } from "../../dcp/types";
import { debugProject } from "../../loc/strings";
import { mergeEnvs } from "../../utils/environment";
import { extensionLogOutputChannel } from "../../utils/logging";
import { extensionContext } from '../../extension';

export async function startPythonProgram(file: string, workingDirectory: string, args: string[], env: EnvVar[], launchOptions: LaunchOptions): Promise<AspireResourceDebugSession | undefined> {
    try {
        const config: AspireExtendedDebugConfiguration = {
            type: 'python',
            request: 'launch',
            name: debugProject('Python Program'),
            program: file,
            args: args,
            cwd: workingDirectory,
            env: mergeEnvs(process.env, env),
            justMyCode: false,
            stopAtEntry: false,
            noDebug: !launchOptions.debug,
            runId: launchOptions.runId,
            dcpId: launchOptions.dcpId,
            console: 'internalConsole'
        };

        return await extensionContext.aspireDebugSession.startAndGetDebugSession(config);
    }
    catch (error) {
        if (error instanceof Error) {
            extensionLogOutputChannel.error(`Failed to start Python program: ${error.message}`);
            vscode.window.showErrorMessage(`Failed to start Python program: ${error.message}`);
            return undefined;
        }
    }
}