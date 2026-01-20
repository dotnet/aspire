import { ConfigWebviewProvider } from '../webviews/ConfigWebviewProvider';
import { isWorkspaceOpen } from '../utils/workspace';

export async function configCommand(webviewProvider: ConfigWebviewProvider) {
    if (!isWorkspaceOpen()) {
        return;
    }

    await webviewProvider.show();
}
