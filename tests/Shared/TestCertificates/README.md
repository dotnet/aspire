# Test certificates

Unless otherwise stated, these certificates are copied from dotnet/aspnetcore:

https://github.com/dotnet/aspnetcore/tree/main/src/Shared/TestCertificates

If you add more, please check whether they need CredScan suppressions by copying any relevant entries from [dotnet/aspnetcore's CredScan suppressions](https://github.com/dotnet/aspnetcore/blob/main/.config/CredScanSuppressions.json) to `.config\CredScanSuppressions.json` in this repo. Certificates containing only public keys do not require suppression.
