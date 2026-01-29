import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function doCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('do');
}
