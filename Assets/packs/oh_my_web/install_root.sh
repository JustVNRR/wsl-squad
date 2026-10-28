#!/usr/bin/env bash
# ==============================================================================
# THE OH_MY_WEB PACK - WHAT IT INSTALLS AS ROOT
# ==============================================================================
# Run by the engine as WSL's own root, before install.sh - no password asked.
# Everything here belongs to root; what it installs is read from pack.conf,
# beside this file - the same line remove.sh reads.

set -euo pipefail

here=$(cd "$(dirname "$0")" && pwd)
declared=$(sed -n 's/^PACK_PACKAGES *:=[[:space:]]*//p' "$here/pack.conf")

if [ -z "$declared" ]; then
    echo "No PACK_PACKAGES found in $here/pack.conf" >&2
    exit 1
fi

# firefox is installed later (its repository comes first), and openresolv does
# not exist in Ubuntu 24.04 (Debian's package, pinned below): both would break
# the apt line.
packages=()
for package in $declared; do
    if [ "$package" != openresolv ] && [ "$package" != firefox ]; then
        packages+=("$package")
    fi
done
resolver_package=openresolv
resolver_url=http://ftp.debian.org/debian/pool/main/o/openresolv/openresolv_3.12.0-1_all.deb

# DEBIAN_FRONTEND: a package reconfigured on the way must not stop to ask.
export DEBIAN_FRONTEND=noninteractive

# The install, with a line at once and then one every 15 seconds while apt
# works in silence: a long wait must not look like a hang. One line per beat,
# because the run is streamed to the console line by line - an animated line
# would never show.
apt_install() {
    printf 'Installing... 0:00\n'
    apt-get -qq install -y --no-install-recommends "$@" > /dev/null &
    local apt_pid=$! i=0
    while kill -0 "$apt_pid" 2>/dev/null; do
        sleep 15
        i=$((i + 15))
        if kill -0 "$apt_pid" 2>/dev/null; then
            printf 'Installing... %d:%02d\n' "$((i / 60))" "$((i % 60))"
        fi
    done
    wait "$apt_pid" || return 1
}

# --- the tunnel -------------------------------------------------------------

# systemd-resolved ships a resolvconf of its own; Debian's openresolv refuses
# to install beside it. Keep the working resolver aside: removing the package
# can leave /etc/resolv.conf pointing at a file that no longer exists, and the
# rest of this install still needs DNS.
resolv_backup=/tmp/wsl-squad-resolv.conf
if [ -s /etc/resolv.conf ]; then
    cp -L /etc/resolv.conf "$resolv_backup"
fi

# 2. wireguard-tools, kmod (the module dies with 'wsl --shutdown' without
#    modprobe), iproute2 and iptables for wg-quick and the kill switch, plus
#    libpulse0 and the browser's decoder - never in PACK_PACKAGES, installed
#    beside it on purpose. The decoder is named after the machine's ffmpeg
#    (libavcodec60 on Ubuntu 24.04, libavcodec61 on Debian trixie), so the
#    name is read, not spelled; a machine carrying none gets the browser
#    without it.
apt-get -qq update
# || true: no name found is grep's exit 1, which pipefail would read as the
# script's own failure.
codec=$(apt-cache pkgnames libavcodec | grep -E '^libavcodec[0-9]+$' | sort -V | tail -1 || true)
codecs=()
if [ -n "$codec" ]; then
    codecs=("$codec")
else
    echo "No libavcodec package in this machine's apt - the browser goes without its decoder."
fi
apt_install "${packages[@]}" libpulse0 "${codecs[@]}"

# 3. The resolver, from Debian's archive (no such package in Ubuntu 24.04).
resolver_deb=/tmp/$resolver_package.deb
wget -q "$resolver_url" -O "$resolver_deb"
# dpkg runs after systemd-resolved is out of the way, at the end.

# 4. WSL must stop rewriting /etc/resolv.conf at every start, or openresolv's
#    work is undone by the next boot. vpn.sh writes the file (the base, from
#    BASE_DNS) before every mount.
if grep -q '^[[:space:]]*generateResolvConf' /etc/wsl.conf 2>/dev/null; then
    sed -i 's/^[[:space:]]*generateResolvConf.*/generateResolvConf = false/' /etc/wsl.conf
else
    printf '\n[network]\ngenerateResolvConf = false\n' >> /etc/wsl.conf
fi

# --- the browser ------------------------------------------------------------

# 5. Mozilla's signing key, fingerprint checked. grep reads the whole stream:
#    with --quiet, gpg would take the SIGPIPE and pipefail would read it as a
#    bad key.
install -d -m 0755 /etc/apt/keyrings
wget -q https://packages.mozilla.org/apt/repo-signing-key.gpg -O /etc/apt/keyrings/packages.mozilla.org.asc
gpg --show-keys --with-colons /etc/apt/keyrings/packages.mozilla.org.asc |
    grep '^fpr:::::::::35BAA0B33E9EB396F59CA838C0BA5CE6DC6315A3:' > /dev/null || {
        echo "Mozilla's signing key is not the one this pack knows - nothing was installed."
        rm -f /etc/apt/keyrings/packages.mozilla.org.asc
        exit 1
    }

# 6. Their repository, and two pins: theirs above Ubuntu's, and Ubuntu's snap
#    stub below everything.
echo 'deb [signed-by=/etc/apt/keyrings/packages.mozilla.org.asc] https://packages.mozilla.org/apt mozilla main' > /etc/apt/sources.list.d/mozilla.list
printf 'Package: *\nPin: origin packages.mozilla.org\nPin-Priority: 1000\n' > /etc/apt/preferences.d/mozilla
printf 'Package: firefox*\nPin: release o=Ubuntu*\nPin-Priority: -1\n' > /etc/apt/preferences.d/firefox-no-snap

apt-get -qq update
# --allow-downgrades: Ubuntu's stub carries an epoch (1:1snap1) and ranks
#    above Mozilla's package, so apt refuses the change without it.
apt_install --allow-downgrades firefox

# The audio sandbox keeps the browser away from WSLg's sound socket; this
# default turns it off (about:config wins). The user half used to write it
# through sudo, which is absent on a build that chose a sudo password: the
# pack failed silently at the first sudo prompt. Root owns the file.
pref_file=/usr/lib/firefox/defaults/pref/wslg-audio.js
if [ -d /usr/lib/firefox/defaults/pref ]; then
    cat > "$pref_file" <<'PREF'
// Set by the oh_my_web pack: the sound of a WSLg window goes through PulseAudio, and
// the audio sandbox keeps the browser away from the socket WSLg serves. This is
// a default - a value set in about:config wins over it.
pref("media.cubeb.sandbox", false);
PREF
    chmod 0644 "$pref_file"
    echo "The sound reaches WSLg (media.cubeb.sandbox off, as a default)."
else
    echo "No /usr/lib/firefox/defaults/pref: the sound was left alone."
fi

# --- the resolver last ------------------------------------------------------

# Firefox and every package above came from the network. Only now remove the
# resolver the image ships, then install the cached Debian package.
if dpkg -s systemd-resolved >/dev/null 2>&1; then
    apt-get -qq remove -y systemd-resolved > /dev/null
fi
dpkg -i "$resolver_deb" > /dev/null
rm -f "$resolver_deb"
if [ -s "$resolv_backup" ] && [ ! -s /etc/resolv.conf ]; then
    rm -f /etc/resolv.conf
    cp "$resolv_backup" /etc/resolv.conf
fi
rm -f "$resolv_backup"
