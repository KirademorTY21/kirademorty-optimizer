# Changelog

All notable changes to KiraDemorTY Optimizer are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/).

## [1.2.0] - 2026-10-26

### Added

- **Welcome page** on first launch: brand mark, what the tool is (and is not), three promises
  (restore point, exact undo, honest numbers) and a channel strip. It becomes the only visible
  surface while open, supports RTL mirroring, and can be reopened from Settings or with **F1**
  (`Esc` closes it). The first-run flag is stored in `settings.ini` as `intro`.
- **Channel link everywhere**: sidebar button, welcome page, Settings card and the HTML report
  footer, all pointing at `https://www.youtube.com/@KIRA-DEMORTY`.
- **Ten more tweaks (33 total)**: disable Fast Startup, stop peer-to-peer update sharing,
  disable Windows Error Reporting, hide the Game Bar startup panel, high-resolution system timer
  (needs a restart), keep the SSD awake, pause Automatic Maintenance, disable Windows tips and
  suggestions, release the reserved QoS bandwidth, and disable the DiagTrack telemetry service
  (fails with a clear message when the service is absent).
- **Tweak filters**: All / Safe / Moderate / Advanced chips above the list plus a live
  "applied of total" counter and an empty-state message when a filter matches nothing.
- **Pending-reboot badge** in the top bar whenever a change is waiting for a restart; it clears
  itself when the boot time moves forward. Clicking it opens the tweaks page.
- **Keyboard shortcuts**: `Ctrl+1`..`Ctrl+7` (numeric row or numpad) for pages, `F1` for the
  welcome page, `Esc` to dismiss it.

### Changed

- Version, About text, sidebar badge, report header and footer now say 1.2.
- The self test grew to 51 checks (catalog size, unique ids, bilingual strings, reboot flags,
  the brand URL and a real paint of the welcome overlay).

### Fixed

- **Pages slid 80 px under the top bar.** `Dock` laid the `Fill` host out before the docked
  top/status bars, so every page was drawn starting at the window's top edge behind the header.
  The shell now sets explicit bounds for sidebar, top bar, status bar and page host on resize.
- **Pending-reboot pill was painted underneath the page heading.** The heading label reserved a
  fixed width, so the pill's left side was hidden; the reserved width now follows the actual
  status pills and reflows when the reboot notice appears.
- Settings page scrolled content kept the channel card reachable; the table now reports its real
  extent to the scroller.
- **The HTML report no longer prints the computer name.** Reports are meant to be shared; the
  header now shows only the timestamp, and the footer links both the channel and the source
  repository.
- **The data folder is displayed as `%LOCALAPPDATA%\KiraDemorTY Optimizer`** on the backup and
  settings cards instead of the expanded path, so screenshots and streams do not expose the
  Windows account name. The button still opens the real folder.
- The About card reads the repository label from `Brand.cs` instead of a hard-coded string, so a
  fork only needs one edit.

### Known limitations

- The executable is unsigned, so SmartScreen may warn on first launch.
- HAGS, VBS/HVCI and the high-resolution timer need a Windows restart.
- Nagle changes apply to new connections; reconnect or restart the game.
- No code signing, auto-update or telemetry in this release.

## [1.1.0] - 2026-10-26

### Added

- **Diagnostics page** with 11 read-only checks: VBS/HVCI, power plan, CPU limits, core
  parking, Game DVR, Game Mode, HAGS, network throttling, TRIM, system-drive space and GPU
  temperature. Severity pills (OK / ATTENTION / PROBLEM), worst first, each fix reusing the
  normal journaled tweak path.
- **Benchmark page** measuring memory latency, 4K random read, CPU integer throughput, ping
  and jitter on this machine. The first measurement is stored as the baseline; the table shows
  before, now and the percentage change with honest colouring, and the pair is also written
  into the HTML report.
- **Undo script export**: a standalone PowerShell file with an admin guard that reverts every
  journaled registry value, created value/key and command, so changes can be undone without
  the app.
- Seven more reversible tweaks (23 total): mouse acceleration, visual effects for best
  performance, Windows transparency, background Store apps, instant menus, web search
  suggestions and SSD TRIM.
