# WAMHelper

Small C# library used in PSMALNET Powershell module to interact with EntraID with the Windows WAM OAuth flow.

# How to build it?

- dotnet new classlib --name WAMHelper -f net6.0
- dotnet add WAMHelper package Microsoft.Identity.Client
- dotnet build -f net6.0 WAMHelper (with or without WAMHelper at the end)
