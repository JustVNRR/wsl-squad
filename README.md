# WSL Squad

WSL Squad builds and manages a fleet of WSL 2 distributions from Docker images.

A distro is built from a Dockerfile, a catalog recipe, a saved image or a registry
reference, imported into WSL, dressed with optional tool packs, and then managed
from a scriptable CLI (an interactive menu is also available). The application is
written in C# on .NET 10.

> The WPF interface is still a scaffold: the CLI, the Core services and everything
> they exercise are implemented; the graphical shell will follow.

## Requirements

- Windows 10/11 with WSL 2
- Docker Desktop (running)
- .NET 10 SDK

## Build and test

```powershell
dotnet build wsl-squad.slnx -c Release
dotnet test tests/wsl-squad.Tests -c Release
```

Unit tests run on any OS. The Windows integration suite (real WSL registry,
`wsl.exe`, Docker Desktop, Windows Terminal) runs on the Windows host:

```powershell
pwsh scripts/test-windows.ps1
```

The script isolates its configuration under `%TEMP%` and requires WSL 2 and
Docker Desktop. The WPF project (`net10.0-windows`) can be compiled on Linux with
`EnableWindowsTargeting`, but only runs on Windows.

## Quick start

```powershell
# Resolve/create the fleet root and write config.json + the editable catalog
wsl-squad init --root D:\WSL\wsl-squad

# List managed instances, orphans and (optionally) archives
wsl-squad list --archives
wsl-squad list --json

# Build a distro from the bundled Ubuntu recipe and install two packs
wsl-squad build --name dev --recipe ubuntu-24-04 --pack oh_my_shell,oh_my_py --register-docker

# Manage it
wsl-squad open dev
wsl-squad appearance dev --font "MesloLGS NF" --scheme "One Half Dark" --icon-text DV
wsl-squad packs status dev
wsl-squad packs apply dev --add oh_my_cloud --remove oh_my_peg
wsl-squad archive create dev --format tar.gz
wsl-squad duplicate dev dev-copy
wsl-squad compact dev
wsl-squad remove dev --archive-first --yes

# Archives
wsl-squad archives list
wsl-squad restore dev_archive --name dev-restored
wsl-squad archives delete old_archive --yes

# Docker Desktop integration
wsl-squad docker register dev --restart
wsl-squad docker unregister dev

# Environment checks and paths
wsl-squad doctor
wsl-squad root show
wsl-squad wslconfig

# Interactive menu (Build / Instances / Archives / Settings)
wsl-squad
```

Run `wsl-squad <command> --help` for the full option list. Exit codes are stable:
`0` success, `1` internal, `2` usage, `3` validation/not-found, `4` conflict or
confirmation required, `5` external tool, `6` permission, `7` disk space,
`8` partial/warning, `130` cancelled.

## State layout

Configuration lives in `%LOCALAPPDATA%\wsl-squad\`:

```
config.json              # { schemaVersion, fleetRoot, catalogRoot? }
catalog/                 # editable copy of packs, recipes, Dockerfiles, fonts, themes
logs/                    # application log
backups/                 # migration backups and their journals
```

The fleet root (default `%USERPROFILE%\WSL\wsl-squad`) contains one folder per
instance:

```
<root>\<instance>\
  .wsl-squad             # ownership marker (JSON)
  instance.json          # versioned metadata: user, Docker state, appearance
  ext4.vhdx
  terminal-icon.png

<root>\archives\<name>\
  archive.json           # format, exact file name, size, sha256, appearance
  <name>.tar.gz
```

Build images are tagged `wsl-squad/<slug>:<buildId>` and removed after import
unless `--keep-image` saves them under `catalog\images\`.

## Migrating from wsl-fleet

The previous PowerShell application (`wsl-fleet`) is adopted in place; VHDX files
and archive tars are not moved.

```powershell
wsl-squad migrate legacy --dry-run    # report instances, archives and blockers
wsl-squad migrate legacy              # adopt and write config.json last
wsl-squad migrate rollback <backup-id>
```

Every migration copies the legacy markers, `instance.json` files and existing
configuration into `%LOCALAPPDATA%\wsl-squad\backups\migration-*` and records a
journal. Legacy `.wsl-fleet` markers are renamed to `.wsl-fleet.migrated`;
`rollback` restores everything.

`wsl-squad root move <path> --yes` moves the whole fleet to a new folder by
archiving, removing and relocating each instance.

## Tooling packs

A pack is a folder under `catalog\packs\<pack>`; the folder name is its identity.
The CLI copies it into `~/.config/packs/<pack>` inside the distro and runs:

- `install_root.sh` as root (optional)
- `install.sh` as the instance user, behind a temporary passwordless sudo rule
  that is always removed afterwards
- on removal: `remove.sh` then `remove_root.sh`, then `cleanup_orphans.sh`

```
<pack>/
├── pack.conf              # catalogue metadata
├── install.sh             # user half of the installation
├── install_root.sh        # optional root half
├── remove.sh              # removal script
├── remove_root.sh         # optional root half of removal
├── make/*.mk              # gmake modules
├── zsh/*.zsh              # shell modules, sourced where they live
├── cheatsheets/*.sh       # cheat sheets
├── env.global.sample      # shared defaults
├── env.project.sample     # project defaults
├── config/                # oh_my_shell configuration
└── README.md
```

`pack.conf` uses GNU make assignments:

| Declaration | Meaning |
| :--- | :--- |
| `PACK_DESCRIPTION` | description shown in the catalog |
| `PACK_REQUIRES` | packs installed as dependencies |
| `PACK_VISIBLE` | `no` hides the pack from candidate lists |
| `PACK_FAMILY` | apt family, default `debian` (matched against `ID`/`ID_LIKE`) |
| `PACK_PACKAGES` | apt packages the pack installs |
| `PACK_OUTSIDE_APT` | tools installed outside apt |
| `PACK_IDENTIFYING_VARS` | variables refused in a shared `.env.global` |
| `PACK_WELCOME` | line printed after a build that selected the pack |

The embedded catalog is seeded additively from the application resources. A pack
file is only overwritten when it was not edited locally; user edits are preserved
and reported.

## Appearance

`wsl-squad appearance <instance>` prints the current font, colour scheme, tab
title and icon settings. Options such as `--font`, `--scheme`, `--tab-title`,
`--icon-text`, `--icon-top`, `--icon-bottom`, `--icon-color` and `--icon-image`
update only the fields provided. Settings are stored in `instance.json` and
mirrored into a Windows Terminal fragment when a matching WSL profile exists.
Icons are rendered as 256 px PNGs on Windows. Fonts must already be installed in
Windows; installation from the catalog is not automated yet.

## Repository layout

```
src/wsl-squad.Core/          Domain, services, repositories, Windows adapters
src/wsl-squad.Presentation/  DTOs, mappers, presenters (shared with the future WPF UI)
src/wsl-squad.UI.Cli/        CLI commands and interactive menu
src/wsl-squad.UI.Wpf/        WPF scaffold (themes, fonts, icons)
tests/wsl-squad.Tests/               unit tests (Linux-friendly)
tests/wsl-squad.Windows.Tests/       Windows integration suite
scripts/test-windows.ps1             host integration runner
```

Dependencies flow `Core ← Presentation ← UI`. Windows-specific code is isolated in
`Infrastructure/Windows` behind interfaces so unit tests run anywhere.

## License

MIT.
