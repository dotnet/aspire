import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function deployCommand(terminalProvider: AspireTerminalProvider) {
    await terminalProvider.sendAspireCommandToAspireTerminal('deploy');
}
