import { RpcServerConnectionInfo } from '../server/AspireRpcServer';
import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/workspace';

export async function addCommand(rpcServerConnectionInfo: RpcServerConnectionInfo) {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire add", rpcServerConnectionInfo);
}
