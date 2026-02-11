// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Aspire.Managed.DevCerts;

/// <summary>
/// Implements the dev-certs subcommand for aspire-managed, providing full parity
/// with <c>dotnet dev-certs https</c> without requiring the .NET SDK on PATH.
/// </summary>
internal static class DevCertsCommand
{
    // Exit codes — matches dotnet dev-certs for compatibility.
    private const int CriticalError = -1;
    private const int Success = 0;
    private const int ErrorCreatingTheCertificate = 1;
    private const int ErrorSavingTheCertificate = 2;
    private const int ErrorExportingTheCertificate = 3;
    private const int ErrorTrustingTheCertificate = 4;
    private const int ErrorUserCancelledTrustPrompt = 5;
    private const int ErrorNoValidCertificateFound = 6;
    private const int ErrorCertificateNotTrusted = 7;
    private const int ErrorCleaningUpCertificates = 8;
    private const int InvalidCertificateState = 9;
    private const int InvalidKeyExportFormat = 10;
    private const int ErrorImportingCertificate = 11;
    private const int MissingCertificateFile = 12;
    private const int FailedToLoadCertificate = 13;
    private const int NoDevelopmentHttpsCertificate = 14;
    private const int ExistingCertificatesPresent = 15;

    public static readonly TimeSpan HttpsCertificateValidity = TimeSpan.FromDays(365);

    public static int Run(string[] args)
    {
        try
        {
            // Parse args without a CLI framework — simple flag matching.
            var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? exportPath = null;
            string? password = null;
            string? format = null;
            string? importPath = null;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-ep" or "--export-path":
                        exportPath = GetNextArg(args, ref i);
                        break;
                    case "-p" or "--password":
                        password = GetNextArg(args, ref i);
                        break;
                    case "--format":
                        format = GetNextArg(args, ref i);
                        break;
                    case "-i" or "--import":
                        importPath = GetNextArg(args, ref i);
                        break;
                    default:
                        flags.Add(args[i]);
                        break;
                }
            }

            var hasCheck = flags.Contains("--check") || flags.Contains("-c");
            var hasTrust = flags.Contains("--trust") || flags.Contains("-t");
            var hasClean = flags.Contains("--clean");
            var hasCheckJson = flags.Contains("--check-trust-machine-readable");
            var hasNoPassword = flags.Contains("--no-password") || flags.Contains("-np");
            var hasVerbose = flags.Contains("--verbose") || flags.Contains("-v");
            var hasQuiet = flags.Contains("--quiet") || flags.Contains("-q");
            var hasHelp = flags.Contains("--help") || flags.Contains("-h");

            if (hasHelp)
            {
                ShowHelp();
                return Success;
            }

            // Route to the appropriate handler.
            if (hasCheckJson)
            {
                return CheckHttpsCertificateJsonOutput();
            }

            if (hasCheck)
            {
                return CheckHttpsCertificate(hasTrust, hasVerbose);
            }

            if (hasClean)
            {
                var cleanResult = CleanHttpsCertificates();
                if (cleanResult != Success || importPath is null)
                {
                    return cleanResult;
                }

                return ImportCertificate(importPath, password);
            }

