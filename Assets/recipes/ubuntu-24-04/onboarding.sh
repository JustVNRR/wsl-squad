#!/bin/bash
set -e

C_RESET=''
C_RED=''
C_GREEN=''
C_YELLOW=''
C_CYAN=''
C_GREY=''
if [ -r /etc/skel/.config/zsh/lib/colours.sh ]; then
    # shellcheck source=/dev/null
    . /etc/skel/.config/zsh/lib/colours.sh || true
fi

clear
echo "============================================================"
echo "            Welcome to your Wsl Squad environment"
echo "============================================================"
echo ""

missing=""
for package in apt-utils adduser sudo tzdata whiptail locales; do
    dpkg-query -W -f '${Status}' "$package" 2>/dev/null | grep -q 'install ok installed' || missing="$missing $package"
done
if [ -n "$missing" ]; then
    export DEBIAN_FRONTEND=noninteractive
    echo "${C_CYAN}Installing the onboarding's own tools:$missing${C_RESET}"
    LC_ALL=C apt-get update -qq

    case " $missing " in
        *" apt-utils "*) LC_ALL=C apt-get install -y --no-install-recommends apt-utils ;;
    esac

    read -ra needed <<< "$missing"
    LC_ALL=C apt-get install -y --no-install-recommends "${needed[@]}"
    echo ""
fi

locale_gen=$(command -v locale-gen 2>/dev/null || true)
if [ -z "$locale_gen" ] && [ -x /usr/sbin/locale-gen ]; then
    locale_gen=/usr/sbin/locale-gen
fi
if [ -n "$locale_gen" ]; then
    LC_ALL=C "$locale_gen" en_US.UTF-8 >/dev/null 2>&1 || true
fi
if ! locale -a 2>/dev/null | grep -qi '^en_US\.utf'; then
    if command -v localedef >/dev/null 2>&1; then
        LC_ALL=C localedef -i en_US -f UTF-8 en_US.UTF-8 >/dev/null 2>&1 || true
    fi
fi

NEW_USER="${1:-}"

while true; do
    if [ -z "$NEW_USER" ]; then
        read -rp "${C_YELLOW}Enter your username: ${C_RESET}" NEW_USER
    fi
    if [[ ! "$NEW_USER" =~ ^[a-z][a-z0-9_-]*$ ]]; then
        echo "${C_YELLOW}Invalid username (lowercase letters, numbers, underscores, and dashes only, starting with a letter).${C_RESET}"
        NEW_USER=''
    elif id "$NEW_USER" >/dev/null 2>&1; then
        echo "${C_YELLOW}The account '$NEW_USER' already exists - pick another name.${C_RESET}"
        NEW_USER=''
    else
        break
    fi
done

echo ""
echo "${C_CYAN}Creating user account $NEW_USER...${C_RESET}"

SHELL_PATH=/usr/bin/zsh
if [ ! -x "$SHELL_PATH" ]; then
    SHELL_PATH=/bin/bash
fi
adduser --disabled-password --gecos "" --shell "$SHELL_PATH" "$NEW_USER"


sed -i '/onboarding\.sh/d' /root/.bashrc 2>/dev/null || true

echo ""

read -rp "${C_YELLOW}Run sudo without a password (passwordless)? [y/N] ${C_RESET}" PASSWORDLESS
echo ""

usermod -aG sudo "$NEW_USER"

PASSWORDLESS_OK=no
if [[ "$PASSWORDLESS" =~ ^[Yy]$ ]]; then

    SUDOERS_FILE="/etc/sudoers.d/010-$NEW_USER-nopasswd"
    echo "$NEW_USER ALL=(ALL) NOPASSWD: ALL" > "$SUDOERS_FILE"
    chmod 0440 "$SUDOERS_FILE"
    if visudo -cf "$SUDOERS_FILE" >/dev/null 2>&1; then
        PASSWORDLESS_OK=yes
        echo "${C_GREEN}sudo will not ask for a password.${C_RESET}"
    else
        rm -f "$SUDOERS_FILE"
        echo "${C_YELLOW}[WARNING] The sudo rule could not be written - falling back to a password.${C_RESET}"
    fi
fi

if [[ "$PASSWORDLESS_OK" == no ]]; then
    echo "${C_YELLOW}Please set a password for $NEW_USER:${C_RESET}"
    while ! passwd "$NEW_USER"; do
        echo ""
        echo "${C_RED}[ERROR] Password setup failed (mismatch or empty). Let's try again.${C_RESET}"
    done
fi

groupadd --force docker
usermod -aG docker "$NEW_USER"

echo ""
echo "${C_CYAN}Configuring timezone...${C_RESET}"

