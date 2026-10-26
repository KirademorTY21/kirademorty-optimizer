# KiraDemorTY Optimizer v1.2.0

**A small, honest Windows stutter and latency tuner.** One executable, no installer, no telemetry,
no fake FPS numbers. Every change is journaled, reversible and measured before and after.

## Download

- `KiraDemorTY-Optimizer.exe` — portable, Windows 10 1809+ / Windows 11 (x64), ~282 KB.
- `SHA256.txt` — verify the file after downloading:

  ```powershell
  Get-FileHash .\KiraDemorTY-Optimizer.exe -Algorithm SHA256
  ```

Run it as Administrator. The executable is **unsigned**, so SmartScreen may warn on first launch;
the full source is in this repository and `build.ps1` reproduces the binary.

## What's new

- **Welcome page** on first launch — what the tool is, three safety rules, the channel link.
  Reopen it any time with **F1** (`Esc` closes it).
- **33 tweaks** (10 new): Fast Startup off, delivery-optimization peer sharing off, Windows Error
  Reporting off, Game Bar startup panel off, high-resolution timer (restart), SSD keep-awake,
  Automatic Maintenance paused, Windows tips off, QoS reserved bandwidth released, DiagTrack off.
- **Tweak filters** — All / Safe / Moderate / Advanced, with a live "applied of total" counter.
- **Pending-reboot badge** in the top bar; clicking it opens the tweaks that need a restart.
- **Keyboard shortcuts** — `Ctrl+1`..`Ctrl+7` for pages, `F1` for the welcome page.
- **Channel links** in the sidebar, welcome page, settings and report footer.

## Fixed

- Pages no longer slide under the top bar.
- The reboot pill is no longer hidden behind the page heading.
- The HTML report no longer contains the computer name; the data folder is shown as
  `%LOCALAPPDATA%\KiraDemorTY Optimizer`, so screenshots and streams do not expose the account
  name.
- Settings scrolling keeps the channel card reachable.

## Safety model

- A restore point is created before the first change.
- Every registry value is stored with its exact type and previous data.
- One click reverts everything, newest first.
- A standalone PowerShell undo script can be exported and run even after the app is deleted.
- Diagnostics are read-only; every fix goes through the same journaled path.

## Known limitations

- HAGS, VBS/HVCI and the high-resolution timer only take effect after a restart.
- Nagle changes apply to new connections — reconnect or restart the game.
- Some tweaks will do nothing on your machine. That is a valid result; the benchmark reports it.
- No code signing, auto-update or telemetry in this release.
- Do not disable VBS/HVCI if you use Hyper-V, WSL2, Docker or Windows Sandbox.

## Checks

- `.\build.ps1` — release build with the in-box C# compiler, no SDK required.
- `.\tools\run-selftest.ps1` — 51 engine checks, all passing.

**YouTube:** <https://www.youtube.com/@KIRA-DEMORTY> · **Source:** <https://github.com/KirademorTY21/kirademorty-optimizer>
