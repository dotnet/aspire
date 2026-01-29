import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function cacheClearCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('cache clear');
}
