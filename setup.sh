#!/usr/bin/env bash
# setup.sh — Install and configure the full web project stack.
#
# Installs system dependencies, builds all front-end assets, installs the
# nginx site config, and registers/starts the web server systemd service.
#
# Run as root (or with sudo) from the repository root:
#   sudo ./setup.sh
#
# NOTE: SSL certificates are NOT handled here — obtain them manually:
#   certbot certonly --webroot --webroot-path /var/www/certbot \
#     -d ackmud.com -d www.ackmud.com -d aha.ackmud.com
#   certbot certonly --webroot --webroot-path /var/www/certbot \
#     -d bailes.us -d www.bailes.us

set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

log()  { echo "[setup] $*"; }
die()  { echo "[setup] ERROR: $*" >&2; exit 1; }

# ── 1. System dependencies ────────────────────────────────────────────────────

log "Installing system dependencies..."
apt-get update -qq
apt-get install -y -qq \
    python3 \
    nodejs \
    npm \
    nginx \
    certbot \
    python3-certbot-nginx

# ── 2. Personal site (React SPA) ─────────────────────────────────────────────

log "Building personal site (React SPA)..."
cd "$REPO_DIR/personal"
npm install --silent
npm run build
log "Personal site built -> personal/dist/"

cd "$REPO_DIR"

# ── 3. nginx ──────────────────────────────────────────────────────────────────

log "Installing nginx config..."
cp nginx/ackmud.conf /etc/nginx/sites-available/ackmud.conf
ln -sf /etc/nginx/sites-available/ackmud.conf /etc/nginx/sites-enabled/ackmud.conf

# Disable default nginx site if present to avoid port 80 conflicts
if [ -f /etc/nginx/sites-enabled/default ]; then
    rm -f /etc/nginx/sites-enabled/default
    log "Disabled default nginx site."
fi

mkdir -p /var/www/certbot

nginx -t || die "nginx config test failed — fix errors before reloading"
systemctl enable nginx
systemctl reload nginx
log "nginx configured and reloaded."

# ── 4. Web server (Python) — AHA + WOL ───────────────────────────────────────

log "Installing web server systemd service..."
sed "s|/home/user/web|${REPO_DIR}|g" systemd/web-server.service \
    | tee /etc/systemd/system/web-server.service > /dev/null
systemctl daemon-reload
systemctl enable web-server
systemctl restart web-server
log "web-server service started."

# ── 5. Certbot renewal hooks ──────────────────────────────────────────────────

log "Installing certbot renewal hooks..."
mkdir -p /etc/letsencrypt/renewal-hooks/post
cp scripts/certbot-post-renew.sh \
    /etc/letsencrypt/renewal-hooks/post/ackmud-reload-nginx.sh
chmod +x /etc/letsencrypt/renewal-hooks/post/ackmud-reload-nginx.sh
cp scripts/certbot-post-renew-acktng.sh \
    /etc/letsencrypt/renewal-hooks/post/acktng-restart.sh
chmod +x /etc/letsencrypt/renewal-hooks/post/acktng-restart.sh

# ── Done ──────────────────────────────────────────────────────────────────────

log ""
log "Setup complete. Next steps:"
log "  1. Obtain SSL certs if not already present:"
log "       certbot certonly --webroot --webroot-path /var/www/certbot \\"
log "         -d ackmud.com -d www.ackmud.com -d aha.ackmud.com"
log "       certbot certonly --webroot --webroot-path /var/www/certbot \\"
log "         -d bailes.us -d www.bailes.us"
log "  2. Reload nginx after certs are in place:"
log "       make nginx-reload"
