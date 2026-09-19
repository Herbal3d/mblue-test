# MBlue Test

`mblue-test` is a .NET 10 console application used to exercise the MBlue 3D
world-storage components. It configures dependency injection, logging, MBlue
configuration, and a small REST/static-content host.

## Prerequisites

- .NET 10 SDK
- Node.js and npm (only for the TypeScript UI assets)

The project references local sibling repositories when they are available:

- `../../mblue-common`
- `../../mblue-ecm`
- `../../mblue-comm`

When a sibling repository is not present, the application restores the
corresponding NuGet package instead.

## Build and run

```bash
# Restore and compile the console application.
dotnet build src/mblue.test.csproj

# Start the test host.
dotnet run --project src/mblue.test.csproj

# Install UI build dependencies and compile TypeScript in assets/web/.
npm install
npm run build
```

The TypeScript compiler writes JavaScript and source maps beside the source
files under `assets/web/`. Edit the `.ts` files; regenerate the corresponding
`.js` and `.js.map` files with `npm run build`.

## Configuration

`MBlue.json` is the required base configuration. `MBlue.Debug.json` is
optionally loaded in Debug builds, and `MBlue.{Environment}.json` can override
settings for a hosting environment. Environment variables prefixed with
`MBlue_` and command-line arguments have higher precedence.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/` | .NET host, MBlue configuration, and REST handlers |
| `assets/` | Static content and TypeScript sources for the browser UI |
| `MBlue*.json` | Application and logging configuration |
| `.github/copilot-instructions.md` | Repository-specific AI contributor guidance |