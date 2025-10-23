import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export async function initCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendToAspireTerminal("aspire init");
};