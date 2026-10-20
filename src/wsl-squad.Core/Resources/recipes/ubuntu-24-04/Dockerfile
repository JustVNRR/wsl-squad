FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive

RUN userdel -r ubuntu 2>/dev/null || true

RUN rm -f /etc/dpkg/dpkg.cfg.d/*

RUN apt-get update \
    && apt-get install -y --no-install-recommends apt-utils \
    && rm -rf /var/lib/apt/lists/*

RUN touch /etc/skel/.zshrc \
    && mkdir -p /etc/skel/.ssh \
    && mkdir -p /etc/skel/projects \
    && chmod 700 /etc/skel/.ssh \
    && chown -R root:root /etc/skel

COPY onboarding.sh /root/onboarding.sh
RUN chmod 755 /root/onboarding.sh \
    && echo 'exec /root/onboarding.sh' >> /root/.bashrc \
    && printf '%s\n' \
        'export ZDOTDIR="/root/.config/zsh"' \
        'skip_global_compinit=1' \
        '[[ -o interactive ]] && [ -x /root/onboarding.sh ] && exec /root/onboarding.sh' \
        > /root/.zshenv

CMD ["/bin/bash"]
