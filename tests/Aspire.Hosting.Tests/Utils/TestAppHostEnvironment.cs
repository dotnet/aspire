// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Aspire.Hosting.Tests.Utils;

internal static class TestAppHostEnvironment
{
    public static AppHostEnvironment Create(IConfiguration? configuration = null, IHostEnvironment? hostEnvironment = null)
    {
        return new AppHostEnvironment(
            configuration ?? new ConfigurationBuilder().Build(), 
            hostEnvironment ?? new TestHostEnvironment());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "/test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class NullFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath) => 
            new NotFoundDirectoryContents();

        public IFileInfo GetFileInfo(string subpath) => 
            new NotFoundFileInfo(subpath);

        public IChangeToken Watch(string filter) => 
            NullChangeToken.Singleton;

        private sealed class NotFoundDirectoryContents : IDirectoryContents
        {
            public bool Exists => false;
            public IEnumerator<IFileInfo> GetEnumerator() => 
                Enumerable.Empty<IFileInfo>().GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => 
                GetEnumerator();
        }

        private sealed class NotFoundFileInfo(string name) : IFileInfo
        {
            public bool Exists => false;
            public bool IsDirectory => false;
            public DateTimeOffset LastModified => DateTimeOffset.MinValue;
            public long Length => -1;
            public string Name => name;
            public string? PhysicalPath => null;
            public Stream CreateReadStream() => throw new FileNotFoundException();
        }
    }
}
