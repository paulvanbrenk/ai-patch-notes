# Lightweight local dev URLs (Windows + Android + Ubuntu dev box + UniFi UDR)

## Goal
Access multiple local dev apps from **both laptop + Android** using friendly hostnames like:

- `notes.devbox.home.arpa`
- `notes-api.devbox.home.arpa`
- `stats.devbox.home.arpa`

…instead of `http://IP:port`.

## High-level design
1. **Local DNS** (dnsmasq) answers `*.devbox.home.arpa` → the dev box IPv4.
2. **Reverse proxy** (Caddy) on the dev box listens on **port 80** and routes by hostname to the right dev server ports.
3. **Separate VLAN** (IPv4-only) ensures only dev devices use the devbox DNS.
4. Dev servers bind to localhost; Caddy proxies to them.

Once set up, you don't touch the router per project; you just start/stop dev servers.

---

## Use `home.arpa` (recommended)
Avoid `.local` for this use case because `.local` is handled specially by mDNS and can be inconsistent across OSes/devices.
`home.arpa` is reserved for home networks and works well with normal DNS.

---

## Step 1 — Run DNS on the dev box (dnsmasq)
Install dnsmasq on Ubuntu and configure it to:

- Answer **wildcard** `*.devbox.home.arpa` locally
- **Forward everything else** upstream (UDR/ISP/Google/etc.)

```bash
sudo apt install dnsmasq
```

Config:

```conf
# /etc/dnsmasq.d/devbox.conf

# Wildcard DNS for *.devbox.home.arpa → this dev box
address=/.devbox.home.arpa/192.168.1.76

# Only listen on the LAN interface (avoids conflict with systemd-resolved on 127.0.0.53)
listen-address=192.168.1.76
bind-interfaces

# Forward everything else to the UDR
no-resolv
server=192.168.1.1

cache-size=10000
```

### Disable the resolvconf helper

The default systemd unit runs a resolvconf `ExecStartPost` that fails harmlessly (tries to register with systemd-resolved via D-Bus). To suppress the error:

```bash
sudo mkdir -p /etc/systemd/system/dnsmasq.service.d
sudo tee /etc/systemd/system/dnsmasq.service.d/override.conf << 'EOF'
[Service]
ExecStartPost=
EOF
sudo systemctl daemon-reload
```

Start/restart dnsmasq:

```bash
sudo systemctl enable --now dnsmasq
```

### Firewall
Allow DNS from your LAN to the dev box:
- UDP 53
- TCP 53 (rare but sometimes used)

### Avoid DNS loops
Keep the dev box’s **own** resolver pointing at the UDR/public DNS (not itself), so forwarding doesn’t loop.

---

## Step 2 — Separate VLAN for dev clients (UniFi)

A dedicated VLAN keeps the devbox DNS scoped to just the **laptop and Android** — other devices on the main LAN are unaffected. IPv4-only avoids IPv6 DNS leaking around the setup.

### Network (UniFi)
- **Name**: `Dev` (or similar)
- **VLAN ID**: `10`
- **Gateway/Subnet**: `10.10.10.1/29` (6 usable IPs — plenty for laptop + phone)
- **DHCP DNS**: `192.168.1.76` (devbox on main LAN, reached via inter-VLAN routing)
- **IPv6**: Disabled

### WiFi (UniFi)
- **SSID**: `Dev` (or whatever you like)
- **Network**: the `Dev` network above
- **PMF**: Optional

### How it works
- Dev VLAN clients get `192.168.1.76` as their DNS via DHCP
- The UDR routes between VLANs by default, so `10.10.10.x` clients can reach `192.168.1.76:53` (dnsmasq) and `:80` (Caddy)
- The devbox stays on the main LAN only — no dual-homing needed
- No IPv6 on the dev VLAN means no RDNSS/SLAAC DNS leaking

---

## Step 3 — Reverse proxy on the dev box (Caddy)
Caddy is installed. It listens on **port 80** (HTTP only, which is fine for local dev) and routes by hostname.

### Caddyfile (`/etc/caddy/Caddyfile`)

```caddyfile
http://stats.devbox.home.arpa {
	reverse_proxy localhost:19999
}

http://notes.devbox.home.arpa {
	reverse_proxy localhost:1100
}

http://notes-api.devbox.home.arpa {
	reverse_proxy localhost:2101
}
```

Reload after changes:

```bash
sudo systemctl reload caddy
```

### Why this matters
Browsers hit `http://hostname` on port 80 by default. The proxy removes the need to expose `:5173` etc.

---

## Step 4 — Dev server ports

Caddy proxies to localhost, so dev servers don't need to bind to the LAN. Just pin each to a unique port:

| Subdomain | Port | App |
|---|---|---|
| `stats.devbox.home.arpa` | 19999 | Netdata |
| `notes.devbox.home.arpa` | 1100 | Vite (patchnotes-web) |
| `notes-api.devbox.home.arpa` | 2101 | .NET API |

### Vite
Port is set in `vite.config.ts` → `server.port`. `allowedHosts: true` is already configured.

### ASP.NET Core
Port is set in `Properties/launchSettings.json` → `applicationUrl`. Binds to `0.0.0.0` so Caddy can reach it.

### Frontend → API
`VITE_API_URL` in `.env.local` points to `http://notes-api.devbox.home.arpa`. CORS in `Program.cs` allows `.devbox.home.arpa` origins in Development mode only.

---

## Common gotchas
- **Android Private DNS**: if set to a specific provider, it may ignore LAN DNS. Set Private DNS to **Automatic/Off** while testing.
- **Secondary DNS**: clients may use it randomly; avoid if you want wildcard names to always resolve.
- **Stable IPv6**: required if you advertise the dev box as IPv6 DNS via RDNSS.

---

## Checklist
- [x] dnsmasq answers `*.devbox.home.arpa` (IPv4 only for now) and forwards other queries
- [x] Dev VLAN (`10.10.10.0/29`, VLAN 10) with DHCP DNS → devbox, IPv6 disabled
- [x] Laptop + Android on Dev WiFi SSID
- [x] Caddy routes `stats` → `:19999`, `notes` → `:1100`, `notes-api` → `:2101`
- [x] Vite pinned to port `1100`, `allowedHosts: true`
- [x] .NET API pinned to port `2101`, binds `0.0.0.0`
- [x] CORS allows `.devbox.home.arpa` in dev only
