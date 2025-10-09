import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";
import { getOpenApphostFile } from "../utils/workspace";

export async function initCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendToAspireTerminal("aspire init");
};