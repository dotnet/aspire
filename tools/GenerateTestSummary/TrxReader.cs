// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace Aspire.TestTools;

public class TrxReader
{
    public static IList<TestResult> GetTestResultsFromTrx(string filepath, Func<string, string, bool>? testFilter = null)
    {
        XmlSerializer serializer = new(typeof(TestRun));
        using FileStream fileStream = new(filepath, FileMode.Open);

        if (serializer.Deserialize(fileStream) is not TestRun testRun || testRun.Results?.UnitTestResults is null)
        {
            return Array.Empty<TestResult>();
        }

        var testResults = new List<TestResult>();

        foreach (var unitTestResult in testRun.Results.UnitTestResults)
        {
            if (string.IsNullOrEmpty(unitTestResult.TestName) || string.IsNullOrEmpty(unitTestResult.Outcome))
            {
                continue;
            }

            if (testFilter is not null && !testFilter(unitTestResult.TestName, unitTestResult.Outcome))
            {
                continue;
            }

            var startTime = unitTestResult.StartTime;
            var endTime = unitTestResult.EndTime;

            testResults.Add(new TestResult(
                Name: unitTestResult.TestName,
                Outcome: unitTestResult.Outcome,
                StartTime: startTime is null ? TimeSpan.MinValue : TimeSpan.Parse(startTime, CultureInfo.InvariantCulture),
                EndTime: endTime is null ? TimeSpan.MinValue : TimeSpan.Parse(endTime, CultureInfo.InvariantCulture),
                ErrorMessage: unitTestResult.Output?.ErrorInfoString,
                Stdout: unitTestResult.Output?.StdOut
            ));
        }

        return testResults;
    }

    public static TestRun? DeserializeTrxFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException($"{nameof(filePath)} cannot be null or empty.", nameof(filePath));
        }

        XmlSerializer serializer = new(typeof(TestRun));

        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        return serializer.Deserialize(fileStream) as TestRun;
    }
}

[XmlRoot("TestRun", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")]
public class TestRun
{
    public Results? Results { get; set; }

    public ResultSummary? ResultSummary { get; set; }
}

public class Results
{
    [XmlElement("UnitTestResult")]
    public List<UnitTestResult>? UnitTestResults { get; set; }
}

public class UnitTestResult
{
    [XmlAttribute("testName")]
    public string? TestName { get; set; }

    [XmlAttribute("outcome")]
    public string? Outcome { get; set; }

    [XmlAttribute("startTime")]
    public string? StartTime { get; set; }

    [XmlAttribute("endTime")]
    public string? EndTime { get; set; }

    public Output? Output { get; set; }
}

public class Output
{
    [XmlAnyElement]
    public XmlElement? ErrorInfo { get; set; }
    public string? StdOut { get; set; }

    [XmlIgnore]
    public string ErrorInfoString => ErrorInfo?.InnerText ?? string.Empty;
}

public class ResultSummary
{
    public string? Outcome { get; set; }
    public Counters? Counters { get; set; }
}

public class Counters
{
    [XmlAttribute("total")]
    public int Total { get; set; }

    [XmlAttribute("executed")]
    public int Executed { get; set; }

    [XmlAttribute("passed")]
    public int Passed { get; set; }

    [XmlAttribute("failed")]
    public int Failed { get; set; }

    [XmlAttribute("error")]
    public int Error { get; set; }

    [XmlAttribute("timeout")]
    public int Timeout { get; set; }

    [XmlAttribute("aborted")]
    public int Aborted { get; set; }

    [XmlAttribute("inconclusive")]
    public int Inconclusive { get; set; }

    [XmlAttribute("passedButRunAborted")]
    public int PassedButRunAborted { get; set; }

    [XmlAttribute("notRunnable")]
    public int NotRunnable { get; set; }

    [XmlAttribute("notExecuted")]
    public int NotExecuted { get; set; }
}

public record TestResult(string Name, string Outcome, TimeSpan StartTime, TimeSpan EndTime, string? ErrorMessage = null, string? Stdout = null);
