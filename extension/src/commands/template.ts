import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export async function templateCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('template');
}
