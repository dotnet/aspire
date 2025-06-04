import { getAppHostProject } from '../utils/projects';
import { getAspireTerminal } from '../utils/terminal';

export async function runCommand() {
    const terminal = getAspireTerminal();
    // Show a quick pick to the user to select a project
    const selectedProject = await getAppHostProject();

    if (selectedProject) {
        terminal.sendText(`aspire run --project "${selectedProject}"`);
        terminal.show();
    }
};