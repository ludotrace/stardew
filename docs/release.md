# Release Process

CI builds and packages the mod on tag push. The only local step is bumping `VERSION`,
opening a PR, and tagging from main.

## Steps

### 1. Prepare a release branch

```bash
git checkout main && git pull
git checkout -b release/vX.Y.Z
```

### 2. Update VERSION

Edit `VERSION` to the new version string:

```
v0.2.0
```

### 3. Commit and open a PR

```bash
git add VERSION
git commit -m "Release vX.Y.Z"
git push -u origin release/vX.Y.Z
```

Open a PR, get it merged to main.

### 4. Tag and release

From main after the PR merges:

```bash
git checkout main && git pull
make release
```

`make release` guards clean state and that you're on main, then creates and pushes the
version tag. GitHub Actions picks up the tag and:

1. Installs .NET 6, injects the version into `manifest.json`
2. Runs `dotnet build -c Release`
3. Zips `LudoTrace.dll` + `manifest.json` + `docs/coaching_prompt.md`
4. Creates a GitHub Release and attaches the zip
5. Updates `CHANGELOG.md` on main with the release notes
6. Uploads the zip to NexusMods

### 5. Update NexusMods mod page version (manual)

The Files page changelog updates automatically. The version badge on the mod header has
no API endpoint — update it manually in Nexus mod settings after the release lands.

---

## NexusMods

Mod page: https://www.nexusmods.com/stardewvalley/mods/48026 (mod ID 48026, hardcoded in
`release.yml`). CI uploads automatically on tag push once `NEXUSMODS_API_KEY` is set as
a repository secret (nexusmods.com → Account → API Keys).

---

## Local build (for testing)

`make build` is for local testing only — it is not part of the release flow:

```bash
make build   # injects VERSION, builds, reverts manifest, copies DLL to dist/
```

`dist/` is gitignored. CI builds from source.

---

## What each make target does

| Target | What it does |
|--------|-------------|
| `make build` | Injects version, builds, reverts manifest, copies to `dist/` (local only) |
| `make release` | Guards clean state + main branch, tags, pushes tag to trigger CI |

---

## Notes

- `VERSION` is the single source of truth for the version string
- `manifest.json` always contains `__VERSION__` in source control
- CI injects the version at build time; the source file is never modified on main
