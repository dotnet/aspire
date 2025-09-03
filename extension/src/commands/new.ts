import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export function newCommand(aspireTerminalProvider: AspireTerminalProvider) {
    aspireTerminalProvider.sendToAspireTerminal("aspire new", null);
};
