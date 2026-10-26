# Contributing to KiraDemorTY Optimizer

Thanks for wanting to improve the tool. This project is deliberately small and boring:
a single WinForms executable, no dependencies, no telemetry. Please keep it that way.

## Ground rules

1. **No new dependencies.** The build uses the C# compiler that ships with Windows
   (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`). No NuGet, no SDK, no installer.
2. **C# 5 syntax only.** The in-box compiler does not understand string interpolation (`$"..."`),
   null-conditional operators (`?.`), `nameof`, expression-bodied members or tuples. Use
   `string.Format`, explicit null checks and classic properties.
3. **Every change must be reversible.** A tweak gets an apply path, an exact undo path and a
   `Verify` function, and it must go through `Tweak.Apply` / `Engine.SetReg` / `Engine.DeleteReg`
   so the journal and the exported undo script stay correct.
4. **No fake numbers.** Do not claim FPS gains, percentages or "stutter elimination". The app
   measures and reports; it does not advertise.
5. **No destructive or privacy-hostile tweaks.** No registry cleaning, no disabling Defender or
   Windows Update, no deleting user files, no network calls except the ICMP ping used by the
   benchmark.
6. **Bilingual by default.** Every user-visible string belongs in `src/Lang.cs` with both the
   Arabic and English text. The Arabic UI is mirrored right-to-left, so check both layouts.

## Build

```powershell
git clone https://github.com/KirademorTY21/kirademorty-optimizer.git
cd kirademorty-optimizer
powershell -ExecutionPolicy Bypass -File .\build.ps1        # release, admin manifest
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Dev   # dev build, no admin manifest
```

Output: `dist\KiraDemorTY-Optimizer.exe` (the `dist` folder is git-ignored).

## Tests

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-selftest.ps1
```

The self test applies and reverts real changes, writes a report, runs all diagnostics and the
benchmark, then restores your original journal. It must finish with `failed 0` before a pull
request is merged. Run it elevated; the CI runner is elevated by default.

## Adding a tweak

1. Add an entry to `src/Tweaks.cs` with a unique lowercase id, bilingual description, risk level
   (`safe` / `moderate` / `advanced`), a `Reboot` flag when the change only takes effect after a
   restart, and an undo that restores the **exact** previous type and data (or deletes a value
   that did not exist).
2. Use the existing helpers (`Engine.SetReg`, `Engine.DeleteReg`, `Engine.Run`) so the journal,
   the undo script and the report pick it up automatically.
3. Make `Verify` read the live state, not the journal.
4. Bump the "applied of total" counter automatically — it is derived from the catalog, but the
   README table and the self test's expected count must be updated by hand.
5. Add a check to `tools/SelfTest.cs` if the tweak has logic that can regress.
6. Update `README.md`, `CHANGELOG.md` and `docs/VIDEO-SCRIPT.md` if the count changes.

## Screenshots

UI screenshots live in `docs/screens/`. When you change a page, regenerate the affected image and
keep the same file names so the README and the video script keep working. Crop out anything that
identifies a machine or an account.

## Pull requests

- One topic per pull request; keep the diff readable.
- Describe **what** changed, **why**, and **how you tested it** (commands and result counts).
- Confirm that `build.ps1` and `run-selftest.ps1` both pass on Windows 10 or 11.
- By contributing you agree that your work is licensed under the project's MIT license.

## Security

Please do not open a public issue for a vulnerability. See [SECURITY.md](SECURITY.md).
