    async _addWithSource0Internal(resourceSource: string): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, resourceSource };
        {
            const resourceSource = getResourceSourceLocation();
            if (resourceSource) { rpcArgs['$source'] = resourceSource; }
        }
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addWithSource0',
            rpcArgs
        );
        return new TestRedisResourceImpl(result, this._client);
    }

    private async _addWithSource1Internal(resourceSource: string): Promise<TestRedisResource> {
        const rpcArgs: Record<string, unknown> = { builder: this._handle, resourceSource };
        {
            const resourceSource = getResourceSourceLocation();
            if (resourceSource) { rpcArgs['$source'] = resourceSource; }
        }
        const result = await this._client.invokeCapability<TestRedisResourceHandle>(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests/addWithSource1',
            rpcArgs
        );
        return new TestRedisResourceImpl(result, this._client);
    }