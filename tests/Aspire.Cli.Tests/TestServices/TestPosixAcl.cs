// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.Cli.Tests.TestServices;

internal static class TestPosixAcl
{
    private const int AclTypeAccess = 0x8000;

    public static void Set(string path, string text)
    {
        if (!NativeLibrary.TryLoad("libacl.so.1", out var library))
        {
            Assert.Skip("POSIX ACL validation requires libacl.");
        }
        NativeLibrary.Free(library);
        var acl = acl_from_text(text);
        Assert.NotEqual(IntPtr.Zero, acl);
        try
        {
            Assert.Equal(0, acl_set_file(path, AclTypeAccess, acl));
        }
        finally
        {
            acl_free(acl);
        }
    }

    public static string Get(string path)
    {
        var acl = acl_get_file(path, AclTypeAccess);
        Assert.NotEqual(IntPtr.Zero, acl);
        try
        {
            var text = acl_to_text(acl, out _);
            Assert.NotEqual(IntPtr.Zero, text);
            try
            {
                return Marshal.PtrToStringUTF8(text)!;
            }
            finally
            {
                acl_free(text);
            }
        }
        finally
        {
            acl_free(acl);
        }
    }

    // This test-only probe does not require enabling unsafe code for the test project.
#pragma warning disable SYSLIB1054
    [DllImport("libacl.so.1", CharSet = CharSet.Ansi)]
    private static extern IntPtr acl_from_text(string text);

    [DllImport("libacl.so.1", CharSet = CharSet.Ansi)]
    private static extern int acl_set_file(string path, int type, IntPtr acl);

    [DllImport("libacl.so.1", CharSet = CharSet.Ansi)]
    private static extern IntPtr acl_get_file(string path, int type);

    [DllImport("libacl.so.1")]
    private static extern IntPtr acl_to_text(IntPtr acl, out nint length);

    [DllImport("libacl.so.1")]
    private static extern int acl_free(IntPtr value);
#pragma warning restore SYSLIB1054
}
