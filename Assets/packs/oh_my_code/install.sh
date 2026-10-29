#!/usr/bin/env bash
# ==============================================================================
# THE OH_MY_CODE PACK - WHAT IT INSTALLS
# ==============================================================================
# The engine stages the pack under ~/.config/packs.staging, runs this script from
# there, then moves the folder into ~/.config/packs/oh_my_code.
#
# Everything this script PRINTS is plain ASCII: it travels through wsl.exe to
# the Windows console, which reads those bytes in its own code page - an emoji
# or an em dash arrives there as garbage. 

set -euo pipefail

# The messages: the shared library replaces this fallback when the image
# carries it; an instance built before it prints a plain sentence.
hint() { printf '%s\n' "$*"; }
success() { printf '%s\n' "$*"; }
if [ -r "$HOME/.config/zsh/lib/message.sh" ]; then
    # shellcheck source=/dev/null
    . "$HOME/.config/zsh/lib/message.sh" || true
fi

# The sample is read from beside this file: the two travel together wherever
# the folder lands.
here=$(cd "$(dirname "$0")" && pwd)

# From a shell that has not read the zsh configuration: ~/.local/bin is not on
# its PATH yet, and the check below needs it there.
export PATH="$HOME/.local/bin:$PATH"

launcher=$HOME/.local/bin/claude

if [ -x "$launcher" ]; then
    echo "Claude Code is already installed ($("$launcher" --version)) - nothing to do."
else
    foreign=$(command -v claude 2>/dev/null || true)
    if [ -n "$foreign" ]; then
        echo ""
        hint "Claude Code is already installed on Windows."
        hint "Install a local version too? [Y/n]"
        if read -r answer; then
            case "$answer" in
                [nN]*)
                    echo "Installation cancelled."
                    exit 2
                    ;;
            esac
        fi
    fi

    echo "Installing Claude Code from Anthropic's own script"
    echo "Please wait..."
    clean_path=$HOME/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
    rm -rf "$HOME/.claude/downloads" "${XDG_CACHE_HOME:-$HOME/.cache}/claude"

    heartbeat() {
        while sleep 5; do
            printf '...\n'
        done
    }
    heartbeat &
    heartbeat_pid=$!

    if PATH=$clean_path curl -fsSL https://claude.ai/install.sh | PATH=$clean_path bash 2>&1 |
        LC_ALL=C tr -d '\200-\377'; then
        installer_status=0
    else
        installer_status=$?
    fi

    kill "$heartbeat_pid" 2>/dev/null || true
    wait "$heartbeat_pid" 2>/dev/null || true

    if [ "$installer_status" -ne 0 ]; then
        echo "The Claude Code installation stopped (exit $installer_status)." >&2
        exit "$installer_status"
    fi
fi

# The dictionary of providers: seeded once, never written again. Mode 600 - the
# tokens go in there, and what the pack writes in your settings comes out of it.
profiles=$HOME/.config/claude/profiles.json
if [ -f "$profiles" ]; then
    echo "$profiles is already there - left as it is."
else
    # Checked before it is copied: a broken sample would be discovered at the
    # first profile switch.
    jq -e . "$here/profiles.sample" > /dev/null ||
        { echo "$profiles: the pack's own sample of providers does not parse - nothing was written." >&2; exit 1; }
    install -d -m 0700 "$(dirname "$profiles")"
    install -m 600 "$here/profiles.sample" "$profiles"
    echo "A dictionary of providers is in place: $profiles."
    echo "Fill it with your tokens."
fi

# The status line the instance's sessions draw: set once, and never over one
# that is already there. remove.sh takes back exactly the one this writes, and
# only while it is still the one.
settings=$HOME/.claude/settings.json
status_command="bash ~/.config/packs/oh_my_code/bin/statusline.sh"
if [ -f "$settings" ] && ! jq -e . "$settings" > /dev/null 2>&1; then
    echo "$settings does not parse - the status line was not set."
elif [ -f "$settings" ] && [ -n "$(jq -r '.statusLine.command // empty' "$settings" 2>/dev/null || true)" ]; then
    echo "$settings already has a status line - left as it is."
else
    install -d -m 0700 "$(dirname "$settings")"
    tmp=$(mktemp)
    if [ -f "$settings" ]; then
        ok=1
        jq --arg cmd "$status_command" '.statusLine = {"type": "command", "command": $cmd}' "$settings" > "$tmp" 2>/dev/null || ok=0
    else
        ok=1
        jq -n --arg cmd "$status_command" '{statusLine: {"type": "command", "command": $cmd}}' > "$tmp" || ok=0
    fi
    if [ "$ok" = "0" ]; then
        rm -f "$tmp"
        echo "$settings could not be written - the status line was not set."
    else
        chmod 600 "$tmp"
        mv "$tmp" "$settings"
    fi
fi

success "Claude Code is ready."
