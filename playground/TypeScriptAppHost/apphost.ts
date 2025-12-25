// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';

console.log("Aspire TypeScript AppHost starting...");

const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;

if (!socketPath) {
    console.error("REMOTE_APP_HOST_SOCKET_PATH environment variable not set");
    console.log("Running in standalone mode (no Aspire integration)");

    // Keep the process running in standalone mode
    const startTime = Date.now();
    console.log("AppHost is running. Press Ctrl+C to stop.");

    const intervalId = setInterval(() => {
        const elapsed = Math.floor((Date.now() - startTime) / 1000);
        console.log(`AppHost running for ${elapsed} seconds...`);
    }, 5000);

    process.on('SIGINT', () => {
        console.log("\nReceived SIGINT. Shutting down gracefully...");
        clearInterval(intervalId);
        process.exit(0);
    });
} else {
    console.log(`Connecting to RemoteAppHost at: ${socketPath}`);

    const socket = net.createConnection(socketPath);

    socket.on('connect', async () => {
        console.log("Connected to RemoteAppHost!");

        const reader = new rpc.SocketMessageReader(socket);
        const writer = new rpc.SocketMessageWriter(socket);
        const connection = rpc.createMessageConnection(reader, writer);

        connection.onClose(() => {
            console.log("Connection closed");
            process.exit(0);
        });

        connection.onError((err: unknown) => {
            console.error("Connection error:", err);
        });

        connection.listen();

        try {
            // Test ping
            console.log("Sending ping...");
            const pong = await connection.sendRequest<string>('ping');
            console.log(`Received: ${pong}`);

            // Create a builder
            console.log("Creating distributed application builder...");
            const createResult = await connection.sendRequest('executeInstruction', JSON.stringify({
                name: 'CREATE_BUILDER',
                builderName: 'builder',
                args: []
            }));
            console.log("Create builder result:", JSON.stringify(createResult, null, 2));

            // Run the builder (this starts the Aspire dashboard and keeps running)
            console.log("Building and running the application...");
            const runResult = await connection.sendRequest('executeInstruction', JSON.stringify({
                name: 'RUN_BUILDER',
                builderName: 'builder'
            }));
            console.log("Run builder result:", JSON.stringify(runResult, null, 2));

            console.log("Aspire application is now running!");
            console.log("Press Ctrl+C to stop.");

        } catch (err) {
            console.error("Error communicating with RemoteAppHost:", err);
            connection.dispose();
            process.exit(1);
        }
    });

    socket.on('error', (err: Error) => {
        console.error("Socket error:", err.message);
        process.exit(1);
    });

    process.on('SIGINT', () => {
        console.log("\nReceived SIGINT. Shutting down gracefully...");
        socket.end();
        process.exit(0);
    });

    process.on('SIGTERM', () => {
        console.log("\nReceived SIGTERM. Shutting down gracefully...");
        socket.end();
        process.exit(0);
    });
}
