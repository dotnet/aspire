import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function addCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('add');
}
