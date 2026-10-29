# Crap4DotNet

A .NET global tool that computes **CRAP (Change Risk Anti-Patterns)** scores for your C# code.

CRAP combines cyclomatic complexity with code coverage to identify methods that are both complex and poorly tested — the riskiest code in your project.

## Install

```bash
dotnet tool install -g Crap4DotNet
```

## Usage

Requires .NET 10. Source inputs include `.cs`, directories, `.csproj`, `.sln`, and `.slnx`.

```bash
# Run tests and analyze in one step
dotnet-crap analyze ./MyApp.sln --run-tests

# MTP v2 (select Microsoft.Testing.Platform in the target's global.json)
dotnet-crap analyze ./MyApp.slnx --run-tests

# MTP with coverlet.MTP instead of Microsoft.Testing.Extensions.CodeCoverage
dotnet-crap analyze ./MyApp.slnx --run-tests --coverage-provider coverlet

# Analyze with explicit coverage file
dotnet-crap analyze ./src/MyProject --coverage ./TestResults/coverage.cobertura.xml

# Write report to file
dotnet-crap analyze ./src/MyProject --coverage ./coverage.xml --output report.json

# Custom threshold (default: 30)
dotnet-crap analyze ./src/MyProject --coverage ./coverage.xml --threshold 15

# Compare two reports
dotnet-crap diff before.json after.json
```

`--run-tests` defaults to Microsoft's coverage extension for MTP and `coverlet.collector`
for VSTest. Install the corresponding package in your test projects. It builds and
executes the target repository's code; only use it with repositories you trust.

Publish a single executable (replace the runtime identifier for your platform):

```bash
dotnet publish src/Crap4DotNet.Cli -c Release -r linux-x64 --self-contained true
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All methods are below the CRAP threshold |
| 1 | One or more methods exceed the CRAP threshold |
| 2 | Error (missing files, parse failure, invalid config) |

## CRAP Formula

```
CRAP(m) = complexity(m)^2 * (1 - coverage(m))^3 + complexity(m)
```

A method with complexity 1 and full coverage has a CRAP score of 1 (perfect).
A method with complexity 20 and 10% coverage has a CRAP score of ~312 (terrible).

## JSON Output

The tool outputs a JSON report with method-level CRAP scores, statistics, severity bands, and a namespace hierarchy. See the [specification](https://github.com/7Factor/crap4dotnet) for the full schema.
