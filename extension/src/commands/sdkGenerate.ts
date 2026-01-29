import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function sdkGenerateCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('sdk generate');
}
