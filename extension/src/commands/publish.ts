import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { isWorkspaceOpen } from '../utils/workspace';

export function publishCommand(aspireTerminalProvider: AspireTerminalProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    aspireTerminalProvider.sendToAspireTerminal("aspire publish", null);
}
