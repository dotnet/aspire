import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function resourcesCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('resources');
}
