# MBlue Testing Project

## Repository Overview

**mblue-test** is a .NET 10 console application for exercising MBlue
components. `src/Main.cs` composes the host, configuration, logging, REST
handlers, and services supplied by the MBlue component packages.

Dependencies:
- .NET 10 SDK
- Microsoft.Extensions.* (for DI, configuration, hosting)
- NLog.Extensions.Logging

## Build and validation

Use the narrowest relevant command:

```bash
dotnet build src/mblue.test.csproj
dotnet run --project src/mblue.test.csproj
npm run build
```

Run `npm install` before the TypeScript build when `node_modules/` is absent.
There is currently no committed automated test project; add focused tests with
behavioral changes when practical.

## Project conventions

- Target framework is `net10.0`, nullable reference types and implicit usings
  are enabled.
- Follow `.editorconfig`: four-space indentation for C#, two spaces for
  JavaScript, and LF line endings.
- Keep the existing file header and Mozilla Public License 2.0 notice in new
  C# source files.
- Preserve the established namespace style:
  `org.herbal3d.mblue` and its sub-namespaces.
- Use constructor injection and register services in `Main.cs`. Resolve
  dependencies through the container; do not add service locators or global
  mutable state.
- Use `MBLogger<T>` and the existing `MBLogLevel` categories for application
  logging instead of introducing another logging mechanism.
- Keep configuration defaults in `MBlue.json`. Use
  `MBlue.Debug.json` only for Debug-specific overrides. Do not add credentials
  or machine-specific values to tracked configuration.

## REST and UI assets

- REST handlers live in `src/Rest/`. Register handlers and their dependencies
  through the existing `RestHandlerFactory` and DI setup.
- Browser assets live in `assets/`. TypeScript files in `assets/web/` compile
  in place to tracked `.js` and `.js.map` files. Edit `.ts` files, run
  `npm run build`, and include regenerated output when TypeScript changes.
- Do not manually edit generated JavaScript or source maps unless there is no
  corresponding TypeScript source.

## MBlue component dependencies

The project uses sibling MBlue repositories when available at:

- `../../mblue-common`
- `../../mblue-ecm`
- `../../mblue-comm`

Otherwise it restores their NuGet packages. Preserve the conditional project
reference/package-reference behavior in `src/mblue.test.csproj`. Use the
`dotnet` CLI to add, remove, or update NuGet packages.

---