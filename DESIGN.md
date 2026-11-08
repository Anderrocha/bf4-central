---
version: alpha
colors:
  background: '#E8EDF0'
  surface: '#FFFFFF'
  ink: '#1E3442'
  muted: '#4D626E'
  accent: '#A34D14'
  border: '#C4D0D7'
typography:
  body:
    fontFamily: 'Segoe UI'
    fontSize: '10pt'
  display:
    fontFamily: 'Bahnschrift'
    fontSize: '25pt'
  heading:
    fontFamily: 'Bahnschrift'
    fontSize: '17pt'
rounded:
  control: '0px'
spacing:
  shell: '26px'
  panel: '22px'
components:
  profileSelector:
    owner: 'Launcher.levels'
    behavior: 'native RadioButton, Appearance.Button'
  button:
    owner: 'Launcher.MakeButton'
  feedback:
    owner: 'StatusLabel'
---

## Overview
Native Windows utility for this user's BF4 LAN bot installation, in Brazilian Portuguese. Its single job is to choose and apply a behavioral preset with explicit restart and reversible backups. Visual reference: a military equipment setup sheet, pale blue-gray with an amber annotation. The five-position difficulty strip is the signature. No stock art, animation, neon or dashboard metrics.

## Colors
Theme in src/Launcher.cs is the runtime owner. The frontmatter mirrors its exact palette. Ink is the primary action color; amber identifies the behavioral summary. White surfaces group readable instructions. Errors use named Maroon plus text; no status depends on color alone.

## Typography
Theme.Body, Theme.Title and Theme.Heading implement the font roles. Use native text and labeled controls; preserve Portuguese accents.

## Layout
Single resizable screen: title, install folder, five profile selectors, behavior details, bot-count command, actions, persistent status. Minimum window 960 by 760; DPI scaling is native. Selector widths adapt to available width. A native desktop app does not define mobile breakpoints.

## Elevation & Depth
Flat surfaces and one native modal level for restart and restore. No shadows or animation.

## Shapes
Square profile buttons, quiet borders, native OS inputs.

## Components
Canonical controls are native WinForms TextBox, RadioButton, NumericUpDown and Button. Folder picker uses the OS. MakeButton owns repeated button styling. Confirm owns modal prompts, cancellation, initial focus and explicit action labels. StatusLabel owns persistent feedback and accessibility notifications. Config owns validation, hashes, backups and atomic updates; Server owns scoped restart.

## Do's and Don'ts
Show that aiming and bullet cadence stay unchanged. Keep count independent from difficulty. Save does not claim the profile is active. Never stop the client game or a server at another path. Preserve unrelated INI keys and current server command-line arguments. Never claim gameplay calibration from a config test.


### One-click startup
The primary action is Iniciar tudo e jogar. OneClick in src/OneClick.cs owns process-path checks, TCP-port ownership, serialized startup, loopback-only launch API and bounded waits. Reuse running components; only close EA Desktop when it owns the required local 3216 port. Unknown owners block startup. Never repeat POST /launch on an uncertain response. The desktop shortcut Jogar Battlefield 4 forwards --play. Administrative elevation resumes that action automatically. This path preserves the existing bot and graphics settings.


### Unified Central

### Friends on LAN
The fourth native tab extends Central.Button/AsyncButton, Field, Work and StatusLabel; no new token system. Host configuration is explicit and requires a closed client. Invite selection is staged and shows host identity/address before acceptance; files are bounded and validated. Network rules are program/Private/LocalSubnet scoped. No RCON secret is shared. Party state is separate from launch state and only reports grouped after server readback; offline, full squad, deferred respawn and paused-on-errors states remain visible. The native scrollable panel preserves keyboard navigation at minimum desktop size. Party monitoring is opt-in per session and ends with the window.
BF4 Central.exe is the user-facing entry point. The three native tabs own Game settings, the existing bot profile editor and Server RCON management. Server edits are staged locally; explicit save includes a snapshot, stale-list guard, readback and best-effort rollback. Current gameplay is never changed by connection/read tests. Native password inputs are masked and have a show/hide control. Single-instance signaling reuses the Central window. Underlying original launcher/backend/server remain installation dependencies and their windows are hidden after startup. Repeated buttons use Central.Button/AsyncButton; all status uses StatusLabel.