- Report: before/after benchmark table and a vector brand mark matching the icon.
- `tools\make-icon.ps1` icon generator and `.github/workflows/build.yml` CI.

### Changed

- **New application icon**: dark rounded tile with a warm inner glow, a thin orange rim and a
  cream-to-orange gradient **K** drawn as geometry. The letter is never black and stays legible
  at 16 px; seven sizes (16-256) are embedded, DIB below 48 px and PNG above.
- Navigation now has seven entries with new pulse (diagnostics) and stopwatch (benchmark) icons.
- The version, About text, sidebar badge and report footer all say 1.1.

### Fixed

- Undo-script test coverage slipped behind the journal code; the export is now verified for the
  admin guard, affected key and removal commands.
- Sparse benchmark table: rows are centred with header/row rules instead of leaving a large
  empty area under the numbers.
- Screenshot/self-test harnesses pumped messages with `DoEvents`, which replaces the WinForms
  synchronization context and made async page loads appear to hang; they now run a real message
  loop.

### Known limitations

- The executable is unsigned, so SmartScreen may warn on first launch.
- HAGS and VBS/HVCI changes need a Windows restart.
- Nagle changes apply to new connections; reconnect or restart the game.
- No code signing, auto-update or telemetry in this release.

## [1.0.0] - 2026-10-26

First public release.

### Added

- Bilingual interface (Arabic / English) that switches at runtime without restarting.
- Dashboard with live CPU, memory, GPU, system-drive and network readings,
  trend sparklines, a health ring and a full hardware summary.
- 16 reversible performance tweaks:
  power plan, CPU min/max state, core parking, PCIe ASPM, USB selective suspend,
  Power Throttling, hardware GPU scheduling (HAGS), Game Mode, Game DVR,
  fullscreen optimizations, MMCSS / Games task priority, network throttling,
  `Win32PrioritySeparation`, startup application delay, Nagle and VBS/HVCI.
- "Apply Recommended" profile that applies every safe and moderate tweak and
  leaves advanced ones to the user.
- Change journal with exact previous values (type and data) for every registry
  edit, plus undo commands for power and boot settings.
- Per-tweak undo, global undo, and an automatic Windows restore point before the
  first change of a session.
- HTML diagnostic report (bilingual, dark theme, print to PDF friendly) listing
  hardware, live readings, the state of all tweaks and the precise change log.
- `TRIM` and `VBS/HVCI` status checks in the dashboard and the report.
- `build.ps1` using the in-box .NET Framework compiler — no SDK, no installer,
  single ~160 KB executable.
- `tools\run-selftest.ps1` engine test covering journal apply/undo, registry
  type round-trip, report generation and power-setting detection.

### Changed

- Visual identity switched to the KiraDemorTY orange brand: accent `#FF8A1E`,
  ember `#FF3F1F`, warm dark neutrals, orange logo glow and toggle states.
- New application icon (rounded orange gradient + dark "K") generated at seven
  sizes (16-256 px) for crisp taskbar, Explorer and Alt-Tab rendering.
- The HTML report uses the same orange palette and mark.
- Arabic page titles and subtitles are now right-aligned; WinForms used to
  mirror `TextAlign` because of `RightToLeft`, which left them on the left side.

### Fixed during development

- Power setting detection read the "possible setting range" instead of the
  current AC index, which reported correct tweaks as off. Values are now read
  from the power-scheme registry; `powercfg` text parsing remains as fallback.
- PCIe ASPM and USB selective suspend were writing to the processor subgroup
  instead of their own subgroups, so they silently failed to apply.
- Layout: pages stayed at their design-time size until the window was resized.
- Text rendering: theme helper name collision with `Control.Text`.
- Crash when a rounded-path control was resized to zero width or height.
- Crash when the first network probe was queued before the main window handle
  existed.
- Report preview panel overflowed its card until the page was resized.
- Native scrollbars and the report text box rendered light grey inside the dark
  theme; they now follow dark mode.

### Known limitations

- The executable is unsigned, so SmartScreen may warn on first launch.
- HAGS and VBS/HVCI changes need a Windows restart.
- Nagle changes apply to new connections; reconnect or restart the game.
- No code signing, auto-update or telemetry in this release.
