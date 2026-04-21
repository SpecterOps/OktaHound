# Contributing to OktaHound

## Introduction

Thanks for your interest in improving OktaHound! This project is still evolving,
so contributions that improve documentation, reliability, platform support, or test coverage are especially valuable.
The guidelines below outline the minimum environment requirements, explain how the repository is organized, and describe the standard developer workflow.

## Requirements

- [.NET SDK 10.0+](https://dotnet.microsoft.com/en-us/download/dotnet/10.0): The .NET SDK is required to build the application and run tests locally. Windows, Linux, and macOS are all supported.
- A code editor or IDE with .NET support, such as [Visual Studio 2026+](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/).
- [Okta Organization](https://developer.okta.com/signup/): Read access to an Okta Workforce Identity Cloud tenant for functional testing.
- [BloodHound](https://bloodhound.specterops.io/get-started/introduction): Optional but recommended if you need to validate collector output.

## Directory Structure

```text
Root/
├─ .github/                                  # GitHub automation and CI workflows
│  └─ workflows/                             # CI and automation definitions
│     └─ autobuild.yml                       # CI build and test pipeline
├─ .vscode/                                  # Visual Studio Code workspace settings
├─ Build/                                    # Compiled binaries, publish outputs, and CI artifacts
├─ Src/                                      # Source code for the application and tests
│  ├─ SpecterOps.OktaHound/                  # Collector source code
│  ├─ SpecterOps.OktaHound.Tests/            # Unit tests
│  └─ Directory.Build.props                  # Shared build settings for all projects
├─ Roadmap.md                                # Project roadmap and development status
├─ global.json                               # .NET SDK version pinning
├─ OktaHound.slnx                            # Solution file encompassing the app and tests
└─ README.md                                 # High-level overview and background material
```

The documentation, schema, query, and automation assets that were removed from this repository now live in `SpecterOps/openhound-okta`.

## Dependencies

These libraries and frameworks are used in the OktaHound project:

- [Okta management SDK for .NET](https://github.com/okta/okta-sdk-dotnet) - Official Okta SDK for interacting with the Okta Management API.
- [Microsoft.Extensions.Logging.Console](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) - Console logging support built on the `ILogger` abstraction.
- [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) - A library for parsing command-line arguments.
- [MSTest.Sdk](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-sdk) and [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro) - The test SDK and runner used for unit tests.
- [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/api/system.text.json) - Used to generate pre-compiled high-performance BloodHound OpenGraph JSON serializers.

## Building from Source

1. Set the working directory to the repository root.

2. Build the source code:

   ```powershell
   dotnet build
   ```

   Build artifacts are written to `Build\bin\SpecterOps.OktaHound\debug`.

3. Create a self-contained (single) binary (example for Windows x64):

   ```powershell
   dotnet publish --runtime win-x64
   ```

   Build artifacts are written to `Build\publish\OktaHound\win-x64`.
   Adjust the [runtime identifier](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids) (`linux-x64`, `osx-arm64`, etc.) to match your target platform.

## Running Unit Tests

1. Set the working directory to the repository root.
2. Create the `okta.yaml` configuration file with the right credentials.
3. Run all tests:

   ```powershell
   dotnet test
   ```

## Additional Tips

- Run `dotnet format` (or your preferred analyzer) before opening a PR to ensure code-style consistency.
- The repository contains sample Okta configuration files ([okta.sample.oauth.yaml](Src/SpecterOps.OktaHound/okta.sample.oauth.yaml),
  [okta.sample.token.yaml](Src/SpecterOps.OktaHound/okta.sample.token.yaml)).
  Never commit actual secrets to the repository.
