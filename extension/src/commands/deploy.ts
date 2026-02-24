import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function deployCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('deploy');
}
