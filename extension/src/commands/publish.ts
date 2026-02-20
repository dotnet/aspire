import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function publishCommand(terminalProvider: AspireTerminalProvider) {
    await terminalProvider.sendAspireCommandToAspireTerminal('publish');
}
