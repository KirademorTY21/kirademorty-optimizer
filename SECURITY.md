# Security policy

## Supported versions

| Version | Supported |
|---|---|
| 1.2.x | ✅ |
| 1.1.x and older | ❌ |

## Reporting a vulnerability

**Do not open a public issue.** Use GitHub's private vulnerability reporting:

1. Open the repository on GitHub.
2. Go to **Security** → **Advisories** → **Report a vulnerability**.
3. Describe the issue and, if possible, include a minimal proof of concept.

You can expect an initial reply as soon as possible; this is a volunteer project, so please allow
a few days. If the report is valid, a fix and a credit (if you want one) will be published in the
release notes.

## What is in scope

- Privilege escalation or code execution beyond the intended admin-level tweaks.
- A tweak whose undo does **not** restore the exact previous state, or an undo script that can be
  tricked into running unintended commands.
- Injection or script execution through the exported PowerShell undo file.
- HTML/script injection in the generated report.
- Any network transmission of user data (the only network use is an ICMP ping to `1.1.1.1` for
  the latency measurement).
- Leaking of machine identifiers (computer name, user name, serial numbers) in the UI or report.

## What is out of scope

- **SmartScreen / antivirus warnings.** The executable is unsigned. The full source and a
  reproducible `build.ps1` are provided; a code-signing certificate is not.
- Antivirus false positives — report those to the vendor and, if useful, open an issue with the
  detection name.
- Tweaks that intentionally reduce a security feature (for example VBS/HVCI). These are marked
  **advanced**, documented in the app and in the README, and are never applied automatically.
- Cosmetic issues, missing translations, or feature requests — use normal issues.
- Problems caused by other "optimizers", manual registry edits or unsupported Windows builds.

## Design notes for reviewers

- The app requests administrator rights through `app.manifest` and does nothing silently: every
  change is triggered by a button.
- All state lives in `%LOCALAPPDATA%\KiraDemorTY Optimizer` (`settings.ini`, `journal.tsv`,
  `Backups\`). Nothing is written to `Program Files` or to the registry outside the documented
  tweak keys.
- There is no auto-update, no crash reporting and no analytics.
- The report is a static HTML file written locally; it is never uploaded.
- The undo script is generated from the journal and starts with an admin guard.
