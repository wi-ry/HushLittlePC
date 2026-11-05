---
name: msix-portable-release
description: 'Use when changing, building, or troubleshooting HushLittlePC MSIX packaging, Microsoft Store submissions, portable publishing, release versions, package manifests, or release workflows.'
---

# MSIX and Portable Releases

Use this workflow to keep Store/MSIX and portable releases consistent.

## Procedure

1. Inspect `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.github/scripts/package-msix.ps1`, `packaging/AppxManifest.xml`, and `HushLittlePC.csproj` before changing release behavior.
2. Identify which artifact is affected: CI validation, push-built MSIX, tagged release portable executable, or Store submission. Preserve the existing separation between the CI MSIX artifact and semantic-release publishing.
3. Keep manifest identity, publisher, display name, version, architecture, minimum Windows version, startup task ID, and toast activation configuration consistent with the app and packaging script. Do not put credentials or Store identity values in source; workflows supply configured repository variables and secrets.
4. Preserve the portable build's runtime choice and single-file behavior unless the request explicitly changes it. Verify output paths and filenames against the release workflow and README.
5. Make packaging edits minimal. For changes to startup or notification activation, also inspect `Settings.cs`, `Program.cs`, and `Notifier.cs` so packaged and unpackaged behavior remain aligned.
6. Run the Release build and unit tests. On Windows, run `.github/scripts/package-msix.ps1` only when the required MSIX identity environment variables are available; never substitute fabricated signing or Store credentials. State clearly if package creation or Store validation could not be run.

## Review checklist

- Version and artifact filenames are propagated from the release workflow, not hard-coded inconsistently.
- Manifest placeholders are replaced by the packaging script.
- MSIX-only capabilities, startup behavior, and activation declarations agree with application code.
- The portable executable still matches the documented runtime requirements.
- No secret values appear in scripts, logs, manifests, or committed files.
