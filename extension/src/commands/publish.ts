import { AspireEditorCommandProvider } from '../editor/AspireEditorCommandProvider';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { getAppHostArgs } from '../utils/appHostArgs';

export async function publishCommand(terminalProvider: AspireTerminalProvider, editorCommandProvider: AspireEditorCommandProvider) {
    const appHostArgs = await getAppHostArgs(editorCommandProvider);
    await terminalProvider.sendAspireCommandToAspireTerminal('publish', true, appHostArgs);
}
