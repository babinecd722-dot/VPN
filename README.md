# AmneziaWG OPEN NAT Port Forwarding

This repository contains a small VPS-side port-forwarding manager for
AmneziaWG/WireGuard single-peer gaming setups. It does not modify the
AmneziaWG protocol. Instead, it creates nftables DNAT/SNAT rules around an
existing `awg0` tunnel, which is the correct layer for GTA Online OPEN NAT.

## What it forwards

The default `gta` profile forwards:

- TCP `80,443`
- UDP `6672,61455-61458`

The `all` profile forwards every TCP/UDP port from the VPS public IPv4 to the
gaming PC, except the ports explicitly excluded in the config. The AmneziaWG
listen UDP port must stay excluded, otherwise the VPN handshake would be
forwarded into the tunnel and the server would become unreachable.

## Files

- `bin/awg-port-forward.py` - CLI manager.
- `config/awg-port-forward.ini` - example config.
- `systemd/awg-port-forward.service` - oneshot service.
- `tests/test_awg_port_forward.py` - unit tests for rule generation and config
  validation.

## Recommended VPS layout

```text
internet
  |
public VPS IPv4 on eth0
  |
AmneziaWG interface awg0: 10.66.66.1/24
  |
gaming PC over tunnel: 10.66.66.2
```

Make sure the gaming PC uses the VPN for GTA traffic and its local firewall
allows the same ports.

## Install on Ubuntu/Debian VPS

Install AmneziaWG normally first, then copy this manager:

```bash
sudo install -m 0755 bin/awg-port-forward.py /usr/local/sbin/awg-port-forward
sudo install -m 0644 config/awg-port-forward.ini /etc/awg-port-forward.ini
sudo install -m 0644 systemd/awg-port-forward.service /etc/systemd/system/awg-port-forward.service
sudo systemctl daemon-reload
```

Edit `/etc/awg-port-forward.ini`:

```ini
[network]
public_interface = eth0
vpn_interface = awg0
vpn_subnet = 10.66.66.0/24
target_ip = 10.66.66.2
amneziawg_listen_port = 51820
ssh_port = 22
disable_ufw = true

[forwarding]
profile = gta
```

Validate and apply:

```bash
sudo awg-port-forward check --nft
sudo awg-port-forward apply
sudo systemctl enable --now awg-port-forward.service
```

Print generated nftables rules without applying them:

```bash
awg-port-forward print
```

Remove generated rules:

```bash
sudo awg-port-forward clean
```

## "Open almost everything" mode

For a disposable single-purpose VPS you can forward nearly all TCP and UDP
ports:

```ini
[forwarding]
profile = all
forward_all_tcp = true
forward_all_udp = true
exclude_tcp_ports = 22
exclude_udp_ports = 51820
```

Keep at least:

- SSH excluded, unless you manage the VPS through another console.
- The AmneziaWG UDP listen port excluded.

If the VPS provider blocks some ports upstream, local nftables cannot override
that provider policy.

## Example AmneziaWG server peer

The server config should route the gaming PC address through its peer:

```ini
[Interface]
Address = 10.66.66.1/24
ListenPort = 51820
PrivateKey = SERVER_PRIVATE_KEY

[Peer]
PublicKey = CLIENT_PUBLIC_KEY
AllowedIPs = 10.66.66.2/32
```

The client should own `10.66.66.2/32`.

## Testing

Run:

```bash
python3 -m unittest discover -s tests
python3 bin/awg-port-forward.py -c config/awg-port-forward.ini print
```

