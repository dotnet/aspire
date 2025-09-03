import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { isWorkspaceOpen } from '../utils/workspace';

export function addCommand(aspireTerminalProvider: AspireTerminalProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    aspireTerminalProvider.sendToAspireTerminal("aspire add", null);
}
