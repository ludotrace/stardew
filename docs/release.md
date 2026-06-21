# Release Process

Releases are built locally (dotnet build runs on your machine), then tagged to trigger CI.
GitHub Actions handles zip packaging, GitHub Release creation, and NexusMods upload.

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

### 3. Build

```bash
make build
```

This reads `VERSION`, injects the version number into `manifest.json`, runs
`dotnet build -c Release`, reverts `manifest.json` to the `__VERSION__` placeholder,
then copies `LudoTrace.dll` and a versioned `manifest.json` into `dist/LudoTrace/`.

### 4. Commit and open a PR

```bash
git add VERSION dist/LudoTrace/LudoTrace.dll dist/LudoTrace/manifest.json
git commit -m "Release vX.Y.Z"
git push -u origin release/vX.Y.Z
```

Open a PR, get it merged to main.

### 5. Tag and release

From main after the PR merges:

```bash
git checkout main && git pull
make release
```

`make release` guards clean state and that you're on main, then creates and pushes the
version tag. GitHub Actions picks up the tag and:

1. Zips `dist/LudoTrace/` + `docs/coaching_prompt.md` into `LudoTrace-Stardew-vX.Y.Z.zip`
2. Creates a GitHub Release and attaches the zip
3. Updates `CHANGELOG.md` on main with the release notes
4. Uploads the zip to NexusMods

### 6. Update NexusMods mod page version (manual)

The Files page changelog updates automatically. The version badge on the mod header has
no API endpoint — update it manually in Nexus mod settings after the release lands.

---

## NexusMods

Mod page: https://www.nexusmods.com/stardewvalley/mods/48026 (mod ID 48026, hardcoded in
`release.yml`). CI uploads automatically on tag push once `NEXUSMODS_API_KEY` is set as
a repository secret (nexusmods.com → Account → API Keys).

---

## What each make target does

| Target | What it does |
|--------|-------------|
| `make build` | Injects version, compiles DLL, reverts manifest, copies to `dist/` |
| `make release` | Guards clean state + main branch, tags, pushes tag to trigger CI |

---

## Notes

- `VERSION` is the single source of truth for the version string
- `manifest.json` always contains `__VERSION__` in source control — the placeholder is
  only substituted during `make build` and immediately reverted
- `dist/LudoTrace/` is committed — CI zips whatever is in `dist/` at tag time, so build
  before tagging
- Never tag before building — the tag push triggers CI immediately
