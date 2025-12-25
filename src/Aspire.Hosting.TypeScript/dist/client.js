// RemoteAppHostClient - Client for communicating with GenericAppHost via Unix domain socket
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';
export class RemoteAppHostClient {
    connection = null;
    socket = null;
    socketPath;
    disconnectCallbacks = [];
    constructor(socketPath) {
        this.socketPath = socketPath || process.env.REMOTE_APP_HOST_SOCKET_PATH || '';
        if (!this.socketPath) {
            throw new Error('Socket path not provided and REMOTE_APP_HOST_SOCKET_PATH environment variable not set');
        }
    }
    /**
     * Register a callback to be called when the connection is lost
     */
    onDisconnect(callback) {
        this.disconnectCallbacks.push(callback);
    }
    notifyDisconnect() {
        for (const callback of this.disconnectCallbacks) {
            try {
                callback();
            }
            catch {
                // Ignore callback errors
            }
        }
    }
    connect(timeoutMs = 5000) {
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => reject(new Error('Connection timeout')), timeoutMs);
            this.socket = net.createConnection(this.socketPath);
            this.socket.once('error', (error) => {
                clearTimeout(timeout);
                reject(error);
            });
            this.socket.once('connect', () => {
                clearTimeout(timeout);
                try {
                    const reader = new rpc.SocketMessageReader(this.socket);
                    const writer = new rpc.SocketMessageWriter(this.socket);
                    this.connection = rpc.createMessageConnection(reader, writer);
                    this.connection.onClose(() => {
                        this.connection = null;
                        this.notifyDisconnect();
                    });
                    this.connection.onError((err) => console.error('JsonRpc connection error:', err));
                    this.connection.listen();
                    resolve();
                }
                catch (e) {
                    reject(e);
                }
            });
            this.socket.on('close', () => {
                this.connection?.dispose();
                this.connection = null;
                this.notifyDisconnect();
            });
        });
    }
    ping() {
        if (!this.connection)
            return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('ping');
    }
    executeInstruction(instruction) {
        if (!this.connection)
            return Promise.reject(new Error('Not connected to RemoteAppHost'));
        return this.connection.sendRequest('executeInstruction', JSON.stringify(instruction));
    }
    disconnect() {
        try {
            this.connection?.dispose();
        }
        catch { /* ignore */ }
        this.connection = null;
        try {
            this.socket?.end();
        }
        catch { /* ignore */ }
        this.socket = null;
    }
    get connected() {
        return this.connection !== null && this.socket !== null;
    }
}
// Singleton instance
let defaultClient = null;
export function getClient() {
    if (!defaultClient) {
        defaultClient = new RemoteAppHostClient();
    }
    return defaultClient;
}
export async function connectToRemoteAppHost() {
    const client = getClient();
    if (!client.connected) {
        await client.connect();
    }
    return client;
}
