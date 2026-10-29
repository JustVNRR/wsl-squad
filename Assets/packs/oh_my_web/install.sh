#!/usr/bin/env bash
# ==============================================================================
# THE OH_MY_WEB PACK - WHAT IT INSTALLS
# ==============================================================================
# The engine stages the pack under ~/.config/packs.staging, runs this script from
# there, then moves the folder into ~/.config/packs/oh_my_web.
#
# Firefox comes from Mozilla's own repository (Ubuntu's `firefox` is a snap
# stub), the tunnel is WireGuard over openresolv, and the servers land in
# ~/.config/vpn/servers.json - seeded here, or migrated from old
# /etc/wireguard profiles.
#
# The commands that need root are in install_root.sh, beside this file, and
# the engine runs it as root before this one starts.

set -euo pipefail

# The messages: the shared library replaces this fallback when the image
# carries it; an instance built before it prints a plain sentence.
success() { printf '%s\n' "$*"; }
if [ -r "$HOME/.config/zsh/lib/message.sh" ]; then
    # shellcheck source=/dev/null
    . "$HOME/.config/zsh/lib/message.sh" || true
fi

here=$(cd "$(dirname "$0")" && pwd)

# The root half ran before this script - the engine's call, as WSL's own
# root, ahead of the door. It also owns the Firefox audio default: the user
# half must never need sudo, because a password-protected sudo has no terminal
# here during the onboarding.

# The privacy link, and the light profile in place: one link in the browser's
# directory pointing at a file of the user's own - the launchers switch with no
# password. Best-effort, like the hook.
if ! bash "$here/bin/fox.sh" light; then
    echo "The privacy link was not set up - run it by hand:"
    echo "   bash ~/.config/packs/oh_my_web/bin/fox.sh light"
fi

# The servers. The JSON is the user's: left alone when it exists; migrated from
# leftover /etc/wireguard profiles; the sample otherwise.
servers=$HOME/.config/vpn/servers.json
if [ -f "$servers" ]; then
    echo "$servers is already there - left untouched."
else
    # The sample is checked first: it would be the user's file from the first minute.
    jq -e . "$here/vpn.servers.sample" > /dev/null ||
        {
            echo "The pack's own sample of servers does not parse - nothing was written to $servers." >&2
            exit 1
        }
    if ! bash "$here/bin/vpn-migrate.sh" "$servers"; then
        echo "Nothing was migrated - the pack's sample is used instead."
    fi
    if [ ! -f "$servers" ]; then
        install -d -m 0700 "$(dirname "$servers")"
        install -m 600 "$here/vpn.servers.sample" "$servers"
        echo "$servers is waiting for your keys - the sample shows where each"
        echo "   value goes (the WireGuard configuration from your provider)."
        echo "   Then: gmake vpn_edit_profiles, gmake vpn_up."
    fi
fi

success "Firefox is installed: 'fox' opens it on the light privacy settings, 'pfox' on the strict ones."
success "WireGuard is installed: gmake vpn_status   (servers first: gmake vpn_edit_profiles)"
if [ -L /etc/resolv.conf ] || grep -q 'generateResolvConf' /etc/wsl.conf 2>/dev/null; then
    echo "The DNS setting is read when the distro starts: restart it once"
    echo "   (wsl-squad restart, from Windows) before the tunnel manages /etc/resolv.conf."
fi
