import { RpcServerConnectionInfo } from '../server/AspireRpcServer';
import { sendToAspireTerminal } from '../utils/terminal';
import { isWorkspaceOpen } from '../utils/workspace';

export async function configCommand(rpcServerConnectionInfo: RpcServerConnectionInfo) {
    if (!isWorkspaceOpen()) {
        return;
    }

    sendToAspireTerminal("aspire config", rpcServerConnectionInfo);
}
