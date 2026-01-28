import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function publishCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('publish');
}
