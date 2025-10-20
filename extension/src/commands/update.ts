import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { getOpenApphostFile, isWorkspaceOpen } from '../utils/workspace';

export async function updateCommand(terminalProvider: AspireTerminalProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    terminalProvider.sendToAspireTerminal('aspire update', getOpenApphostFile());
}
