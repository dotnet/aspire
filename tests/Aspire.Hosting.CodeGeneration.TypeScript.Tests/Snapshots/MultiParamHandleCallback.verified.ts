private async _withMultiParamHandleCallbackInternal(callback: (arg1: TestCallbackContext, arg2: TestEnvironmentContext) => Promise<void>): Promise<TestRedisResource> {
        const callbackId = registerCallback(async (arg1Data: unknown, arg2Data: unknown) => {
            const arg1Handle = wrapIfHandle(arg1Data) as TestCallbackContextHandle;
            const arg1 = new TestCallbackContext(arg1Handle, this._client);
            const arg2Handle = wrapIfHandle(arg2Data) as TestEnvironmentContextHandle;
            const arg2 = new TestEnvironmentContext(arg2Handle, this._client);
            await callback(arg1, arg2);
        });
        const rpcArgs: Record<string, unknown> = { builder: this._handle, callback: callbackId };