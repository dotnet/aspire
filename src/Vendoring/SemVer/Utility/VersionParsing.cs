using System;
using Semver.Parsing;

namespace Semver.Utility;

internal static class VersionParsing
{
    /// <remarks>
    /// This exception is used with the
    /// <see cref="SemVersionParser.Parse(string,SemVersionStyles,Exception,int,out SemVersion)"/>
    /// method to indicate parse failure without constructing a new exception.
    /// This exception should never be thrown or exposed outside of this
    /// package.
    /// </remarks>
    public static readonly Exception FailedException = new Exception("Parse Failed");
}
