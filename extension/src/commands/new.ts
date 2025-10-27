import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export async function newCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('new');
};
