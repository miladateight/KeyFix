# Code signing policy

This document describes who can put code into KeyFix, who can authorise a signature over the resulting binary, and what a user can verify about a file they downloaded.

> **Status:** SignPath Foundation has not issued a certificate for this project yet. Everything below describes how signing works once it does; delete this note when the first signed release ships.

## Attribution

Free code signing is provided by [SignPath.io](https://signpath.io), certificate by the [SignPath Foundation](https://signpath.org).

The certificate belongs to the Foundation, not to this project, so a signed installer identifies **SignPath Foundation** as the publisher. That is how the Foundation vouches for open-source projects, and it is the trade-off that makes signing available without a commercial certificate.

## Project roles

KeyFix is maintained by one person, so the same name appears in every role. Naming them anyway is the point of this document: it says exactly who can cause a signature to exist.

| Role | Who |
|---|---|
| Committers | Milad Ateight ([@miladateight](https://github.com/miladateight)) |
| Reviewers | Milad Ateight |
| Approvers | Milad Ateight |

Dependabot opens dependency-update pull requests. It cannot merge them and it cannot approve a signing request; every such pull request is reviewed and merged by a human committer.

Two-factor authentication is required on the GitHub account with write access to this repository.

## Repository

Source code: <https://github.com/miladateight/KeyFix>

Everything that ends up in a release is built from that repository. The project has no proprietary components, no commercial edition, and no dual licence: it is MIT throughout. Third-party dependencies and their licences are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and in the SBOM published with each release.

KeyFix installs a low-level keyboard hook, which is a capability that deserves scrutiny rather than trust. What it does with what it sees is described in the privacy policy below, and the whole of it is readable in this repository.

## Privacy policy

<https://github.com/miladateight/KeyFix/blob/main/docs/PRIVACY.md>

## Release process

Releases are cut from a git tag by [`.github/workflows/release.yml`](../.github/workflows/release.yml) on a GitHub-hosted runner. No release artifact is ever built on a developer machine.

1. A tag `vX.Y.Z` is pushed by a committer.
2. The workflow refuses the tag unless it matches the version declared in both project files and in `installer/KeyboardLanguageGuard.iss`.
3. The solution is built and the full test suite runs on a clean checkout.
4. The installer is compiled with Inno Setup.
5. A CycloneDX SBOM is generated from the solution.
6. The installer is submitted to SignPath for signing. **A human approver must approve that request manually** — signing is never automatic, which is the point of the step.
7. SHA-256 checksums are taken after signing, because a signature changes the file.
8. GitHub attests the build provenance, binding the artifact to this repository, this workflow and this commit.
9. The release is published with the installer, its checksum, and the SBOM.

## What a user can verify

A downloaded installer can be checked two independent ways.

The signature, in the file's Properties → Digital Signatures tab, or:

```powershell
Get-AuthenticodeSignature .\KeyFixSetup-1.0.0.exe | Format-List
```

And the provenance, which says the file came out of this repository rather than merely being signed by someone:

```bash
gh attestation verify KeyFixSetup-1.0.0.exe --repo miladateight/KeyFix
```

The checksum published with the release covers the same file after signing.

## Reporting a problem

Security issues go to the process described in [SECURITY.md](../.github/SECURITY.md). If you believe a signed artifact does not match this policy — a release you cannot verify, or a signature on something that was never tagged here — report it the same way and treat the file as untrusted until it is resolved.
