import { OutputLogCategory, vscOutputChannelWriter } from "./vsc";

export async function logAsyncOperation<T>(category: OutputLogCategory, beforeMessage: string, afterMessage: (result: T) => string, operation: () => Promise<T>): Promise<T> {
    vscOutputChannelWriter.appendLine(category, beforeMessage);
    try {
        const result = await operation();
        vscOutputChannelWriter.appendLine(category, afterMessage(result));
        return result;
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : JSON.stringify(error);
        vscOutputChannelWriter.appendLine(category, `Error during operation: ${errorMessage}`);
        throw error;
    }
}