            return EnsureHttpsCertificate(exportPath, password, hasNoPassword, hasTrust, format);
        }
        catch
        {
            return CriticalError;
        }
    }

    private static string? GetNextArg(string[] args, ref int index)
    {
        if (index + 1 < args.Length)
        {
            return args[++index];
        }

        return null;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("""
            Usage: aspire-managed dev-certs [options]

            Options:
              --check, -c                      Check for the existence of the certificate
              --check-trust-machine-readable   Check trust status and output JSON
              --trust, -t                      Trust the certificate
              --clean                          Remove all HTTPS development certificates
              --import, -i <path>              Import a certificate (use with --clean)
              --export-path, -ep <path>        Export the certificate to a file
              --password, -p <password>        Password for export/import
              --no-password, -np               Export PEM key without password
              --format <Pfx|Pem>               Export format (default: Pfx)
              --verbose, -v                    Display verbose output
              --quiet, -q                      Display warnings and errors only
              --help, -h                       Show this help
            """);
    }

    private static int CheckHttpsCertificateJsonOutput()
    {
        var availableCertificates = CertificateManager.Instance.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);
        var certReports = availableCertificates.Select(CertificateReport.FromX509Certificate2).ToList();
        Console.WriteLine(JsonSerializer.Serialize(certReports, DevCertsJsonContext.Default.ListCertificateReport));
        return Success;
    }

    private static int CheckHttpsCertificate(bool checkTrust, bool verbose)
    {
        var certificateManager = CertificateManager.Instance;
        var certificates = certificateManager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);

        if (certificates.Count == 0)
        {
            Console.WriteLine("No valid certificate found.");
            return ErrorNoValidCertificateFound;
        }

        var validCertificates = new List<X509Certificate2>();
        foreach (var certificate in certificates)
        {
            var status = certificateManager.CheckCertificateState(certificate);
            if (!status.Success)
            {
                Console.Error.WriteLine(status.FailureMessage);
                return InvalidCertificateState;
            }
            validCertificates.Add(certificate);
        }

        if (checkTrust)
        {
            var trustedCertificates = certificates
                .Where(cert => certificateManager.GetTrustLevel(cert) == CertificateManager.TrustLevel.Full)
                .ToList();

            if (trustedCertificates.Count == 0)
            {
                Console.WriteLine($"The following certificates were found, but none of them is trusted: {CertificateManager.ToCertificateDescription(certificates)}");
                if (!verbose)
                {
                    Console.WriteLine("Run the command with --verbose for more details.");
                }
                return ErrorCertificateNotTrusted;
            }

            ReportCertificates(trustedCertificates, "trusted");
        }
        else
        {
            ReportCertificates(validCertificates, "valid");
            Console.WriteLine("Run the command with both --check and --trust options to ensure that the certificate is not only valid but also trusted.");
        }

        return Success;
    }

    private static void ReportCertificates(IReadOnlyList<X509Certificate2> certificates, string certificateState)
    {
        Console.WriteLine(certificates.Count switch
        {
            1 => $"A {certificateState} certificate was found: {CertificateManager.GetDescription(certificates[0])}",
            _ => $"{certificates.Count} {certificateState} certificates were found: {CertificateManager.ToCertificateDescription(certificates)}"
        });
    }

    private static int CleanHttpsCertificates()
    {
        var manager = CertificateManager.Instance;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Cleaning HTTPS development certificates from the machine. A prompt might get " +
                    "displayed to confirm the removal of some of the certificates.");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("Cleaning HTTPS development certificates from the machine. This operation might " +
                    "require elevated privileges. If that is the case, a prompt for credentials will be displayed.");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Cleaning HTTPS development certificates from the machine. You may wish to update the " +
                    "SSL_CERT_DIR environment variable. " +
                    "See https://aka.ms/dev-certs-trust for more information.");
            }

            manager.CleanupHttpsCertificates();
            Console.WriteLine("HTTPS development certificates successfully removed from the machine.");
            return Success;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("There was an error trying to clean HTTPS development certificates on this machine.");
            Console.Error.WriteLine(e.Message);
            return ErrorCleaningUpCertificates;
        }
    }

    private static int ImportCertificate(string importPath, string? password)
    {
        if (password is null)
        {
            Console.Error.WriteLine("Password is required when importing a certificate.");
            return CriticalError;
        }

        var manager = CertificateManager.Instance;
        try
        {
            var result = manager.ImportCertificate(importPath, password);
            return result switch
            {
                ImportCertificateResult.Succeeded => PrintAndReturn("The certificate was successfully imported.", Success),
                ImportCertificateResult.CertificateFileMissing => PrintErrorAndReturn($"The certificate file '{importPath}' does not exist.", MissingCertificateFile),
                ImportCertificateResult.InvalidCertificate => PrintErrorAndReturn($"The provided certificate file '{importPath}' is not a valid PFX file or the password is incorrect.", FailedToLoadCertificate),
                ImportCertificateResult.NoDevelopmentHttpsCertificate => PrintErrorAndReturn($"The certificate at '{importPath}' is not a valid ASP.NET Core HTTPS development certificate.", NoDevelopmentHttpsCertificate),
                ImportCertificateResult.ExistingCertificatesPresent => PrintErrorAndReturn("There are one or more ASP.NET Core HTTPS development certificates present in the environment. Remove them before importing the given certificate.", ExistingCertificatesPresent),
                ImportCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore => PrintErrorAndReturn("There was an error saving the HTTPS developer certificate to the current user personal certificate store.", ErrorSavingTheCertificate),
                _ => Success
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"An unexpected error occurred: {exception}");
            return ErrorImportingCertificate;
        }
    }

    private static int EnsureHttpsCertificate(string? exportPath, string? password, bool noPassword, bool trust, string? exportFormat)
    {
        var now = DateTimeOffset.Now;
        var manager = CertificateManager.Instance;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var certificates = manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true, exportPath is not null);
            foreach (var certificate in certificates)
            {
                var status = manager.CheckCertificateState(certificate);
                if (!status.Success)
                {
                    Console.Error.WriteLine("One or more certificates might be in an invalid state. We will try to access the certificate key " +
                        "for each certificate and as a result you might be prompted one or more times to enter " +
                        "your password to access the user keychain. " +
                        "When that happens, select 'Always Allow' to grant access to the certificate key in the future.");
                }
                break;
            }
        }

        if (trust)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.Error.WriteLine("Trusting the HTTPS development certificate was requested. If the certificate is not " +
                    "already trusted we will run the following command:" + Environment.NewLine +
                    "'security add-trusted-cert -p basic -p ssl -k <<login-keychain>> <<certificate>>'" +
                    Environment.NewLine + "This command might prompt you for your password to install the certificate " +
                    "on the keychain. To undo these changes: 'security remove-trusted-cert <<certificate>>'" + Environment.NewLine);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Error.WriteLine("Trusting the HTTPS development certificate was requested. A confirmation prompt will be displayed " +
                    "if the certificate was not previously trusted. Click yes on the prompt to trust the certificate.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.Error.WriteLine("Trusting the HTTPS development certificate was requested. " +
                    "Trust is per-user and may require additional configuration. " +
                    "See https://aka.ms/dev-certs-trust for more information.");
            }
        }

        var format = CertificateKeyExportFormat.Pfx;
        if (exportFormat is not null && !Enum.TryParse(exportFormat, ignoreCase: true, out format))
        {
            Console.Error.WriteLine($"Unknown key format '{exportFormat}'.");
            return InvalidKeyExportFormat;
        }

        var result = manager.EnsureAspNetCoreHttpsDevelopmentCertificate(
            now,
            now.Add(HttpsCertificateValidity),
            exportPath,
            trust,
            password is not null || (noPassword && format == CertificateKeyExportFormat.Pem),
            password,
            exportFormat is not null ? format : CertificateKeyExportFormat.Pfx);

        return result switch
        {
            EnsureCertificateResult.Succeeded => PrintAndReturn("The HTTPS developer certificate was generated successfully.", Success),
            EnsureCertificateResult.ValidCertificatePresent => PrintAndReturn("A valid HTTPS certificate is already present.", Success),
            EnsureCertificateResult.ErrorCreatingTheCertificate => PrintErrorAndReturn("There was an error creating the HTTPS developer certificate.", ErrorCreatingTheCertificate),
            EnsureCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore => PrintErrorAndReturn("There was an error saving the HTTPS developer certificate to the current user personal certificate store.", ErrorSavingTheCertificate),
            EnsureCertificateResult.ErrorExportingTheCertificate or EnsureCertificateResult.ErrorExportingTheCertificateToNonExistentDirectory => PrintErrorAndReturn("There was an error exporting the HTTPS developer certificate to a file.", ErrorExportingTheCertificate),
            EnsureCertificateResult.PartiallyFailedToTrustTheCertificate => PrintErrorAndReturn("There was an error trusting the HTTPS developer certificate. It will be trusted by some clients but not by others.", ErrorTrustingTheCertificate),
            EnsureCertificateResult.FailedToTrustTheCertificate => PrintErrorAndReturn("There was an error trusting the HTTPS developer certificate.", ErrorTrustingTheCertificate),
            EnsureCertificateResult.UserCancelledTrustStep => PrintErrorAndReturn("The user cancelled the trust step.", ErrorUserCancelledTrustPrompt),
            EnsureCertificateResult.ExistingHttpsCertificateTrusted => PrintAndReturn("Successfully trusted the existing HTTPS certificate.", Success),
            EnsureCertificateResult.NewHttpsCertificateTrusted => PrintAndReturn("Successfully created and trusted a new HTTPS certificate.", Success),
            _ => PrintErrorAndReturn("Something went wrong. The HTTPS developer certificate could not be created.", CriticalError)
        };
    }

    private static int PrintAndReturn(string message, int exitCode)
    {
        Console.WriteLine(message);
        return exitCode;
    }

    private static int PrintErrorAndReturn(string message, int exitCode)
    {
        Console.Error.WriteLine(message);
        return exitCode;
    }
}

