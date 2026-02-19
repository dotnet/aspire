import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function updateCommand(terminalProvider: AspireTerminalProvider) {
    await terminalProvider.sendAspireCommandToAspireTerminal('update');
}
