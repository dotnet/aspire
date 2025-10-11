import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { getOpenApphostFile, isWorkspaceOpen } from '../utils/workspace';

export async function addCommand(terminalProvider: AspireTerminalProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    terminalProvider.sendToAspireTerminal("aspire add", getOpenApphostFile());
}
