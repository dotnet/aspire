import * as vscode from 'vscode';
import { noCsprojFound, selectProjectToRun } from '../constants/strings';

export async function getProjectFiles(): Promise<vscode.Uri[]> {
    const projectFiles = await vscode.workspace.findFiles('**/*.csproj');
    projectFiles.sort((a, b) => a.fsPath.localeCompare(b.fsPath));
    return projectFiles;
}

// will defer to aspire cli in the future, for now provide dummy implementation
export async function getAppHostProject(): Promise<string | undefined> {
    function prioritizeAppHostProject(projects: string[]): string[] {
        const index = projects.findIndex(p => p.includes('AppHost'));
        if (index > -1) {
            const [appHostProject] = projects.splice(index, 1);
            projects.unshift(appHostProject);
        }
        return projects;
    }
    
    const projectFiles = await getProjectFiles();

    if (projectFiles.length === 0) {
        vscode.window.showErrorMessage(noCsprojFound);
        return;
    }


    // Extract project names from the file paths
    const projectNames = projectFiles.map(file => vscode.workspace.asRelativePath(file));

    // it should just find whatever app host is in the workspace and internally pass that as --project
    const sortedProjectNames = prioritizeAppHostProject([...projectNames]);

    // Show a quick pick to the user to select a project
    const selectedProject = await vscode.window.showQuickPick(sortedProjectNames, {
        placeHolder: selectProjectToRun
    });

    return selectedProject;
}