TZ_NAME=$(readlink /etc/localtime 2>/dev/null | sed 's|.*/zoneinfo/||')
if [ -n "$TZ_NAME" ] && [ -f "/usr/share/zoneinfo/$TZ_NAME" ]; then
    debconf-set-selections <<EOF
tzdata tzdata/Areas select ${TZ_NAME%%/*}
tzdata tzdata/Zones/${TZ_NAME%%/*} select ${TZ_NAME#*/}
EOF
    dpkg-reconfigure -f noninteractive tzdata
    echo "${C_GREEN}Timezone: $TZ_NAME (from Windows).${C_RESET}"
else
    dpkg-reconfigure -f dialog tzdata
fi
clear

cat << WSLCONF > /etc/wsl.conf
[user]
default=$NEW_USER

[automount]
enabled=true
mountFsTab=false

[interop]
enabled=true
appendWindowsPath=true
WSLCONF

pack_catalog=""
cmd_exe=""
if command -v cmd.exe >/dev/null 2>&1; then
    cmd_exe=cmd.exe
elif [ -x /mnt/c/Windows/System32/cmd.exe ]; then
    cmd_exe=/mnt/c/Windows/System32/cmd.exe
fi
if [ -n "$cmd_exe" ] && command -v wslpath >/dev/null 2>&1; then
    win_local=$("$cmd_exe" /c 'echo %LOCALAPPDATA%' 2>/dev/null | tr -d '\r')
    if [ -n "$win_local" ]; then
        pack_catalog="$(wslpath -u "$win_local")/wsl-squad/packs"
    fi
fi

distro_family() {
    local id=""
    local like=""

    if [ -r /etc/os-release ]; then
        id=$(sed -n 's/^ID=//p' /etc/os-release | head -n 1 | tr -d '"')
        like=$(sed -n 's/^ID_LIKE=//p' /etc/os-release | head -n 1 | tr -d '"')
    fi

    if [ "$id" = "debian" ] || printf '%s\n' "$like" | tr ' ' '\n' | grep -qx 'debian'; then
        echo debian
    else
        echo "$id"
    fi
}

pack_family=$(distro_family)

