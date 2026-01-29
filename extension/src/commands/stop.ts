import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export async function stopCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('stop');
}
