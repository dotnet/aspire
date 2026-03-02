import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function addCommand(terminalProvider: AspireTerminalProvider) {
    await terminalProvider.sendAspireCommandToAspireTerminal('add');
}
