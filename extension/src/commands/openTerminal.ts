import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';

export async function openTerminalCommand(terminalProvider: AspireTerminalProvider): Promise<void> {
    // Ensure the Aspire terminal exists and show it
    const aspireTerminal = terminalProvider.getAspireTerminal(null);
    aspireTerminal.terminal.show();
}
