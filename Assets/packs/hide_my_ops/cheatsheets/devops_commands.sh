# ==========================================
# THE PROJECT TARGETS (the hide_my_ops pack)
# ==========================================
# What a project is built and pushed with, and the PRs it opens. The Docker and
# PR targets run from the root of a project under ~/projects; the gh_auth_*
# targets are machine-wide and run from anywhere.

# --- 1. DOCKER (LOCAL IMAGES) ---
gmake docker_build_local                     # Build Docker image for local development environment
gmake docker_run_local                       # Run Docker container locally

# --- 2. GITHUB CLI (AUTH AND PULL REQUESTS) ---
gmake gh_auth_https                          # Log in to GitHub for HTTPS remotes
gmake gh_auth_ssh                            # Log in to GitHub for SSH remotes
gmake gh_pr_create                           # Create a new Pull Request on GitHub
gmake gh_pr_toreview                         # Mark a Pull Request as ready for review
gmake gh_pr_wip                              # Convert a Pull Request back to draft status (WIP)
gmake gh_pr_ls                               # List all open Pull Requests in the repository
