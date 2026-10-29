#!/bin/bash
set -u

sudo_file=/etc/sudoers.d/90-wsl-squad-manage-packs
sudo_open=0

close_sudo() {
    if [ "${sudo_open:-0}" -eq 1 ]; then
        rm -f "${sudo_file:-}" 2>/dev/null || true
        sudo_open=0
    fi
}

cleanup_state() {
    close_sudo
    if [ -n "${staging_root:-}" ]; then
        rm -rf "$staging_root" 2>/dev/null || true
    fi
    rm -f /root/manage-packs.sh /root/manage-packs.catalog /root/manage-packs-cleanup.sh /root/manage-packs-bootstrap.sh 2>/dev/null || true
}

trap cleanup_state EXIT
trap 'exit 130' HUP INT TERM

C_RESET=''
C_RED=''
C_GREEN=''
C_YELLOW=''
C_CYAN=''
C_GREY=''
if [ -r /etc/skel/.config/zsh/lib/colours.sh ]; then
    . /etc/skel/.config/zsh/lib/colours.sh || true
fi

clear
echo "============================================================"
echo "            Wsl Squad - pack manager"
echo "============================================================"
echo ""

catalog=""
if [ -s /root/manage-packs.catalog ]; then
    catalog=$(cat /root/manage-packs.catalog)
fi
if [ ! -d "$catalog" ]; then
    echo "${C_RED}The pack catalog is not reachable: '$catalog'${C_RESET}"
    read -rp "Press Enter to close... " _
    exit 1
fi
if ! command -v whiptail >/dev/null 2>&1; then
    echo "${C_RED}whiptail is required to manage the packs.${C_RESET}"
    read -rp "Press Enter to close... " _
    exit 1
fi

distro_family() {
    local id=""
    local like=""
    if [ -r /etc/os-release ]; then
        id=$(sed -n 's/^ID=//p' /etc/os-release | head -n 1 | tr -d '"' | tr -d '\r')
        like=$(sed -n 's/^ID_LIKE=//p' /etc/os-release | head -n 1 | tr -d '"' | tr -d '\r')
    fi
    if [ "$id" = "debian" ] || printf '%s\n' "$like" | tr ' ' '\n' | grep -qx 'debian'; then
        echo debian
    else
        echo "$id"
    fi
}

family=$(distro_family)
if [ -z "$family" ]; then
    echo "${C_RED}The instance's family could not be determined; no pack is offered.${C_RESET}"
    read -rp "Press Enter to close... " _
    exit 1
fi

default_user=$(awk -F= '/^[[:space:]]*default[[:space:]]*=/{gsub(/^[[:space:]]+|[[:space:]]+$/, "", $2); print $2; exit}' /etc/wsl.conf 2>/dev/null | tr -d '\r')
[ -n "$default_user" ] || default_user=$(id -un)
if command -v getent >/dev/null 2>&1; then
    user_home=$(getent passwd "$default_user" | cut -d: -f6)
else
    user_home=$(awk -F: -v u="$default_user" '$1 == u { print $6; exit }' /etc/passwd | tr -d '\r')
