import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export async function initCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('init');
};