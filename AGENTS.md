# BF4 Central

- Edit the WinForms C# project in `src/`; the compiler is the Windows x64 .NET Framework compiler invoked by `build.cmd`. Keep C# 5 compatibility.
- Run `test.cmd` for isolated tests. Tests use a simulated RCON and temporary files. Live game/server operations should be explicit and scoped to the selected installation.
- `scripts/build-installer.ps1` builds the Windows setup from the current `dist/BF4 Central.exe`. Use its `--self-test` mode before distribution. See `docs/DISTRIBUICAO.md`.
- Read `DESIGN.md` for native UI conventions. Keep local art in ignored `local-assets/`; builds without it must still work.
- The game, mod files, accounts, passwords, certificates, invites, and logs stay outside Git. Do not overwrite the user's active installation during tests. Preserve the distinction between verified server state and gameplay that has not been tested.
