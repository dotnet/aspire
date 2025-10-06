import { ResourceDebuggerExtension } from "../debuggerExtensions";

export const pythonDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'python',
    debugAdapter: 'debugpy',
    extensionId: 'ms-python.python',
    displayName: 'Python'
};
