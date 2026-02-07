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

Example config:

```conf
# /etc/dnsmasq.d/devbox.conf

# Answer your dev hostnames locally (wildcard)
# Replace with your dev box LAN IPv4 + stable LAN IPv6
address=/.devbox.home.arpa/192.168.1.10
address=/.devbox.home.arpa/2001:db8:abcd:1234::10

# Forward all other DNS queries upstream
no-resolv
server=192.168.1.1                 # UDR (often simplest)
# Optional public fallbacks:
server=1.1.1.1
server=8.8.8.8
server=2606:4700:4700::1111
server=2001:4860:4860::8888

# Optional
cache-size=10000
strict-order
```

Restart dnsmasq:

```bash
sudo systemctl restart dnsmasq
```

### Firewall
Allow DNS from your LAN to the dev box:
- UDP 53
- TCP 53 (rare but sometimes used)

### Avoid DNS loops
Keep the dev box’s **own** resolver pointing at the UDR/public DNS (not itself), so forwarding doesn’t loop.

---

## Step 2 — Tell the UDR to hand out the dev box as DNS

### IPv4 (DHCP Option 6)
In your UniFi Network LAN DHCP settings:
- Set **DHCP Name Server** to the dev box **IPv4**
- Prefer **no secondary DNS** (some clients will bypass the primary intermittently)

Also make the dev box IPv4 stable:
- DHCP reservation **or** static IP.

### IPv6 (SLAAC-only)
With SLAAC-only, DNS comes from **Router Advertisements (RDNSS)**, not DHCPv6.

In UniFi Network → your LAN → IPv6:
- Keep **SLAAC / RA enabled**
- Set **DHCPv6/RDNSS DNS Control = Manual**
- Add the dev box **stable IPv6** as DNS

> Key requirement: the dev box needs a **stable IPv6** address. Don’t use temporary/privacy addresses for the DNS server target.

---

## Step 3 — Reverse proxy on the dev box (Caddy)
Run a lightweight reverse proxy that listens on **80/443** and routes by hostname:

- `app1.devbox.home.arpa` → Vite port (e.g., 5173)
- `app2.devbox.home.arpa` → Vite port (e.g., 5174)
- `api.devbox.home.arpa` → .NET API port (e.g., 5000)

### Why this matters
Browsers hit `http(s)://hostname` with default ports 80/443. The proxy removes the need to expose `:5173` etc.

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
- [ ] dnsmasq answers `*.devbox.home.arpa` (A + AAAA) and forwards other queries
- [ ] UDR DHCPv4 hands out dev box IPv4 as DNS
- [ ] UDR RA/RDNSS hands out dev box stable IPv6 as DNS (SLAAC-only)
- [ ] Caddy (or other proxy) routes hostnames to Vite/.NET ports
- [ ] Vite binds to LAN + allowedHosts includes `.devbox.home.arpa`
- [ ] .NET binds to LAN interfaces
