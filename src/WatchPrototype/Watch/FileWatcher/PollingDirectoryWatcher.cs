// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.DotNet.Watch
{
    internal sealed class PollingDirectoryWatcher : DirectoryWatcher
    {
        // The minimum interval to rerun the scan
        private static readonly TimeSpan _minRunInternal = TimeSpan.FromSeconds(.5);

        private readonly DirectoryInfo _watchedDirectory;

        private Dictionary<string, DateTime> _currentSnapshot = new(PathUtilities.OSSpecificPathComparer);

        // The following are sets that are used to calculate new snapshot and cleared on eached use (pooled):
        private Dictionary<string, DateTime> _snapshotBuilder = new(PathUtilities.OSSpecificPathComparer);
        private readonly Dictionary<string, ChangeKind> _changesBuilder = new(PathUtilities.OSSpecificPathComparer);

        private readonly Thread _pollingThread;
        private bool _raiseEvents;

        private volatile bool _disposed;

        public PollingDirectoryWatcher(string watchedDirectory, ImmutableHashSet<string> watchedFileNames, bool includeSubdirectories)
            : base(watchedDirectory, watchedFileNames, includeSubdirectories)
        {
            _watchedDirectory = new DirectoryInfo(watchedDirectory);

            _pollingThread = new Thread(new ThreadStart(PollingLoop))
            {
                IsBackground = true,
                Name = nameof(PollingDirectoryWatcher)
            };

            CaptureInitialSnapshot();

            _pollingThread.Start();
        }

        public override void Dispose()
        {
            EnableRaisingEvents = false;
            _disposed = true;
        }

        public override bool EnableRaisingEvents
        {
            get => _raiseEvents;
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _raiseEvents = value;
            }
        }

        private void PollingLoop()
        {
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            while (!_disposed)
            {
                if (stopwatch.Elapsed < _minRunInternal)
                {
                    // Don't run too often
                    // The min wait time here can be double
                    // the value of the variable (FYI)
                    Thread.Sleep(_minRunInternal);
                }

                stopwatch.Reset();

                if (!_raiseEvents)
                {
                    continue;
                }

                CheckForChangedFiles();
            }

            stopwatch.Stop();
        }

        private void CaptureInitialSnapshot()
        {
            Debug.Assert(_currentSnapshot.Count == 0);

            ForeachEntityInDirectory(_watchedDirectory, _currentSnapshot.Add);
        }

        private void CheckForChangedFiles()
        {
            Debug.Assert(_changesBuilder.Count == 0);
            Debug.Assert(_snapshotBuilder.Count == 0);

            ForeachEntityInDirectory(_watchedDirectory, (filePath, currentWriteTime) =>
            {
                if (!_currentSnapshot.TryGetValue(filePath, out var snapshotWriteTime))
                {
                    _changesBuilder.TryAdd(filePath, ChangeKind.Add);
                }
                else if (snapshotWriteTime != currentWriteTime)
                {
                    _changesBuilder.TryAdd(filePath, ChangeKind.Update);
                }

                _snapshotBuilder.Add(filePath, currentWriteTime);
            });

            foreach (var (filePath, _) in _currentSnapshot)
            {
                if (!_snapshotBuilder.ContainsKey(filePath))
                {
                    _changesBuilder.TryAdd(filePath, ChangeKind.Delete);
                }
            }

            NotifyChanges(_changesBuilder);

            // Swap the two dictionaries
            (_snapshotBuilder, _currentSnapshot) = (_currentSnapshot, _snapshotBuilder);

            _changesBuilder.Clear();
            _snapshotBuilder.Clear();
        }

        private void ForeachEntityInDirectory(DirectoryInfo dirInfo, Action<string, DateTime> fileAction)
        {
            if (!dirInfo.Exists)
            {
                return;
            }

            IEnumerable<FileSystemInfo> entities;
            try
            {
                entities = dirInfo.EnumerateFileSystemInfos("*.*", SearchOption.TopDirectoryOnly);
            }
            // If the directory is deleted after the exists check this will throw and could crash the process
            catch (DirectoryNotFoundException)
            {
                return;
            }

            foreach (var entity in entities)
            {
                if (entity is DirectoryInfo subdirInfo)
                {
                    if (IncludeSubdirectories)
                    {
                        ForeachEntityInDirectory(subdirInfo, fileAction);
                    }
                }
                else
                {
                    string filePath;
                    DateTime currentWriteTime;
                    try
                    {
                        filePath = entity.FullName;
                        currentWriteTime = entity.LastWriteTimeUtc;
                    }
                    catch (FileNotFoundException)
                    {
                        continue;
                    }

                    fileAction(filePath, currentWriteTime);
                }
            }
        }

        private void NotifyChanges(Dictionary<string, ChangeKind> changes)
        {
            foreach (var (path, kind) in changes)
            {
                if (_disposed || !_raiseEvents)
                {
                    break;
                }

                NotifyChange(path, kind);
            }
        }
    }
}
