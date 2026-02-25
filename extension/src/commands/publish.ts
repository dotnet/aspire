import { AspireEditorCommandProvider } from '../editor/AspireEditorCommandProvider';
import { AspireTerminalProvider } from '../utils/AspireTerminalProvider';
import { getProjectArgs } from '../utils/appHostArgs';

export async function publishCommand(terminalProvider: AspireTerminalProvider, editorCommandProvider: AspireEditorCommandProvider) {
    const projectArgs = await getProjectArgs(editorCommandProvider);
    await terminalProvider.sendAspireCommandToAspireTerminal('publish', true, projectArgs);
}
