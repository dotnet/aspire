using System;
using Microsoft.AspNetCore.Certificates.Generation;

// Test if the circular validation in property setters causes issues during constructor
var mgr = new TestCertificateManager();
Console.WriteLine($"Success: Version={mgr.AspNetHttpsCertificateVersion}, MinVersion={mgr.MinimumAspNetHttpsCertificateVersion}");

class TestCertificateManager : CertificateManager
{
    public TestCertificateManager() : base("CN=Test", 6, 4)
    {
    }
}
