import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { isWorkspaceOpen } from '../utils/workspace';

export function deployCommand(terminalProvider: AspireTerminalProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    terminalProvider.sendToAspireTerminal("aspire deploy", null);
}
