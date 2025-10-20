import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { getOpenApphostFile, isWorkspaceOpen } from '../utils/workspace';

export async function publishCommand(terminalProvider: AspireTerminalProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    terminalProvider.sendToAspireTerminal("aspire publish", getOpenApphostFile());
}
