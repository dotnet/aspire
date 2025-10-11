import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";
import { getOpenApphostFile } from "../utils/workspace";

export async function newCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendToAspireTerminal("aspire new", getOpenApphostFile());
};
