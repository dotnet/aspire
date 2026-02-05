// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal class FileWatcher(ILogger logger, EnvironmentOptions environmentOptions) : IDisposable
    {
        // Directory watcher for each watched directory tree.
        // Keyed by full path to the root directory with a trailing directory separator.
        protected readonly Dictionary<string, DirectoryWatcher> _directoryTreeWatchers = new(PathUtilities.OSSpecificPathComparer);

        // Directory watcher for each watched directory (non-recursive).
        // Keyed by full path to the root directory with a trailing directory separator.
        protected readonly Dictionary<string, DirectoryWatcher> _directoryWatchers = new(PathUtilities.OSSpecificPathComparer);

        private bool _disposed;
        public event Action<ChangedPath>? OnFileChange;

        public bool SuppressEvents { get; set; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var (_, watcher) in _directoryTreeWatchers)
            {
                watcher.OnFileChange -= WatcherChangedHandler;
                watcher.OnError -= WatcherErrorHandler;
                watcher.Dispose();
            }
        }

        protected virtual DirectoryWatcher CreateDirectoryWatcher(string directory, ImmutableHashSet<string> fileNames, bool includeSubdirectories)
        {
            var watcher = DirectoryWatcher.Create(directory, fileNames, environmentOptions.IsPollingEnabled, includeSubdirectories);
            if (watcher is EventBasedDirectoryWatcher eventBasedWatcher)
            {
                eventBasedWatcher.Logger = message => logger.LogTrace(message);
            }

            return watcher;
        }

        public bool WatchingDirectories
            => _directoryTreeWatchers.Count > 0 || _directoryWatchers.Count > 0;

        /// <summary>
        /// Watches individual files.
        /// </summary>
        public void WatchFiles(IEnumerable<string> filePaths)
            => Watch(filePaths, containingDirectories: false, includeSubdirectories: false);

        /// <summary>
        /// Watches an entire directory or directory tree.
        /// </summary>
        public void WatchContainingDirectories(IEnumerable<string> filePaths, bool includeSubdirectories)
            => Watch(filePaths, containingDirectories: true, includeSubdirectories);

        private void Watch(IEnumerable<string> filePaths, bool containingDirectories, bool includeSubdirectories)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Debug.Assert(containingDirectories || !includeSubdirectories);

            var filesByDirectory =
                from path in filePaths
                group path by PathUtilities.EnsureTrailingSlash(PathUtilities.NormalizeDirectorySeparators(Path.GetDirectoryName(path)!))
                into g
                select (g.Key, containingDirectories ? [] : g.Select(Path.GetFileName).ToImmutableHashSet(PathUtilities.OSSpecificPathComparer));

            foreach (var (directory, fileNames) in filesByDirectory)
            {
                // the directory is watched by active directory watcher:
                if (!includeSubdirectories && _directoryWatchers.TryGetValue(directory, out var existingDirectoryWatcher))
                {
                    if (existingDirectoryWatcher.WatchedFileNames.IsEmpty)
                    {
                        // already watching all files in the directory
                        continue;
                    }

                    if (fileNames.IsEmpty)
                    {
                        // watch all files:
                        existingDirectoryWatcher.WatchedFileNames = fileNames;
                        continue;
                    }

                    // merge sets of watched files:
                    foreach (var fileName in fileNames)
                    {
                        existingDirectoryWatcher.WatchedFileNames = existingDirectoryWatcher.WatchedFileNames.Add(fileName);
                    }

                    continue;
                }

                // the directory is a root or subdirectory of active directory tree watcher:
                var alreadyWatched = _directoryTreeWatchers.Any(d => directory.StartsWith(d.Key, PathUtilities.OSSpecificPathComparison));
                if (alreadyWatched)
                {
                    continue;
                }

                var newWatcher = CreateDirectoryWatcher(directory, fileNames, includeSubdirectories);
                newWatcher.OnFileChange += WatcherChangedHandler;
                newWatcher.OnError += WatcherErrorHandler;
                newWatcher.EnableRaisingEvents = true;

                // watchers that are now redundant (covered by the new directory watcher):
                if (includeSubdirectories)
                {
                    Debug.Assert(fileNames.IsEmpty);

                    RemoveRedundantWatchers(_directoryTreeWatchers);
                    RemoveRedundantWatchers(_directoryWatchers);

                    void RemoveRedundantWatchers(Dictionary<string, DirectoryWatcher> watchers)
                    {
                        var watchersToRemove = watchers
                            .Where(d => d.Key.StartsWith(directory, PathUtilities.OSSpecificPathComparison))
                            .ToList();

                        foreach (var (watchedDirectory, watcher) in watchersToRemove)
                        {
                            watchers.Remove(watchedDirectory);

                            watcher.EnableRaisingEvents = false;
                            watcher.OnFileChange -= WatcherChangedHandler;
                            watcher.OnError -= WatcherErrorHandler;

                            watcher.Dispose();
                        }
                    }

                    _directoryTreeWatchers.Add(directory, newWatcher);
                }
                else
                {
                    _directoryWatchers.Add(directory, newWatcher);
                }
            }
        }

        private void WatcherErrorHandler(object? sender, Exception error)
        {
            if (sender is DirectoryWatcher watcher)
            {
                logger.LogWarning("The file watcher observing '{WatchedDirectory}' encountered an error: {Message}", watcher.WatchedDirectory, error.Message);
            }
        }

        private void WatcherChangedHandler(object? sender, ChangedPath change)
        {
            if (!SuppressEvents)
            {
                OnFileChange?.Invoke(change);
            }
        }

        public async Task<ChangedFile?> WaitForFileChangeAsync(IReadOnlyDictionary<string, FileItem> fileSet, Action? startedWatching, CancellationToken cancellationToken)
        {
            var changedPath = await WaitForFileChangeAsync(
                acceptChange: change => fileSet.ContainsKey(change.Path),
                startedWatching,
                cancellationToken);

            return changedPath.HasValue ? new ChangedFile(fileSet[changedPath.Value.Path], changedPath.Value.Kind) : null;
        }

        public async Task<ChangedPath?> WaitForFileChangeAsync(Predicate<ChangedPath> acceptChange, Action? startedWatching, CancellationToken cancellationToken)
        {
            var fileChangedSource = new TaskCompletionSource<ChangedPath?>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => fileChangedSource.TrySetResult(null));

            void FileChangedCallback(ChangedPath change)
            {
                if (acceptChange(change))
                {
                    fileChangedSource.TrySetResult(change);
                }
            }

            ChangedPath? change;

            OnFileChange += FileChangedCallback;
            try
            {
                startedWatching?.Invoke();
                change = await fileChangedSource.Task;
            }
            finally
            {
                OnFileChange -= FileChangedCallback;
            }

            return change;
        }

        public static async ValueTask WaitForFileChangeAsync(string filePath, ILogger logger, EnvironmentOptions environmentOptions, Action? startedWatching, CancellationToken cancellationToken)
        {
            using var watcher = new FileWatcher(logger, environmentOptions);

            watcher.WatchContainingDirectories([filePath], includeSubdirectories: false);

            var fileChange = await watcher.WaitForFileChangeAsync(
                acceptChange: change => change.Path == filePath,
                startedWatching,
                cancellationToken);

            if (fileChange != null)
            {
                logger.LogInformation("File changed: {FilePath}", filePath);
            }
        }
    }
}
