import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function execCommand(terminalProvider: AspireTerminalProvider): Promise<void> {
    terminalProvider.sendAspireCommandToAspireTerminal('exec');
}
