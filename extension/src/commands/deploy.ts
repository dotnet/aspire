import { RpcServerConnectionInfo } from '../server/AspireRpcServer';
import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/workspace';

export async function deployCommand(rpcServerConnectionInfo: RpcServerConnectionInfo) {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire deploy", rpcServerConnectionInfo);
}
