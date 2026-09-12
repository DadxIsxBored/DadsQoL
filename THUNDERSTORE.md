# Thunderstore release

Run:

```powershell
.\build.ps1 -Package
```

Upload the single ZIP written to `dist/`. The ZIP contains all required files at its root.

Before release, update the version in all of these locations:

- `DadsQoL.csproj`
- `src/AssemblyInfo.cs`
- `src/DadsQoLPlugin.cs`
- `package/manifest.json`
- `CHANGELOG.md`
