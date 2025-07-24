import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/workspace';

export async function runCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire run");
};
