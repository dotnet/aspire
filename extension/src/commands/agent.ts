import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function agentCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('agent');
}
