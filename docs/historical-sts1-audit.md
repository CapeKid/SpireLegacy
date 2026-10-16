# Retired entry-point isolation

The supported mod targets Slay the Spire 2. Old launchers and Python commands refuse execution before importing dependencies, changing files or starting a game. They also refuse imports, without an environment-variable bypass. Obsolete generation, extraction and publishing implementations, original prompt archives and old listing configurations have been removed from the current tree. They are not supported APIs. Published Git history has not been rewritten.

Current builds use `scripts/sts2-generate.py` and `sts2/SpireLegacy.csproj`. Current release configuration is `sts2/melty.json` and `sts2/workshop.json`. The unsupported Java source is excluded from native compilation and packaging; its saved identifiers have not been migrated into current saves.

Closed release manifests reject retired executable/source files, readers, caches, saves and historical directories, including attempts to declare them as future inputs. Native mod assets and the pinned BaseLib dependency remain allowed.

Run `python scripts/check-historical-isolation.py` for disposable sentinel tests of the retired Python commands/imports, PowerShell wrappers, Windows/Bash launchers and release exclusions. No game, Steam library or personal profile is opened. `python scripts/historical-audit-inventory.py` writes a current source inventory under ignored `private/`; old audit snapshots have been removed. These checks establish entry-point and package isolation, not device gameplay or the safety of old releases already installed elsewhere.