if [ -d "$pack_catalog" ]; then
    pack_names=()
    pack_desc=()

    for conf in "$pack_catalog"/*/pack.conf; do
        [ -f "$conf" ] || continue
        visible=$(sed -n 's/^PACK_VISIBLE[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1)
        if [ "$visible" = "no" ]; then
            continue
        fi
        family=$(sed -n 's/^PACK_FAMILY[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1)
        if [ -z "$family" ]; then
            family=debian
        fi
        if [ -n "$pack_family" ] && [ "$family" != "$pack_family" ]; then
            continue
        fi
        name=$(basename "$(dirname "$conf")")
        if [[ "$name" == _* ]]; then
            continue
        fi
        pack_names+=("$name")
        pack_desc+=("$(sed -n 's/^PACK_DESCRIPTION[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1)")
    done

    if [ "${#pack_names[@]}" -gt 0 ]; then
        pack_choice=""

        if command -v whiptail >/dev/null 2>&1; then
            pack_colors='root=white,black;window=white,black;border=cyan,black;shadow=black,black;title=black,cyan;button=black,cyan;actbutton=black,white;compactbutton=white,black;checkbox=white,black;actcheckbox=black,cyan;entry=black,white;label=white,black;listbox=white,black;actlistbox=white,black;sellistbox=black,cyan;actsellistbox=black,cyan;textbox=white,black;acttextbox=black,cyan;helpline=white,black;roottext=white,black'
            pack_items=()
            for index in "${!pack_names[@]}"; do
                state=OFF
                if [ "${pack_names[$index]}" = "oh_my_shell" ]; then
                    state=ON
                fi
                pack_items+=("$((index + 1))" "${pack_names[$index]} - ${pack_desc[$index]}" "$state")
            done

            list_height=${#pack_names[@]}
            if [ "$list_height" -gt 12 ]; then
                list_height=12
            fi

            if ! pack_choice=$(NEWT_COLORS="$pack_colors" whiptail \
                --title "Wsl Squad - packs" \
                --checklist "Select the packs to install for $NEW_USER. Space toggles, Enter confirms, Esc skips." \
                20 78 "$list_height" \
                "${pack_items[@]}" 3>&1 1>&2 2>&3); then
                pack_choice=""
            fi

            pack_choice=$(printf '%s\n' "$pack_choice" | tr -d '"')
        else
            echo ""
            echo "${C_CYAN}Available packs:${C_RESET}"
            for index in "${!pack_names[@]}"; do
                printf '  %2d) %-18s %s\n' "$((index + 1))" "${pack_names[$index]}" "${pack_desc[$index]}"
            done
            echo ""
            read -rp "${C_YELLOW}Packs to install (numbers separated by spaces, empty for none): ${C_RESET}" pack_choice
        fi

        selected=()
        for token in $pack_choice; do
            if [[ "$token" =~ ^[0-9]+$ ]] && [ "$token" -ge 1 ] && [ "$token" -le "${#pack_names[@]}" ]; then
                selected+=("${pack_names[$((token - 1))]}")
            fi
        done

        if [ "${#selected[@]}" -gt 0 ]; then
            declare -A chosen=()
            ordered=()

            add_pack() {
                local name="$1"
                if [ -n "${chosen[$name]:-}" ]; then
                    return
                fi
                chosen[$name]=1

                local conf="$pack_catalog/$name/pack.conf"
                if [ -f "$conf" ]; then
                    local deps
                    deps=$(sed -n 's/^PACK_REQUIRES[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1)
                    for dep in $deps; do
                        add_pack "$dep"
                    done
                fi

                ordered+=("$name")
            }

            for name in "${selected[@]}"; do
                add_pack "$name"
            done

            user_home=$(getent passwd "$NEW_USER" | cut -d: -f6)
            user_packs="$user_home/.config/packs"
            user_staging="$user_home/.config/packs.staging"
            mkdir -p "$user_packs"
            rm -rf "$user_staging"
            mkdir -p "$user_staging"
            chown "$NEW_USER:$NEW_USER" "$user_home" "$user_home/.config" "$user_packs" "$user_staging"

            for name in "${ordered[@]}"; do
                source_pack="$pack_catalog/$name"
                staging_pack="$user_staging/$name"
                target_pack="$user_packs/$name"

                if [ ! -d "$source_pack" ]; then
                    echo "${C_YELLOW}[WARNING] Pack '$name' is missing from the catalog.${C_RESET}"
                    continue
                fi

                rm -rf "$staging_pack"
                cp -a "$source_pack" "$staging_pack"
                chown -R "$NEW_USER:$NEW_USER" "$staging_pack"

                if [ -f "$staging_pack/install_root.sh" ]; then
                    if ! ( cd "$staging_pack" && bash install_root.sh ); then
                        echo "${C_YELLOW}[WARNING] The root part of '$name' failed.${C_RESET}"
                        rm -rf "$staging_pack"
                        continue
                    fi
                fi

                if [ -f "$staging_pack/install.sh" ]; then
                    if ! su - "$NEW_USER" -c "cd '$staging_pack' && bash install.sh"; then
                        echo "${C_YELLOW}[WARNING] The pack '$name' was not installed for $NEW_USER.${C_RESET}"
                        rm -rf "$staging_pack"
                        continue
                    fi
                fi

                chown -R "$NEW_USER:$NEW_USER" "$staging_pack"
                rm -rf "$target_pack"

                if ! mv "$staging_pack" "$target_pack"; then
                    echo "${C_YELLOW}[WARNING] The pack '$name' could not be moved into place.${C_RESET}"
                    rm -rf "$staging_pack"
                    continue
                fi

                welcome=$(sed -n 's/^PACK_WELCOME[[:space:]]*:=[[:space:]]*//p' "$target_pack/pack.conf" | head -n 1)
                if [ -n "$welcome" ]; then
                    echo ""
                    echo "${C_CYAN}$welcome${C_RESET}"
                fi
            done
            rmdir "$user_staging" 2>/dev/null || true
        fi
    fi
else
    echo "${C_GREY}Pack catalog not reachable; add packs later with Manage Packs.${C_RESET}"
fi

rm -f /root/onboarding.sh /root/onboarding-run.sh /root/onboarding.user

restart_distro() {
    local wsl_exe=""
    if command -v wsl.exe >/dev/null 2>&1; then
        wsl_exe=wsl.exe
    elif [ -x /mnt/c/Windows/System32/wsl.exe ]; then
        wsl_exe=/mnt/c/Windows/System32/wsl.exe
    fi
    if [ -n "$wsl_exe" ] && [ -n "${WSL_DISTRO_NAME:-}" ]; then
        "$wsl_exe" --terminate "$WSL_DISTRO_NAME" >/dev/null 2>&1 || true
    fi
}

read -rp "${C_YELLOW}Open a shell as $NEW_USER now? [Y/n] ${C_RESET}" SWITCH_USER
if [[ ! "$SWITCH_USER" =~ ^[Nn]$ ]]; then
    su - "$NEW_USER" || true
fi

restart_distro
exit 0
