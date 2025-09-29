import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { isWorkspaceOpen } from '../utils/workspace';

export async function configCommand(terminalProvider: AspireTerminalProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    terminalProvider.sendToAspireTerminal("aspire config");
}
