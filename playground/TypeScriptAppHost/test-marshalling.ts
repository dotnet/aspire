// test-marshalling.ts - Simple test harness for object marshalling
import * as net from 'net';
import * as rpc from 'vscode-jsonrpc/node.js';

interface MarshalledObject {
    $id: string;
    $type: string;
    [key: string]: unknown;
}

class DotNetProxy {
    private readonly _id: string;
    private readonly _type: string;
    private readonly _data: Record<string, unknown>;
    private readonly _connection: rpc.MessageConnection;

    constructor(marshalled: MarshalledObject, connection: rpc.MessageConnection) {
        this._id = marshalled.$id;
        this._type = marshalled.$type;
        this._data = { ...marshalled };
        this._connection = connection;
    }

    get $id(): string { return this._id; }
    get $type(): string { return this._type; }

    getCachedValue(propertyName: string): unknown {
        return this._data[propertyName];
    }

    async getProperty(propertyName: string): Promise<unknown> {
        const result = await this._connection.sendRequest('GetProperty', this._id, propertyName);
        return this.wrapIfProxy(result);
    }

    async setIndexer(key: string | number, value: unknown): Promise<void> {
        await this._connection.sendRequest('SetIndexer', this._id, key, value);
    }

    async invokeMethod(methodName: string, args?: Record<string, unknown>): Promise<unknown> {
        const result = await this._connection.sendRequest('InvokeMethod', this._id, methodName, args ?? null);
        return this.wrapIfProxy(result);
    }

    private wrapIfProxy(value: unknown): unknown {
        if (value && typeof value === 'object' && '$id' in value && '$type' in value) {
            return new DotNetProxy(value as MarshalledObject, this._connection);
        }
        return value;
    }
}

async function main() {
    const socketPath = process.env.TEST_SOCKET_PATH;
    if (!socketPath) {
        console.error('TEST_SOCKET_PATH environment variable required');
        process.exit(1);
    }

    console.log(`Connecting to: ${socketPath}`);

    const socket = net.createConnection(socketPath);

    socket.once('error', (error: Error) => {
        console.error('Connection error:', error);
        process.exit(1);
    });

    socket.once('connect', async () => {
        console.log('Connected!');

        const reader = new rpc.SocketMessageReader(socket);
        const writer = new rpc.SocketMessageWriter(socket);
        const connection = rpc.createMessageConnection(reader, writer);

        // Handle callback invocations from .NET
        connection.onRequest('invokeCallback', async (callbackId: string, args: unknown) => {
            console.log(`\n>>> Callback invoked: ${callbackId}`);
            console.log('>>> Raw args:', JSON.stringify(args, null, 2));

            // Wrap the args in a proxy if it's a marshalled object
            if (args && typeof args === 'object' && '$id' in args && '$type' in args) {
                console.log('>>> Creating DotNetProxy for callback args');
                const proxy = new DotNetProxy(args as MarshalledObject, connection);

                console.log('>>> Proxy $id:', proxy.$id);
                console.log('>>> Proxy $type:', proxy.$type);

                // Test getCachedValue
                const resourceName = proxy.getCachedValue('ResourceName');
                console.log('>>> Cached ResourceName:', resourceName);

                // Test getProperty to get EnvironmentVariables
                console.log('>>> Calling getProperty("EnvironmentVariables")...');
                const envVars = await proxy.getProperty('EnvironmentVariables') as DotNetProxy;
                console.log('>>> EnvironmentVariables $type:', envVars.$type);

                // Test setIndexer to add environment variable
                console.log('>>> Calling setIndexer("TEST_VAR", "Hello from TypeScript!")...');
                await envVars.setIndexer('TEST_VAR', 'Hello from TypeScript!');
                console.log('>>> setIndexer completed!');

                return { success: true };
            } else {
                console.log('>>> Args is not a marshalled object');
                return { success: false, error: 'Not a marshalled object' };
            }
        });

        connection.listen();

        try {
            // Create builder
            console.log('\n=== Creating builder ===');
            const createResult = await connection.sendRequest('ExecuteInstructionAsync', JSON.stringify({
                name: 'CREATE_BUILDER',
                builderName: 'builder1',
                args: [],
                projectDirectory: process.cwd()
            }));
            console.log('Create result:', createResult);

            // Add container
            console.log('\n=== Adding container ===');
            const containerResult = await connection.sendRequest('ExecuteInstructionAsync', JSON.stringify({
                name: 'INVOKE',
                source: 'builder1',
                target: 'container1',
                methodAssembly: 'Aspire.Hosting',
                methodType: 'Aspire.Hosting.ContainerResourceBuilderExtensions',
                methodName: 'AddContainer',
                methodArgumentTypes: ['Aspire.Hosting.IDistributedApplicationBuilder', 'System.String', 'System.String'],
                metadataToken: 0,
                args: { name: 'testredis', image: 'redis:latest' }
            }));
            console.log('Container result:', containerResult);

            // Register a callback and add WithEnvironment
            console.log('\n=== Adding WithEnvironment with callback ===');
            const withEnvResult = await connection.sendRequest('ExecuteInstructionAsync', JSON.stringify({
                name: 'INVOKE',
                source: 'container1',
                target: 'container2',
                methodAssembly: 'Aspire.Hosting',
                methodType: 'Aspire.Hosting.ResourceBuilderExtensions',
                methodName: 'WithEnvironment',
                methodArgumentTypes: ['Aspire.Hosting.ApplicationModel.IResourceBuilder<T>', 'System.Action<Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext>'],
                metadataToken: 0,
                args: { callback: 'test_callback_1' }
            }));
            console.log('WithEnvironment result:', withEnvResult);

            // Build the app (this will trigger the callback)
            console.log('\n=== Building app (will trigger callback) ===');
            const buildResult = await connection.sendRequest('ExecuteInstructionAsync', JSON.stringify({
                name: 'INVOKE',
                source: 'builder1',
                target: 'app1',
                methodAssembly: 'Aspire.Hosting',
                methodType: 'Aspire.Hosting.IDistributedApplicationBuilder',
                methodName: 'Build',
                methodArgumentTypes: [],
                metadataToken: 0,
                args: {}
            }));
            console.log('Build result:', buildResult);

            console.log('\n=== Test complete! ===');

        } catch (error) {
            console.error('Error:', error);
        }

        // Keep connection alive for a bit
        await new Promise(resolve => setTimeout(resolve, 2000));
        connection.dispose();
        socket.end();
    });
}

main();
