import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function updateCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('update');
}
