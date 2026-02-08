// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Tar;
using System.IO.Compression;
using Aspire.Cli.Projects;
using Aspire.Shared;

namespace Aspire.Cli.Tests;

public class BundleTrailerTests
{
    [Fact]
    public void TryRead_ReturnsNull_ForEmptyFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var result = BundleTrailer.TryRead(tempFile);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void TryRead_ReturnsNull_ForFileTooSmall()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[16]);
            var result = BundleTrailer.TryRead(tempFile);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void TryRead_ReturnsNull_ForFileWithNoMagic()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[64]);
            var result = BundleTrailer.TryRead(tempFile);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void WriteAndRead_Roundtrips()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            // Simulate: [fake CLI binary 100 bytes] [fake payload 50 bytes] [trailer 32 bytes]
            var fakeCliData = new byte[100];
            var fakePayload = new byte[50];
            Random.Shared.NextBytes(fakeCliData);
            Random.Shared.NextBytes(fakePayload);

            var versionHash = BundleTrailer.ComputeVersionHash("13.2.0-dev");

            using (var stream = File.Create(tempFile))
            {
                stream.Write(fakeCliData);
                stream.Write(fakePayload);
                BundleTrailer.Write(stream, payloadOffset: 100, payloadSize: 50, versionHash: versionHash);
            }

            // File should be 100 + 50 + 32 = 182 bytes
            Assert.Equal(182, new FileInfo(tempFile).Length);

            var trailer = BundleTrailer.TryRead(tempFile);
            Assert.NotNull(trailer);
            Assert.Equal(100UL, trailer.PayloadOffset);
            Assert.Equal(50UL, trailer.PayloadSize);
            Assert.Equal(versionHash, trailer.VersionHash);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OpenPayload_ReturnsCorrectSlice()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var fakeCliData = new byte[100];
            var payloadData = new byte[] { 1, 2, 3, 4, 5 };
            var versionHash = BundleTrailer.ComputeVersionHash("test");

            using (var stream = File.Create(tempFile))
            {
                stream.Write(fakeCliData);
                stream.Write(payloadData);
                BundleTrailer.Write(stream, payloadOffset: 100, payloadSize: 5, versionHash: versionHash);
            }

            var trailer = BundleTrailer.TryRead(tempFile)!;
            using var payloadStream = BundleTrailer.OpenPayload(tempFile, trailer);

            var buffer = new byte[5];
            var read = payloadStream.Read(buffer, 0, buffer.Length);
            Assert.Equal(5, read);
            Assert.Equal(payloadData, buffer);

            // Should be at end
            Assert.Equal(0, payloadStream.Read(buffer, 0, buffer.Length));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ComputeVersionHash_IsDeterministic()
    {
        var hash1 = BundleTrailer.ComputeVersionHash("13.2.0-dev");
        var hash2 = BundleTrailer.ComputeVersionHash("13.2.0-dev");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeVersionHash_DiffersForDifferentVersions()
    {
        var hash1 = BundleTrailer.ComputeVersionHash("13.2.0-dev");
        var hash2 = BundleTrailer.ComputeVersionHash("13.3.0-dev");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VersionMarker_WriteAndRead_Roundtrips()
    {
        var tempDir = Directory.CreateTempSubdirectory("aspire-test");
        try
        {
            var hash = BundleTrailer.ComputeVersionHash("13.2.0");
            BundleTrailer.WriteVersionMarker(tempDir.FullName, hash);

            var readHash = BundleTrailer.ReadVersionMarker(tempDir.FullName);
            Assert.Equal(hash, readHash);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void VersionMarker_ReturnsNull_WhenMissing()
    {
        var tempDir = Directory.CreateTempSubdirectory("aspire-test");
        try
        {
            var readHash = BundleTrailer.ReadVersionMarker(tempDir.FullName);
            Assert.Null(readHash);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ExtractPayloadAsync_ExtractsWithStripComponents()
    {
        var tempFile = Path.GetTempFileName();
        var extractDir = Directory.CreateTempSubdirectory("aspire-extract-test");
        try
        {
            // Create a tar.gz with a top-level directory
            var tarGzBytes = CreateTestTarGz("aspire-13.2.0-linux-x64", new Dictionary<string, string>
            {
                ["runtime/test.txt"] = "runtime-content",
                ["dashboard/index.html"] = "<html/>",
            });

            var versionHash = BundleTrailer.ComputeVersionHash("13.2.0");

            // Build self-extracting binary: [fake cli] [tar.gz] [trailer]
            using (var stream = File.Create(tempFile))
            {
                var fakeCliData = new byte[64];
                stream.Write(fakeCliData);
                stream.Write(tarGzBytes);
                BundleTrailer.Write(stream, payloadOffset: 64, payloadSize: (ulong)tarGzBytes.Length, versionHash: versionHash);
            }

            var trailer = BundleTrailer.TryRead(tempFile)!;

            await AppHostServerProjectFactory.ExtractPayloadAsync(tempFile, trailer, extractDir.FullName, CancellationToken.None);

            // Verify files were extracted with top-level directory stripped
            Assert.True(File.Exists(Path.Combine(extractDir.FullName, "runtime", "test.txt")));
            Assert.Equal("runtime-content", File.ReadAllText(Path.Combine(extractDir.FullName, "runtime", "test.txt")));
            Assert.True(File.Exists(Path.Combine(extractDir.FullName, "dashboard", "index.html")));
            Assert.Equal("<html/>", File.ReadAllText(Path.Combine(extractDir.FullName, "dashboard", "index.html")));
        }
        finally
        {
            File.Delete(tempFile);
            extractDir.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Creates a tar.gz byte array with the given files nested under a top-level directory.
    /// </summary>
    private static byte[] CreateTestTarGz(string topLevelDir, Dictionary<string, string> files)
    {
        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
        using (var tarWriter = new TarWriter(gzipStream, leaveOpen: true))
        {
            // Add top-level directory entry
            var dirEntry = new PaxTarEntry(TarEntryType.Directory, topLevelDir + "/");
            tarWriter.WriteEntry(dirEntry);

            foreach (var (relativePath, content) in files)
            {
                // Add intermediate directories
                var fullPath = $"{topLevelDir}/{relativePath}";
                var dir = Path.GetDirectoryName(fullPath)!.Replace('\\', '/');
                var dirParts = dir.Split('/');
                for (var i = 1; i <= dirParts.Length; i++)
                {
                    var subDir = string.Join('/', dirParts[..i]) + "/";
                    var subDirEntry = new PaxTarEntry(TarEntryType.Directory, subDir);
                    tarWriter.WriteEntry(subDirEntry);
                }

                // Add file entry
                var fileEntry = new PaxTarEntry(TarEntryType.RegularFile, fullPath)
                {
                    DataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content))
                };
                tarWriter.WriteEntry(fileEntry);
            }
        }

        return memoryStream.ToArray();
    }
}
