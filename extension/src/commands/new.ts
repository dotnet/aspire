import { RpcServerConnectionInfo } from '../server/AspireRpcServer';
import { sendToAspireTerminal } from '../utils/terminal';

export async function newCommand(rpcServerConnectionInfo: RpcServerConnectionInfo) {
    sendToAspireTerminal("aspire new", rpcServerConnectionInfo);
};
