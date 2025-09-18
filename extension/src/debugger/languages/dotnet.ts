import * as vscode from 'vscode';
import { extensionLogOutputChannel } from '../../utils/logging';
import { noCsharpBuildTask, buildFailedWithExitCode, noOutputFromMsbuild, failedToGetTargetPath } from '../../loc/strings';
import { execFile } from 'child_process';
import * as util from 'util';
import * as path from 'path';
import { doesFileExist } from '../../utils/io';
import { AspireResourceExtendedDebugConfiguration } from '../../dcp/types';
import { ResourceDebuggerExtension } from '../debuggerExtensions';

const execFileAsync = util.promisify(execFile);

export const projectDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'project',
    debugAdapter: 'coreclr',
    extensionId: 'ms-dotnettools.csharp',
    displayName: 'C#',
    createDebugSessionConfigurationCallback: async (launchConfig, args, env, launchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration): Promise<void> => {
        const projectPath = launchConfig.project_path;
        const workingDirectory = path.dirname(launchConfig.project_path);

        const outputPath = await getDotNetTargetPath(projectPath);

        if (!(await doesFileExist(outputPath)) || launchOptions.forceBuild) {
            await buildDotNetProject(projectPath);
        }

        debugConfiguration.program = outputPath;
        debugConfiguration.cwd = workingDirectory;
    }
};

async function buildDotNetProject(projectFile: string): Promise<void> {
    const csharpDevKit = vscode.extensions.getExtension('ms-dotnettools.csdevkit');
    if (!csharpDevKit) {
        // If c# dev kit is not installed, we will have already built this project on the command line using the Aspire CLI
        // thus we should just immediately return
        return Promise.resolve();
    }

    if (!csharpDevKit.isActive) {
        extensionLogOutputChannel.info('Activating C# Dev Kit extension...');
        await csharpDevKit.activate();
    }

    // C# Dev Kit may not register the build task immediately, so we need to retry until it is available
    const pRetry = (await import('p-retry')).default;
    const buildTask = await pRetry(async () => {
        const tasks = await vscode.tasks.fetchTasks();
        const buildTask = tasks.find(t => t.source === "dotnet" && t.name?.includes('build'));
        if (!buildTask) {
            throw new Error(noCsharpBuildTask);
        }

        return buildTask;
    });

// Modify the task to target the specific project
    const projectName = path.basename(projectFile, '.csproj');

    // Create a modified task definition with just the project file
    const modifiedDefinition = {
        ...buildTask.definition,
        file: projectFile  // This will make it build the specific project directly
    };

    // Create a new task with the modified definition
    const modifiedTask = new vscode.Task(
        modifiedDefinition,
        buildTask.scope || vscode.TaskScope.Workspace,
        `build ${projectName}`,
        buildTask.source,
        buildTask.execution,
        buildTask.problemMatchers
    );

    extensionLogOutputChannel.info(`Executing build task: ${modifiedTask.name} for project: ${projectFile}`);
    await vscode.tasks.executeTask(modifiedTask);

    let disposable: vscode.Disposable;
    return new Promise<void>((resolve, reject) => {
        disposable = vscode.tasks.onDidEndTaskProcess(async e => {
            if (e.execution.task === modifiedTask) {
                if (e.exitCode !== 0) {
                    reject(new Error(buildFailedWithExitCode(e.exitCode ?? 0)));
                }
                else {
                    return resolve();
                }
            }
        });
    }).finally(() => disposable.dispose());
}

async function getDotNetTargetPath(projectFile: string): Promise<string> {
    const args = [
        'msbuild',
        projectFile,
        '-nologo',
        '-getProperty:TargetPath',
        '-v:q',
        '-property:GenerateFullPaths=true'
    ];
    try {
        const { stdout } = await execFileAsync('dotnet', args, { encoding: 'utf8' });
        const output = stdout.trim();
        if (!output) {
            throw new Error(noOutputFromMsbuild);
        }

        return output;
    } catch (err) {
        throw new Error(failedToGetTargetPath(String(err)));
    }
}
