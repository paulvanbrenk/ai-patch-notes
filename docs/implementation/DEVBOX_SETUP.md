# Lightweight local dev URLs (Windows + Android + Ubuntu dev box + UniFi UDR7) — IPv4 + IPv6 (SLAAC)

## Goal
Access multiple local dev apps from **both laptop + Android** using friendly hostnames like:

- `app1.devbox.home.arpa`
- `app2.devbox.home.arpa`

…instead of `http://IP:port`.

## High-level design
1. **Local DNS** answers `*.devbox.home.arpa` → the dev box IPs (A + AAAA).
2. **Reverse proxy** on the dev box listens on **80/443** and routes by hostname to the right dev server ports (Vite / .NET).
3. Dev servers bind to the LAN (not localhost).

Once set up, you don’t touch the router per project; you just start/stop dev servers.

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

## Step 2 — Point specific clients at the dev box DNS

Only the **laptop and Android** device should use the dev box as their DNS server — not all LAN clients.

> **TODO:** Figure out the best mechanism for this (per-client DHCP reservations with custom DNS, a separate VLAN/network, manual static DNS on each device, etc.).

### IPv4
The selected clients need to resolve DNS via the dev box **IPv4**.
- Prefer **no secondary DNS** on those clients (some will bypass the primary intermittently)

Also make the dev box IPv4 stable:
- DHCP reservation **or** static IP.

### IPv6 (SLAAC-only)
With SLAAC-only, DNS normally comes from **Router Advertisements (RDNSS)**, which apply to all clients on the subnet. Limiting this to specific devices may require a separate approach.

> Key requirement: the dev box needs a **stable IPv6** address. Don't use temporary/privacy addresses for the DNS server target.

---

## Step 3 — Reverse proxy on the dev box (Caddy)
Caddy is installed. It listens on **port 80** (HTTP only, which is fine for local dev) and routes by hostname.

### Caddyfile (`/etc/caddy/Caddyfile`)

```caddyfile
http://stats.devbox.home.arpa {
	reverse_proxy 192.168.1.76:19999
}

# Add more sites as needed:
# http://app.devbox.home.arpa {
# 	reverse_proxy 192.168.1.76:5173
# }
# http://api.devbox.home.arpa {
# 	reverse_proxy 192.168.1.76:5000
# }
```

Reload after changes:

```bash
sudo systemctl reload caddy
```

### Why this matters
Browsers hit `http://hostname` on port 80 by default. The proxy removes the need to expose `:5173` etc.

---

## Step 4 — Make dev servers reachable on the LAN

### Vite (React + Vite)
Ensure Vite binds beyond localhost and permits your hostname:

- Bind: `--host 0.0.0.0` (or `server.host: true`)
- Allow hosts: include `.devbox.home.arpa`

Example `vite.config.*`:

```js
export default {
  server: {
    host: true,
    allowedHosts: ['.devbox.home.arpa'],
  },
}
```

### ASP.NET Core (Kestrel)
Bind Kestrel to LAN interfaces (IPv4 + IPv6), e.g. via `ASPNETCORE_URLS` / `--urls`, so your proxy can reach it.

---

## “No reconfig when I change projects” strategies

### Option A (simplest): fixed port convention
Pick a stable mapping once:
- app1 → 5173
- app2 → 5174
- api → 5000

Then Caddy config stays stable forever.

### Option B (more automatic): include/reload snippets
Have Caddy `import` per-app snippets from a directory and use a small script to:
- pick an available port
- write the snippet
- reload Caddy

---

## Common gotchas
- **Android Private DNS**: if set to a specific provider, it may ignore LAN DNS. Set Private DNS to **Automatic/Off** while testing.
- **Secondary DNS**: clients may use it randomly; avoid if you want wildcard names to always resolve.
- **Stable IPv6**: required if you advertise the dev box as IPv6 DNS via RDNSS.

---

## Checklist
- [x] dnsmasq answers `*.devbox.home.arpa` (IPv4 only for now) and forwards other queries
- [ ] Laptop + Android use dev box as DNS (mechanism TBD)
- [ ] IPv6 DNS for those clients points to dev box stable IPv6 (mechanism TBD)
- [x] Caddy routes `stats.devbox.home.arpa` → `:19999`
- [ ] Vite binds to LAN + allowedHosts includes `.devbox.home.arpa`
- [ ] .NET binds to LAN interfaces
