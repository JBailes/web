#!/usr/bin/env bash
# setup.sh — install and configure the full web stack
#
# Run as root (or with sudo) from the web directory:
#   sudo bash setup.sh
#
# What this does:
#   1. Install system packages (nginx, node, dotnet, certbot)
#   2. Build the personal React SPA
#   3. Publish the Blazor WASM clients and AckWeb.Api
#   4. Install the nginx config
#   5. Install and enable the AckWeb.Api systemd service
#   6. Print SSL certificate commands (must be run manually after DNS is live)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_DIR="${SCRIPT_DIR}"
PUBLISH_DIR="${WEB_DIR}/publish"

# ── 1. System packages ────────────────────────────────────────────────────────
echo "[setup] Installing system packages..."
apt-get update -qq
apt-get install -y -qq nginx nodejs npm certbot python3-certbot-nginx

# Install .NET 9 SDK via the official install script (avoids Microsoft apt repo
# issues on Debian 13/Trixie where package signing is rejected).
if ! command -v dotnet &>/dev/null || ! dotnet --list-sdks | grep -q "^9\."; then
    echo "[setup] Installing .NET 9 SDK..."
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- \
        --channel 9.0 \
        --install-dir /usr/local/dotnet
    # Make dotnet available system-wide
    ln -sf /usr/local/dotnet/dotnet /usr/local/bin/dotnet
fi

# ── 2. Build personal React SPA ───────────────────────────────────────────────
echo "[setup] Building personal React SPA..."
cd "${WEB_DIR}/personal"
npm install --silent
npm run build

# ── 3. Publish Blazor WASM clients and API ────────────────────────────────────
echo "[setup] Publishing AckWeb.Client.Aha..."
dotnet publish "${WEB_DIR}/AckWeb.Client.Aha/AckWeb.Client.Aha.csproj" \
    --configuration Release \
    --output "${PUBLISH_DIR}/aha"

echo "[setup] Publishing AckWeb.Client.Wol..."
dotnet publish "${WEB_DIR}/AckWeb.Client.Wol/AckWeb.Client.Wol.csproj" \
    --configuration Release \
    --output "${PUBLISH_DIR}/wol"

echo "[setup] Publishing AckWeb.Api..."
dotnet publish "${WEB_DIR}/AckWeb.Api/AckWeb.Api.csproj" \
    --configuration Release \
    --output "${PUBLISH_DIR}/api"

# Blazor WASM publish puts the static files in wwwroot/ inside the output dir;
# nginx roots need to point directly at those static files.
# Create symlinks so nginx can use /root/web/publish/aha and /root/web/publish/wol.
if [ -d "${PUBLISH_DIR}/aha/wwwroot" ]; then
    rm -rf "${PUBLISH_DIR}/aha.static"
    ln -sf "${PUBLISH_DIR}/aha/wwwroot" "${PUBLISH_DIR}/aha.static"
    echo "[setup] Note: update nginx root to ${PUBLISH_DIR}/aha/wwwroot"
fi
if [ -d "${PUBLISH_DIR}/wol/wwwroot" ]; then
    rm -rf "${PUBLISH_DIR}/wol.static"
    ln -sf "${PUBLISH_DIR}/wol/wwwroot" "${PUBLISH_DIR}/wol.static"
    echo "[setup] Note: update nginx root to ${PUBLISH_DIR}/wol/wwwroot"
fi

# ── 4. nginx config ───────────────────────────────────────────────────────────
echo "[setup] Installing nginx config..."
cp "${WEB_DIR}/nginx/ackmud.conf" /etc/nginx/sites-available/ackmud.conf
ln -sf /etc/nginx/sites-available/ackmud.conf /etc/nginx/sites-enabled/ackmud.conf
# Remove the old acktng WSS-only config if it exists
rm -f /etc/nginx/conf.d/ackmud-wss.conf
nginx -t
systemctl reload nginx

# ── 5. AckWeb.Api systemd service ────────────────────────────────────────────
echo "[setup] Installing AckWeb.Api systemd service..."
sed "s|/root/web|${WEB_DIR}|g" "${WEB_DIR}/systemd/ackweb.service" \
    > /etc/systemd/system/ackweb.service
systemctl daemon-reload
systemctl enable ackweb.service
systemctl restart ackweb.service

# Disable the old Python web server if it's still installed
if systemctl is-enabled web-server.service &>/dev/null; then
    systemctl disable --now web-server.service || true
fi

# ── 6. SSL certificates (manual) ─────────────────────────────────────────────
echo ""
echo "=========================================================="
echo "  SSL certificates must be obtained manually once DNS"
echo "  is pointing to this server:"
echo ""
echo "  certbot certonly --webroot --webroot-path /var/www/certbot \\"
echo "    -d ackmud.com -d www.ackmud.com -d aha.ackmud.com"
echo ""
echo "  certbot certonly --webroot --webroot-path /var/www/certbot \\"
echo "    -d bailes.us -d www.bailes.us"
echo ""
echo "  Then: nginx -t && systemctl reload nginx"
echo "=========================================================="
echo ""
echo "[setup] Done."
