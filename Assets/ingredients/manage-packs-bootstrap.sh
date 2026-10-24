#!/bin/sh
set -u

TERM="${TERM:-xterm-256color}"
LANG="${LANG:-C.UTF-8}"
export TERM LANG

fail() {
    printf '\n%s\n' "$1"
    printf 'Press Enter to close... '
    read -r _ || true
    exit 1
}

need_install=0
command -v bash >/dev/null 2>&1 || need_install=1
command -v whiptail >/dev/null 2>&1 || need_install=1

if [ "$need_install" -eq 1 ]; then
    [ "$(id -u)" -eq 0 ] || fail "bash and whiptail are required, and installing them needs root."

    if command -v apt-get >/dev/null 2>&1; then
        apt-get update || true
        DEBIAN_FRONTEND=noninteractive apt-get install -y bash whiptail
    elif command -v apk >/dev/null 2>&1; then
        apk add --no-cache bash newt
    elif command -v dnf >/dev/null 2>&1; then
        dnf install -y bash newt
    elif command -v yum >/dev/null 2>&1; then
        yum install -y bash newt
    elif command -v pacman >/dev/null 2>&1; then
        pacman -Sy --noconfirm bash libnewt
    elif command -v zypper >/dev/null 2>&1; then
        zypper --non-interactive install bash newt
    else
        fail "bash and whiptail are required, but no supported package manager was found."
    fi

    command -v bash >/dev/null 2>&1 || fail "bash could not be installed."
    command -v whiptail >/dev/null 2>&1 || fail "whiptail could not be installed."
fi

[ -f /root/manage-packs.sh ] || fail "The pack manager script is missing."

exec bash /root/manage-packs.sh
