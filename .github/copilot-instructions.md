# MBlue Common Routine Library

## Repository Overview

**mblue-common** is a .NET 9.0 library providing common routines for MBlue projects.
It includes utilities for logging, configuration, dependency injection, and other shared functionality.

Dependencies:
- Dotnet 10.0 SDK
- Microsoft.Extensions.* (for DI, configuration, hosting)
- NLog 5.1.0 (for logging)

---

## Logging System: MBLogger<T>

Custom logging wrapper in [src/Logging/MBLogger.cs](src/Logging/MBLogger.cs):

```csharp
// Standard usage pattern
private readonly MBLogger<MyClass> m_log;

public MyClass(MBLogger<MyClass> pLog) {
    m_log = pLog;
}

// Custom KeeKee log levels (configured per-feature in appsettings.json)
m_log.Log(KLogLevel.DCOMM, "Communication detail: {0}", data);
m_log.Log(KLogLevel.DWORLDDETAIL, "World update detail");
```

**Feature flags** in `appsettings.json` → `MBLogger` section:
- `DCOMM`, `DCOMMDETAIL`: Communication layer
- `DWORLD`, `DWORLDDETAIL`: World/entity updates 
- `DRENDER`, `DRENDERDETAIL`: Rendering
- `DRESTDETAIL`, `DWORKQUEUEDETAIL`, `DUIDETAIL`

