import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/workspace';

export async function addCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire add");
}
