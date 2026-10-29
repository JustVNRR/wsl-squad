# ============================================================
# THE BROWSER
# ============================================================
# The pack's shell file, read where it lives (~/.config/packs/oh_my_web/zsh/web.zsh).
# `fox` opens the light privacy profile, `pfox` the strict one in a private
# window, with the check page first (README.md).

# X11, not Wayland: WSLg announces Wayland, and that path misbehaves under WSL.
# Ask for it back for one call: `MOZ_ENABLE_WAYLAND=1 fox`.
export MOZ_ENABLE_WAYLAND=0

# The pack's root, from this file's own path - %x is where a function here was
# defined (inside one, $0 is the function's name).
PACK=${${(%):-%x}:A:h:h}
FOX_SH=$PACK/bin/fox.sh

# WSLg can stay at scale:1 while Windows is at 150 or 200%: GTK then draws
# Firefox at 96 dpi and the interface is tiny. GDK_DPI_SCALE enlarges the fonts
# only - the window keeps its size. A value the user set wins.
_wslg_font_scale() {
    local log=/mnt/wslg/weston.log scale factor
    [ -r "$log" ] || return 0
    scale=$(grep 'Head mode change' "$log" 2>/dev/null | tail -n 1 | grep -o 'scale:[0-9][0-9]*' | cut -d: -f2 | head -n 1)
    [ "$scale" = 1 ] || return 0
    factor=$(grep -o 'desktopScaleFactor:[0-9][0-9]*' "$log" 2>/dev/null | sed 's/.*://' | grep -v '^0$' | tail -n 1)
    [ -n "$factor" ] && [ "$factor" -gt 100 ] || return 0
    awk -v factor="$factor" 'BEGIN { printf "%.2f\n", factor / 100 }'
}

# What both launchers end on. No session bus in the image, and Firefox's first
# window never opens without one: the first launch of a shell starts it.
_fox_launch() {
    local scale
    if [[ -z "$DBUS_SESSION_BUS_ADDRESS" ]] && command -v dbus-launch >/dev/null 2>&1; then
        # dbus refuses WSLg's runtime directory (world-writable); give this
        # call one of its own. The shell keeps WSLg's - Wayland and the sound
        # go through it.
        install -d -m 0700 "$HOME/.run"
        eval "$(XDG_RUNTIME_DIR="$HOME/.run" dbus-launch --sh-syntax)"
    fi
    scale=$(_wslg_font_scale)
    if [ -n "$scale" ] && [ -z "${GDK_DPI_SCALE:-}" ]; then
        GDK_DPI_SCALE="$scale" firefox "$@"
    else
        firefox "$@"
    fi
}

# fox opens on the light profile - and puts it back after a strict visit.
fox() {
    "$FOX_SH" light || return
    _fox_launch "$@"
}

# pfox opens the private window on the strict profile, check page first (the
# profile and the resolver travel in its fragment). The switch counts at the
# next start: with Firefox already running it would be silently wrong - refused.
pfox() {
    if pgrep -x firefox >/dev/null 2>&1; then
        print -u2 "Firefox is already running - close it, then run pfox again (the profile is read when Firefox starts)."
        return 1
    fi
    "$FOX_SH" strict || return
    local dns
    dns=$(awk '/^[[:space:]]*nameserver/{print $2; exit}' /etc/resolv.conf 2>/dev/null)
    _fox_launch --private-window "file://$PACK/privacy-check.html#strict;dns=$dns" "$@"
}
