// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Watches for changes in a <see cref="WatchedDirectory"/> and its subdirectories.
/// </summary>
internal abstract class DirectoryWatcher(string watchedDirectory, ImmutableHashSet<string> watchedFileNames, bool includeSubdirectories) : IDisposable
{
    public string WatchedDirectory { get; } = watchedDirectory;
    public ImmutableHashSet<string> WatchedFileNames { get; set; } = watchedFileNames;
    public bool IncludeSubdirectories { get; } = includeSubdirectories;

    public event EventHandler<ChangedPath>? OnFileChange;
    public event EventHandler<Exception>? OnError;

    public abstract bool EnableRaisingEvents { get; set; }
    public abstract void Dispose();

    protected void NotifyChange(string fullPath, ChangeKind kind)
    {
        var onFileChange = OnFileChange;
        if (onFileChange == null)
        {
            return;
        }

        var watchedFileNames = WatchedFileNames;
        if (watchedFileNames.Count > 0 && !watchedFileNames.Contains(Path.GetFileName(fullPath)))
        {
            return;
        }

        onFileChange.Invoke(this, new ChangedPath(fullPath, kind));
    }

    protected void NotifyError(Exception e)
    {
        OnError?.Invoke(this, e);
    }

    public static DirectoryWatcher Create(string watchedDirectory, ImmutableHashSet<string> watchedFileNames, bool usePollingWatcher, bool includeSubdirectories)
    {
        return usePollingWatcher ?
            new PollingDirectoryWatcher(watchedDirectory, watchedFileNames, includeSubdirectories) :
            new EventBasedDirectoryWatcher(watchedDirectory, watchedFileNames, includeSubdirectories);
    }
}
