# CodeDetective WinForms - VS2022 .NET Solution Package

## Open in Visual Studio 2022

1. Install Visual Studio 2022 with the **.NET desktop development** workload.
2. Install the .NET 8 SDK if it is not already installed.
3. Open `CodeDetective.WinForms.sln`.
4. Select `Debug|Any CPU` or `Release|Any CPU`.
5. Build the solution.

## Command-line build

```bat
dotnet restore CodeDetective.WinForms.sln
dotnet build CodeDetective.WinForms.sln -c Release
```

## Output executable

```text
CodeDetective.WinForms\bin\Release\net8.0-windows\CodeDetective.exe
```

## Publish package

```bat
publish-win-x64.bat
```

The publish output is written to:

```text
publish\win-x64
```

## Included Visual Studio files

- `CodeDetective.WinForms.sln`
- `CodeDetective.WinForms\CodeDetective.WinForms.csproj`
- `global.json`
- `Directory.Build.props`
- `.editorconfig`
- `build-release.bat`
- `build-debug.bat`
- `publish-win-x64.bat`
