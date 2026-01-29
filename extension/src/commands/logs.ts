import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function logsCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('logs');
}
