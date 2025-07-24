import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/workspace';

export async function configCommand() {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire config");
}
