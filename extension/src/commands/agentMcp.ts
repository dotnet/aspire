import { AspireTerminalProvider } from "../utils/AspireTerminalProvider";

export async function agentMcpCommand(terminalProvider: AspireTerminalProvider) {
    terminalProvider.sendAspireCommandToAspireTerminal('agent mcp');
}
