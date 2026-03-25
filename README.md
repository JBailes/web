# Web

This repo contains the full web stack for the ACKmud ecosystem: three sites served by nginx, a shared .NET API, and an installation script.

## Architecture

```
nginx
 ├── ackmud.com / www.ackmud.com  ──→  /root/web/publish/wol/wwwroot   (Blazor WASM)
 ├── aha.ackmud.com               ──→  /root/web/publish/aha/wwwroot   (Blazor WASM)
 └── bailes.us / www.bailes.us    ──→  /root/web/personal/dist         (React SPA)

 /api/*  (on ackmud.com and aha.ackmud.com)
         ──→  AckWeb.Api  (ASP.NET Core minimal API, localhost:5000)
```

### Sites

| Domain | Project | Stack | Description |
|---|---|---|---|
| `ackmud.com` | `AckWeb.Client.Wol` | C# Blazor WASM | AHA: World of Lore, the new game |
| `aha.ackmud.com` | `AckWeb.Client.Aha` | C# Blazor WASM | ACKmud Historical Archive |
| `bailes.us` | `personal/` | React + Vite + TypeScript | Personal landing page |

### API (`AckWeb.Api`)

ASP.NET Core minimal API running at `localhost:5000`. Provides dynamic data for both Blazor sites:

| Endpoint | Description |
|---|---|
| `GET /api/who` | Live player list, proxies the acktng game server's `/who` endpoint |
| `GET /api/gsgp` | Game stats JSON, proxies the acktng game server's `/gsgp` endpoint |
| `GET /api/reference/{type}` | List help/shelp/lore topic names (optional `?q=` filter) |
| `GET /api/reference/{type}/{topic}` | Return the text content of a specific topic file |

Reference data is read from the filesystem under `~/acktng/` (the game's `help/`, `shelp/`, and `lore/` directories).

### WebSocket proxies

nginx proxies three secure WebSocket (WSS) ports to the game servers:

| Port | World |
|---|---|
| `18890` | ACK!TNG |
| `8891` | ACK! 4.3.1 |
| `8892` | ACK! 4.2 |

## Directory layout

```
web/
├── AckWeb.sln                  .NET solution
├── AckWeb.Api/                 ASP.NET Core minimal API
├── AckWeb.Client.Aha/          Blazor WASM client for aha.ackmud.com
├── AckWeb.Client.Wol/          Blazor WASM client for ackmud.com
├── personal/                   React + Vite SPA for bailes.us
├── nginx/
│   └── ackmud.conf             nginx site config
├── systemd/
│   └── ackweb.service          systemd unit for AckWeb.Api
├── img/                        shared static images (terrain tiles, world map)
├── setup.sh                    installation script
└── README.md
```

## Installation

Run as root from this directory:

```sh
sudo bash setup.sh
```

This will:
1. Install system packages (`nginx`, `nodejs`, `npm`, `dotnet` SDK 9, `certbot`)
2. Build the personal React SPA (`npm run build`)
3. Publish the Blazor WASM clients and AckWeb.Api via `dotnet publish`
4. Install the nginx config and reload nginx
5. Install and enable the `ackweb.service` systemd unit

### SSL certificates

After DNS is pointed at the server, obtain certificates manually:

```sh
certbot certonly --webroot --webroot-path /var/www/certbot \
  -d ackmud.com -d www.ackmud.com -d aha.ackmud.com

certbot certonly --webroot --webroot-path /var/www/certbot \
  -d bailes.us -d www.bailes.us

nginx -t && systemctl reload nginx
```

## Development

### Build the .NET solution

```sh
export PATH="$PATH:/usr/local/dotnet"   # if dotnet is not already on PATH
dotnet build AckWeb.sln
```

### Run the API locally

```sh
cd AckWeb.Api
dotnet run
# Listens on http://localhost:5000 by default
```

### Build the personal SPA

```sh
cd personal
npm install
npm run build   # output → personal/dist/
```
