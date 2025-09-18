import * as vscode from 'vscode';
import { ICliRpcClient } from './rpcClient';
import { formatText } from '../utils/strings';
import { extensionLogOutputChannel } from '../utils/logging';

export class ProgressNotifier {
    private _rpcClient?: ICliRpcClient;
    private _currentProgress?: {
        resolve: () => void;
        updateMessage: (msg: string) => void;
    };

    // If a new non-null status arrives within the delay, the clear is cancelled and the
    // current progress is updated.
    private _pendingClearTimeout?: ReturnType<typeof setTimeout>;

    public get isActive() {
        return !!this._currentProgress || !!this._pendingClearTimeout;
    }

    constructor(rpcClient?: ICliRpcClient) {
        this._rpcClient = rpcClient;
    }

    show(statusText: string | null) {
        extensionLogOutputChannel.info(`Setting status/progress: ${statusText ?? 'null'}`);

        if (!statusText) {
            // If there is an active progress, wait a short period before
            // actually clearing it. This allows callers to quickly call
            // show(null) followed by show(non-null) within 250ms and have the
            // existing notification updated instead of being torn down and recreated.
            if (this._currentProgress) {
                if (this._pendingClearTimeout) {
                    clearTimeout(this._pendingClearTimeout);
                }
                this._pendingClearTimeout = setTimeout(() => {
                    this.clear();
                    this._pendingClearTimeout = undefined;
                }, 250);
            }
            return;
        }

        // A new non-null status arrived; if there was a pending clear scheduled
        // from a recent show(null), cancel it so we can update the existing
        // progress in-place.
        if (this._pendingClearTimeout) {
            clearTimeout(this._pendingClearTimeout);
            this._pendingClearTimeout = undefined;
        }

        // If a progress notification is already active, update its message
        if (this._currentProgress) {
            try {
                this._currentProgress.updateMessage(formatText(statusText));
            }
            catch (err) {
                extensionLogOutputChannel.error(`Failed to update progress message: ${err}`);
            }
            return;
        }

        // No active progress: create one that can be cancelled by the user
        let resolveFn: () => void;
        const waitPromise = new Promise<void>(resolve => { resolveFn = resolve; });

        this._currentProgress = {
            resolve: () => { resolveFn(); },
            updateMessage: (_m: string) => {}
        };

        vscode.window.withProgress({
            location: vscode.ProgressLocation.Notification,
            cancellable: true
        }, async (progress, token) => {
            this._currentProgress!.updateMessage = (m: string) => progress.report({ message: m });

            // Report the initial message as the progress message (no title)
            progress.report({ message: formatText(statusText) });

            const cancelListener = token.onCancellationRequested(() => {
                extensionLogOutputChannel.info('User cancelled progress; attempting to stop CLI');
                try {
                    this._rpcClient?.stopCli();
                }
                catch (err) {
                    extensionLogOutputChannel.error(`Failed to stop CLI: ${err}`);
                }
            });

            // Keep the progress alive until show(null) calls resolve
            try {
                return await waitPromise;
            } finally {
                return cancelListener.dispose();
            }
        }).then(undefined, (err: any) => {
            extensionLogOutputChannel.error(`Progress failed: ${err}`);
        });
    }

    clear() {
        // Cancel any scheduled deferred clear so we don't race with an
        // incoming show call.
        if (this._pendingClearTimeout) {
            clearTimeout(this._pendingClearTimeout);
            this._pendingClearTimeout = undefined;
        }

        if (this._currentProgress) {
            this._currentProgress.resolve();
            this._currentProgress = undefined;
        }
    }
}
