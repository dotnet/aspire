import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export async function newCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendToAspireTerminal("aspire new", null);
};
