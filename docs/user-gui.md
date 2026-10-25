# WSL Squad GUI

The GUI manages the same fleet as the CLI: build, open, archive, restore,
remove, and configure WSL instances.

## Main window

The header shows the fleet root. The buttons are:

- **Add** - build a new instance.
- **Refresh** - reload the instance and archive lists.
- **Settings** - fleet and interface settings.
- **Theme** - switch between the dark and light variants.
- **Close** - quit the application.

Each live instance has action buttons on its row:

| Action | What it does |
| :--- | :--- |
| Shell | Open the instance. A pending recipe onboarding opens a root shell; afterwards the Windows Terminal profile is used when present. |
| Restart | Stop and start the instance. |
| Edit | Open the pack manager inside the instance. |
| Appearance | Change the tab icon, font, and colours. |
| Duplicate | Copy the instance under another name. |
| Archive | Write the instance to a named archive. |
| Compact | Reclaim space from the virtual disk. |
| Docker Desktop | Register or unregister the instance, depending on its state. |
| Remove | Unregister the distribution and delete its folder. |

Archive rows offer **Restore** and **Delete**. Actions that do not apply to a
row are shown disabled so the columns stay aligned.

A progress bar appears while an action is running. Errors are shown in a
dialog and written to `%LOCALAPPDATA%\wsl-squad\wsl-squad.log`.

## Build an instance

The Add dialog asks for:

- the instance name;
- the recipe; recipes can be created, duplicated, opened, or deleted;
- whether to build from its saved image when one exists; otherwise, or when
  unchecked, whether to save the Docker image locally.

**Create** starts the build. The recipe's Dockerfile owns the onboarding and
the default account.

## Instance actions

- **Shell** opens a terminal in the instance.
- **Restart** stops and starts it.
- **Edit** opens the pack manager inside the instance.
- **Appearance** edits the tab icon, font, and colours. The preview updates
  while you change the choices.
- **Duplicate** asks for the new name and warns that the source is stopped
  while it is copied.
- **Archive** writes a `tar.gz` archive under the fleet root. If the instance
  is running, it is stopped first; an archive with the same name is replaced
  after confirmation.
- **Compact** reclaims unused space. It can archive the instance first and
  restarts it if it was running.
- **Docker Desktop** registers or unregisters the instance. Docker Desktop is
  restarted when needed.
- **Remove** asks you to type the instance name. The operation is permanent:
  the distribution is unregistered and its folder is deleted.

## Appearance dialog

- **Icon text** - one to three letters or digits.
- **Icon colour** - the tile gradient and text colour.
- **Icon file** - a PNG, JPG, JPEG, ICO, or BMP image. An image takes
  precedence over the text icon.
- **Font** - the terminal font.
- **Colour scheme** - the Windows Terminal scheme.
- **Preview** - shows the selected font, background, text, and palette.

**Apply** writes the profile fragment and updates the instance.

## Settings

### Language

English and French are built in. At startup the app seeds the bundled
`Assets/languages/*.json` files into `%LOCALAPPDATA%\wsl-squad\languages\`. To
add another language without rebuilding, drop a JSON file in that folder:

```json
{
  "code": "de",
  "displayName": "Deutsch",
  "strings": {
    "SettingsTitle": "Einstellungen",
    "ActionCancel": "Abbrechen"
  }
}
```

The keys are listed in the seeded `languages\en.json`; missing keys fall back
to English.

Pick a language in Settings, or use the buttons to:

- open the selected language file in the default editor;
- upload a language JSON file;
- download the selected language;
- delete the selected language file.

A language change applies immediately and is saved in
`%LOCALAPPDATA%\wsl-squad\config.json`; it is restored at the next start.

### Root folder

The current fleet root is shown read-only. The pencil opens the move dialog;
the fleet and its archives are migrated to the new folder. Existing instances
are archived and unregistered first.

### WSL config

The path of `%USERPROFILE%\.wslconfig` is shown read-only. The pencil opens it
in the default editor, creating it when missing.

### Appearance and theme

Choose the window font and size, and the theme. Theme changes preview
immediately and are applied when the settings window closes. Themes can be
opened in the editor, uploaded, downloaded, or deleted.

### Packs catalog

Choose a catalog pack to open its folder, duplicate it under a new name, or
delete it. Deleting a pack lists the packs that depend on it first.

**Close** keeps the current appearance and closes the window.

## Files

- `%LOCALAPPDATA%\wsl-squad\config.json` - language and fleet root.
- `<fleet root>\archives\` - named archives; the fleet root is shown in the
  main window.
- `%LOCALAPPDATA%\wsl-squad\languages\*.json` - interface catalogs.
- `%LOCALAPPDATA%\wsl-squad\themes\*.xaml` - uploaded window themes.
- `%LOCALAPPDATA%\wsl-squad\wsl-squad.log` - application log.