fi
if [ -z "$user_home" ] || [ ! -d "$user_home" ]; then
    user_home=""
    for candidate in /home/* /root; do
        if [ -d "$candidate/.config/packs" ]; then
            user_home="$candidate"
            if [ "$candidate" = "/root" ]; then
                default_user=root
            else
                default_user=$(basename "$candidate")
            fi
            break
        fi
    done
fi
if [ -z "$user_home" ] || [ ! -d "$user_home" ]; then
    default_user=root
    user_home=/root
fi

packs_dir="$user_home/.config/packs"
staging_root="$user_home/.config/packs.staging"
mkdir -p "$packs_dir"
rm -rf "$staging_root"
mkdir -p "$staging_root"
chown "$default_user:$default_user" "$user_home/.config" "$packs_dir" "$staging_root" 2>/dev/null || true

open_sudo() {
    close_sudo
    if [ "$default_user" = "root" ]; then
        return 0
    fi
    if ! command -v visudo >/dev/null 2>&1; then
        return 1
    fi
    local tmp
    tmp=$(mktemp /tmp/wsl-squad-manage-sudo.XXXXXX) || return 1
    if ! printf '%s ALL=(ALL) NOPASSWD: ALL\n' "$default_user" > "$tmp"; then
        rm -f "$tmp"
        return 1
    fi
    chmod 0440 "$tmp" || { rm -f "$tmp"; return 1; }
    if ! visudo -cf "$tmp" >/dev/null 2>&1; then
        rm -f "$tmp"
        return 1
    fi
    if ! mv "$tmp" "$sudo_file"; then
        rm -f "$tmp"
        return 1
    fi
    sudo_open=1
    return 0
}

declare -A desc=()
declare -A family_of=()
declare -A requires=()
declare -A visible=()

for conf in "$catalog"/*/pack.conf; do
    [ -f "$conf" ] || continue
    name=${conf%/pack.conf}
    name=${name##*/}
    case "$name" in
        _*) continue ;;
    esac
    desc[$name]=$(sed -n 's/^PACK_DESCRIPTION[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1 | tr -d '\r')
    family_of[$name]=$(sed -n 's/^PACK_FAMILY[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1 | tr -d '\r')
    [ -n "${family_of[$name]}" ] || family_of[$name]=debian
    requires[$name]=$(sed -n 's/^PACK_REQUIRES[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1 | tr -d '\r')
    visible[$name]=$(sed -n 's/^PACK_VISIBLE[[:space:]]*:=[[:space:]]*//p' "$conf" | head -n 1 | tr -d '\r')
    [ -n "${visible[$name]}" ] || visible[$name]=yes
done

# The pack's env.global.sample is only visible once its folder is in
# ~/.config/packs; merging while it is still staging leaves its variables out
# of .env.global.
merge_global_env() {
    local makefile="$user_home/.config/zsh/gmake/Makefile"
    [ -f "$makefile" ] || return 1
    if [ "$default_user" = "root" ]; then
        HOME="$user_home" make -f "$makefile" env_global_enable
    else
        su - "$default_user" -c "make -f '$makefile' env_global_enable"
    fi
}

install_pack() {
    local name="$1"
    local source="$catalog/$name"
    local staging="$staging_root/$name"
    local target="$packs_dir/$name"

    if [ ! -d "$source" ]; then
        echo "${C_RED}Pack '$name' is missing from the catalog.${C_RESET}"
        return 1
    fi

    rm -rf "$staging"
    cp -a "$source" "$staging" || return 1
    chown -R "$default_user:$default_user" "$staging" 2>/dev/null || true

    if [ -f "$staging/install_root.sh" ]; then
        if ! ( cd "$staging" && HOME="$user_home" bash install_root.sh ); then
            echo "${C_RED}The root part of '$name' failed.${C_RESET}"
            rm -rf "$staging"
            return 1
        fi
    fi

    if [ -f "$staging/install.sh" ]; then
        if ! open_sudo; then
            echo "${C_YELLOW}The sudo door for $default_user could not be opened; the pack may ask for a password.${C_RESET}"
        fi
        su - "$default_user" -c "cd '$staging' && bash install.sh"
        local status=$?
        close_sudo
        if [ "$status" -eq 2 ]; then
            echo "${C_YELLOW}The installation of '$name' was cancelled.${C_RESET}"
            rm -rf "$staging"
            return 2
        fi
        if [ "$status" -ne 0 ]; then
            echo "${C_RED}The pack '$name' was not installed for $default_user.${C_RESET}"
            rm -rf "$staging"
            return 1
        fi
    fi

    chown -R "$default_user:$default_user" "$staging" 2>/dev/null || true
    rm -rf "$target"
    if ! mv "$staging" "$target"; then
        echo "${C_RED}The pack '$name' could not be moved into place.${C_RESET}"
        rm -rf "$staging"
        return 1
    fi

    if [ -f "$target/env.global.sample" ] && ! merge_global_env; then
        echo "${C_RED}The variables of '$name' were not merged into .env.global.${C_RESET}"
        echo "${C_YELLOW}The pack is installed; run 'gmake env_global_enable' as $default_user before using it or restarting.${C_RESET}"
        return 1
    fi

    local welcome
    welcome=$(sed -n 's/^PACK_WELCOME[[:space:]]*:=[[:space:]]*//p' "$target/pack.conf" | head -n 1 | tr -d '\r')
    if [ -n "$welcome" ]; then
        echo ""
        echo "${C_CYAN}$welcome${C_RESET}"
    fi
    return 0
}

remove_pack() {
    local name="$1"
    local target="$packs_dir/$name"

    if [ -f "$catalog/$name/remove.sh" ] && [ ! -f "$target/remove.sh" ]; then
        cp -f "$catalog/$name/remove.sh" "$target/remove.sh" 2>/dev/null || true
    fi
    if [ -f "$target/remove.sh" ]; then
        if ! ( cd "$target" && HOME="$user_home" bash remove.sh ); then
            echo "${C_RED}The removal of '$name' failed.${C_RESET}"
            return 1
        fi
    fi
    rm -rf "$target"
    return 0
}

while true; do
    declare -A installed=()
    for conf in "$packs_dir"/*/pack.conf; do
        [ -f "$conf" ] || continue
        name=${conf%/pack.conf}
        name=${name##*/}
        installed[$name]=1
        if [ -z "${desc[$name]:-}" ]; then
            desc[$name]="Installed pack (not in the catalog)"
            family_of[$name]=""
            requires[$name]=""
            visible[$name]=yes
        fi
    done

    listed=()
    while IFS= read -r name; do
        [ -n "$name" ] || continue
        if [ -n "${installed[$name]:-}" ]; then
            listed+=("$name")
            continue
        fi
        [ "${visible[$name]:-yes}" = "no" ] && continue
        if [ -n "$family" ] && [ "${family_of[$name]:-debian}" != "$family" ]; then
            continue
        fi
        listed+=("$name")
    done < <(printf '%s\n' "${!desc[@]}" | sort)

    if [ ${#listed[@]} -eq 0 ]; then
        whiptail --title "Wsl Squad - packs" --msgbox "No pack is available for this instance." 8 60
        break
    fi

    items=()
    for name in "${listed[@]}"; do
        state=OFF
        [ -n "${installed[$name]:-}" ] && state=ON
        label="$name"
        if [ -n "${desc[$name]:-}" ]; then
            label="$name - ${desc[$name]} [${family_of[$name]:-?}]"
        fi
        items+=("$name" "$label" "$state")
    done

    list_height=${#listed[@]}
    if [ "$list_height" -gt 12 ]; then
        list_height=12
    fi

    checklist_text="Select the packs to install or remove. Installed packs are checked.
- Space: select / deselect pack
- Enter: apply changes
- Esc: cancel
- Tab: navigate"

    if ! choice=$(NEWT_COLORS='root=white,black;window=white,black;border=cyan,black;shadow=black,black;title=black,cyan;button=black,cyan;actbutton=black,white;compactbutton=white,black;checkbox=white,black;actcheckbox=black,cyan;entry=black,white;label=white,black;listbox=white,black;actlistbox=white,black;sellistbox=black,cyan;actsellistbox=black,cyan;textbox=white,black;acttextbox=black,cyan;helpline=white,black;roottext=white,black' \
        whiptail --title "Wsl Squad - packs" \
        --separate-output \
        --checklist "$checklist_text" \
        23 82 "$list_height" "${items[@]}" 3>&1 1>&2 2>&3); then
        break
    fi

    declare -A desired=()
    while IFS= read -r name; do
        [ -n "$name" ] && desired[$name]=1
    done <<< "$choice"

    changed=1
    unknown=""
    while [ "$changed" -eq 1 ]; do
        changed=0
        for name in "${!desired[@]}"; do
            for req in ${requires[$name]:-}; do
                if [ -n "${desired[$req]:-}" ]; then
                    continue
                fi
                if [ -n "${installed[$req]:-}" ] || [ -n "${desc[$req]:-}" ]; then
                    desired[$req]=1
                    changed=1
                else
                    unknown="$req"
                fi
            done
        done
        [ -n "$unknown" ] && break
    done

    if [ -n "$unknown" ]; then
        whiptail --title "Wsl Squad - packs" --msgbox "The required pack '$unknown' is missing from the catalog." 8 66
        continue
    fi

    add=()
    remove=()
    for name in "${!desired[@]}"; do
        if [ -z "${installed[$name]:-}" ]; then
            add+=("$name")
        fi
    done
    for name in "${!installed[@]}"; do
        if [ -z "${desired[$name]:-}" ]; then
            remove+=("$name")
        fi
    done

    blocked_by=""
    blocked_pack=""
    for name in "${remove[@]}"; do
        for other in "${!installed[@]}"; do
            if [ -n "${desired[$other]:-}" ]; then
                continue
            fi
            case " ${requires[$other]:-} " in
                *" $name "*)
                    blocked_pack="$name"
                    blocked_by="$other"
                    break
                    ;;
            esac
        done
        [ -n "$blocked_by" ] && break
    done

    if [ -n "$blocked_by" ]; then
        whiptail --title "Wsl Squad - packs" --msgbox \
            "The pack '$blocked_pack' is required by '$blocked_by'. Keep it checked, or remove '$blocked_by' too." \
            9 68
        continue
    fi

    declare -A seen=()
    install_order=()
    visit_install() {
        local name="$1"
        [ -n "${seen[$name]:-}" ] && return
        seen[$name]=1
        for req in ${requires[$name]:-}; do
            if [ -n "${desired[$req]:-}" ] && [ -z "${installed[$req]:-}" ]; then
                visit_install "$req"
            fi
        done
        if [ -z "${installed[$name]:-}" ]; then
            install_order+=("$name")
        fi
    }
    for name in "${add[@]}"; do
        visit_install "$name"
    done

    declare -A removed=()
    remove_order=()
    while [ ${#removed[@]} -lt ${#remove[@]} ]; do
        progressed=0
        for name in "${remove[@]}"; do
            [ -n "${removed[$name]:-}" ] && continue
            blocked=0
            for other in "${remove[@]}"; do
                [ -n "${removed[$other]:-}" ] && continue
                case " ${requires[$other]:-} " in
                    *" $name "*) blocked=1 ;;
                esac
            done
            if [ "$blocked" -eq 0 ]; then
                remove_order+=("$name")
                removed[$name]=1
                progressed=1
                break
            fi
        done
        if [ "$progressed" -eq 0 ]; then
            for name in "${remove[@]}"; do
                if [ -z "${removed[$name]:-}" ]; then
                    remove_order+=("$name")
                    removed[$name]=1
                fi
            done
        fi
    done

    failed=0
    for name in "${install_order[@]}"; do
        echo ""
        echo "${C_CYAN}Installing '$name'...${C_RESET}"
        install_pack "$name"
        result=$?
        if [ "$result" -eq 2 ]; then
            continue
        fi
        if [ "$result" -ne 0 ]; then
            failed=1
            break
        fi
    done
    if [ "$failed" -eq 0 ]; then
        for name in "${remove_order[@]}"; do
            echo ""
            echo "${C_CYAN}Removing '$name'...${C_RESET}"
            if ! remove_pack "$name"; then
                failed=1
                break
            fi
        done
    fi
    if [ "$failed" -eq 0 ] && [ "${#remove_order[@]}" -gt 0 ] && [ -f /root/manage-packs-cleanup.sh ]; then
        echo ""
        echo "${C_CYAN}Taking back the dependencies the removed packs left behind...${C_RESET}"
        if ! ( cd /tmp && HOME="$user_home" bash /root/manage-packs-cleanup.sh ); then
            echo "${C_YELLOW}The dependency cleanup did not finish.${C_RESET}"
        fi
    fi

    if [ "$failed" -eq 0 ]; then
        echo ""
        echo "${C_GREEN}Packs updated.${C_RESET}"
    else
        echo ""
        echo "${C_RED}The pack changes stopped on an error.${C_RESET}"
    fi

    su - "$default_user"
    break
done

exit 0
