# WSL Squad CLI

The CLI manages WSL instances, archives, packs, appearance, and the fleet
configuration from a terminal.

## Launch

```powershell
wsl-squad
```

With no argument, the CLI opens its interactive menu. To run one action
directly:

```powershell
wsl-squad <command> [name]
```

Some commands need an instance or archive name. When it is omitted, the CLI
asks you to pick one from the list.

## Interactive menu

The main menu offers:

- **Build** - create a new instance.
- **Settings** - language, fleet folder, `.wslconfig`, and the packs catalog.
- **Instances** - pick an instance, then choose an action.
- **Archives** - pick an archive to restore or delete.
- **Exit** - leave the CLI.

Use the arrow keys and Enter. Escape (or `0` in numbered prompts) cancels the
current step. Ctrl+C exits with code 130. After an action, press Enter to
return to the menu.

### Instance actions

- **Shell** - open the instance. The recipe's onboarding runs in a root shell
  until it has completed; afterwards the Windows Terminal profile is used when
  it exists.
- **Restart** - stop and start the instance.
- **Manage packs** - open the pack manager inside the instance.
- **Theme** - change the tab icon, the terminal font, or the colour scheme.
- **Duplicate** - copy the instance under another name.
- **Archive** - write the instance to `<fleet root>\archives`.
- **Compact** - reclaim space from the virtual disk.
- **Docker Desktop** - register or unregister the instance.
- **Remove** - unregister the distribution and delete its folder.

Archive rows offer **Restore** and **Delete**.

### Settings

- **Language** - change the interface language. The choice is saved.
- **Migrate** - move the fleet and its archives to another folder. Running
  instances are archived and unregistered first.
- **WSL config** - open `%USERPROFILE%\.wslconfig`, creating it when missing.
- **Packs catalog** - open, duplicate, or delete a catalog pack.

## Direct commands

| Command | Name | Action |
| :--- | :--- | :--- |
| `build` | - | Build an instance interactively |
| `settings` | - | Open the settings submenu |
| `instances` | - | Browse instances and choose an action |
| `archives` | - | Browse archives and choose an action |
| `language` | - | Choose the interface language |
| `shell` | instance | Open a shell in the instance |
| `restart` | instance | Restart the instance |
| `manage_packs` | instance | Open the pack manager |
| `theme` | instance | Change the icon, font, or colours |
| `duplicate` | instance | Copy the instance |
| `archive` | instance | Archive the instance |
| `compact` | instance | Compact the instance |
| `register_docker` | instance | Register it with Docker Desktop |
| `unregister_docker` | instance | Remove it from Docker Desktop |
| `unregister` | instance | Remove the instance permanently |
| `restore` | archive | Restore an archive as a new instance |
| `delete_archive` | archive | Delete an archive |
| `migrate` | optional folder | Move the fleet |
| `wslconfig` | - | Open the Windows-wide WSL settings |

Examples:

```powershell
wsl-squad build
wsl-squad restart ubuntu
wsl-squad theme ubuntu
wsl-squad archive ubuntu
wsl-squad restore ubuntu_archive
wsl-squad migrate D:\WSL
```

Build asks for the name and the recipe, then whether to keep the Docker image.

Destructive commands ask for confirmation. Removing an instance and deleting
an archive cannot be undone.

## Exit codes

| Code | Meaning |
| :--- | :--- |
| `0` | Success, or the action was cancelled |
| `1` | Failure, or unknown command |
| `130` | Interrupted with Ctrl+C |

## Files

- `%LOCALAPPDATA%\wsl-squad\config.json` - selected language and fleet root.
- `%LOCALAPPDATA%\wsl-squad\languages\*.json` - interface catalogs.
- `<fleet root>\archives\` - named archives; the fleet root is stored in
  `config.json` and shown in the GUI header.
- `%USERPROFILE%\.wslconfig` - Windows-wide WSL settings.

Missing translation keys fall back to English. Language files can also be
edited, downloaded, uploaded, and deleted from the GUI settings.