/// <summary>
/// JSON-serializable certificate report matching the <c>dotnet dev-certs https --check-trust-machine-readable</c> output format.
/// </summary>
internal sealed class CertificateReport
{
    public string? Thumbprint { get; init; }
    public string? Subject { get; init; }
    public List<string>? X509SubjectAlternativeNameExtension { get; init; }
    public int Version { get; init; }
    public DateTime ValidityNotBefore { get; init; }
    public DateTime ValidityNotAfter { get; init; }
    public bool IsHttpsDevelopmentCertificate { get; init; }
    public bool IsExportable { get; init; }
    public string? TrustLevel { get; init; }

    public static CertificateReport FromX509Certificate2(X509Certificate2 cert)
    {
        var certificateManager = CertificateManager.Instance;
        var status = certificateManager.CheckCertificateState(cert);
        string statusString;
        if (!status.Success)
        {
            statusString = "Invalid";
        }
        else
        {
            var trustStatus = certificateManager.GetTrustLevel(cert);
            statusString = trustStatus.ToString();
        }

        return new CertificateReport
        {
            Thumbprint = cert.Thumbprint,
            Subject = cert.Subject,
            X509SubjectAlternativeNameExtension = GetSanExtension(cert),
            Version = CertificateManager.GetCertificateVersion(cert),
            ValidityNotBefore = cert.NotBefore,
            ValidityNotAfter = cert.NotAfter,
            IsHttpsDevelopmentCertificate = CertificateManager.IsHttpsDevelopmentCertificate(cert),
            IsExportable = certificateManager.IsExportable(cert),
            TrustLevel = statusString
        };

        static List<string> GetSanExtension(X509Certificate2 cert)
        {
            var dnsNames = new List<string>();
            foreach (var extension in cert.Extensions)
            {
                if (extension is X509SubjectAlternativeNameExtension sanExtension)
                {
                    foreach (var dns in sanExtension.EnumerateDnsNames())
                    {
                        dnsNames.Add(dns);
                    }
                }
            }
            return dnsNames;
        }
    }
}

[JsonSerializable(typeof(List<CertificateReport>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class DevCertsJsonContext : JsonSerializerContext;
