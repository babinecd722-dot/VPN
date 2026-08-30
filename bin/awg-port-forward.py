#!/usr/bin/env python3
"""AmneziaWG port-forwarding manager for single-peer OPEN NAT setups.

The tool intentionally stays outside the AmneziaWG protocol implementation:
it configures Linux nftables NAT/filtering around an existing awg0 tunnel.
"""

from __future__ import annotations

import argparse
import configparser
import ipaddress
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence


DEFAULT_CONFIG_PATH = "/etc/awg-port-forward.ini"
DEFAULT_TABLE_FAMILY = "inet"
DEFAULT_TABLE_NAME = "awg_port_forward"
GTA_TCP_PORTS = ("80", "443")
GTA_UDP_PORTS = ("6672", "61455-61458")


class ConfigError(ValueError):
    """Raised when the forwarding configuration is invalid."""


@dataclass(frozen=True)
class ForwardingConfig:
    public_interface: str
    vpn_interface: str
    vpn_subnet: ipaddress.IPv4Network
    target_ip: ipaddress.IPv4Address
    amneziawg_listen_port: int
    ssh_port: int
    preserve_ssh: bool
    disable_ufw: bool
    profile: str
    tcp_ports: tuple[str, ...]
    udp_ports: tuple[str, ...]
    forward_all_tcp: bool
    forward_all_udp: bool
    exclude_tcp_ports: tuple[str, ...]
    exclude_udp_ports: tuple[str, ...]
    table_family: str
    table_name: str
    enable_ip_forward: bool
    masquerade: bool

    @property
    def needs_tcp_rules(self) -> bool:
        return self.forward_all_tcp or bool(self.tcp_ports)

    @property
    def needs_udp_rules(self) -> bool:
        return self.forward_all_udp or bool(self.udp_ports)


def parse_bool(value: str, *, field: str) -> bool:
    normalized = value.strip().lower()
    if normalized in {"1", "yes", "true", "on"}:
        return True
    if normalized in {"0", "no", "false", "off"}:
        return False
    raise ConfigError(f"{field}: expected true/false, got {value!r}")


def get_option(
    parser: configparser.ConfigParser,
    section: str,
    option: str,
    default: str,
) -> str:
    if not parser.has_section(section):
        return default
    return parser.get(section, option, fallback=default).strip()


def parse_port(value: str, *, field: str) -> int:
    try:
        port = int(value, 10)
    except ValueError as exc:
        raise ConfigError(f"{field}: expected numeric port, got {value!r}") from exc
    if not 1 <= port <= 65535:
        raise ConfigError(f"{field}: port must be in 1..65535, got {port}")
    return port


def parse_port_tokens(value: str, *, field: str) -> tuple[str, ...]:
    tokens: list[str] = []
    seen: set[str] = set()
    for raw_token in value.replace("\n", ",").split(","):
        token = raw_token.strip()
        if not token:
            continue
        if "-" in token:
            left, sep, right = token.partition("-")
            if not sep or "-" in right:
                raise ConfigError(f"{field}: invalid port range {token!r}")
            start = parse_port(left.strip(), field=field)
            end = parse_port(right.strip(), field=field)
            if start > end:
                raise ConfigError(f"{field}: invalid descending range {token!r}")
            normalized = f"{start}-{end}"
        else:
            normalized = str(parse_port(token, field=field))
        if normalized not in seen:
            tokens.append(normalized)
            seen.add(normalized)
    return tuple(tokens)


def parse_ipv4_address(value: str, *, field: str) -> ipaddress.IPv4Address:
    try:
        address = ipaddress.ip_address(value)
    except ValueError as exc:
        raise ConfigError(f"{field}: invalid IPv4 address {value!r}") from exc
    if not isinstance(address, ipaddress.IPv4Address):
        raise ConfigError(f"{field}: IPv6 is not supported by this NAT profile")
    return address


def parse_ipv4_network(value: str, *, field: str) -> ipaddress.IPv4Network:
    try:
        network = ipaddress.ip_network(value, strict=False)
    except ValueError as exc:
        raise ConfigError(f"{field}: invalid IPv4 network {value!r}") from exc
    if not isinstance(network, ipaddress.IPv4Network):
        raise ConfigError(f"{field}: IPv6 is not supported by this NAT profile")
    return network


def nft_quote(value: str) -> str:
    escaped = value.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def nft_set(tokens: Sequence[str]) -> str:
    if not tokens:
        raise ValueError("nft set cannot be empty")
    return "{ " + ", ".join(tokens) + " }"


def protocol_match(protocol: str, *, forward_all: bool, ports: Sequence[str], excludes: Sequence[str]) -> str:
    if forward_all:
        if excludes:
            return f"{protocol} dport != {nft_set(excludes)}"
        return ""
    return f"{protocol} dport @{protocol}_forward_ports"


def load_config(path: str | Path) -> ForwardingConfig:
    parser = configparser.ConfigParser()
    read_files = parser.read(path)
    if not read_files:
        raise ConfigError(f"configuration file not found: {path}")

    public_interface = get_option(parser, "network", "public_interface", "eth0")
    vpn_interface = get_option(parser, "network", "vpn_interface", "awg0")
    vpn_subnet = parse_ipv4_network(get_option(parser, "network", "vpn_subnet", "10.66.66.0/24"), field="network.vpn_subnet")
    target_ip = parse_ipv4_address(get_option(parser, "network", "target_ip", "10.66.66.2"), field="network.target_ip")
    if target_ip not in vpn_subnet:
        raise ConfigError(f"network.target_ip {target_ip} is outside network.vpn_subnet {vpn_subnet}")

    amneziawg_listen_port = parse_port(
        get_option(parser, "network", "amneziawg_listen_port", "51820"),
        field="network.amneziawg_listen_port",
    )
    ssh_port = parse_port(get_option(parser, "network", "ssh_port", "22"), field="network.ssh_port")
    preserve_ssh = parse_bool(get_option(parser, "network", "preserve_ssh", "true"), field="network.preserve_ssh")
    disable_ufw = parse_bool(get_option(parser, "network", "disable_ufw", "false"), field="network.disable_ufw")

    profile = get_option(parser, "forwarding", "profile", "gta").lower()
    if profile not in {"gta", "all", "custom"}:
        raise ConfigError("forwarding.profile must be one of: gta, all, custom")

    if profile == "gta":
        default_tcp_ports = ",".join(GTA_TCP_PORTS)
        default_udp_ports = ",".join(GTA_UDP_PORTS)
        default_all_tcp = "false"
        default_all_udp = "false"
    elif profile == "all":
        default_tcp_ports = ""
        default_udp_ports = ""
        default_all_tcp = "true"
        default_all_udp = "true"
    else:
        default_tcp_ports = ""
        default_udp_ports = ""
        default_all_tcp = "false"
        default_all_udp = "false"

    tcp_ports = parse_port_tokens(get_option(parser, "forwarding", "tcp_ports", default_tcp_ports), field="forwarding.tcp_ports")
    udp_ports = parse_port_tokens(get_option(parser, "forwarding", "udp_ports", default_udp_ports), field="forwarding.udp_ports")
    forward_all_tcp = parse_bool(
        get_option(parser, "forwarding", "forward_all_tcp", default_all_tcp),
        field="forwarding.forward_all_tcp",
    )
    forward_all_udp = parse_bool(
        get_option(parser, "forwarding", "forward_all_udp", default_all_udp),
        field="forwarding.forward_all_udp",
    )

    default_exclude_tcp = [str(ssh_port)] if preserve_ssh else []
    default_exclude_udp = [str(amneziawg_listen_port)]
    exclude_tcp_ports = parse_port_tokens(
        get_option(parser, "forwarding", "exclude_tcp_ports", ",".join(default_exclude_tcp)),
        field="forwarding.exclude_tcp_ports",
    )
    exclude_udp_ports = parse_port_tokens(
        get_option(parser, "forwarding", "exclude_udp_ports", ",".join(default_exclude_udp)),
        field="forwarding.exclude_udp_ports",
    )

    if not (forward_all_tcp or forward_all_udp or tcp_ports or udp_ports):
        raise ConfigError("no forwarding rules configured")
    if amneziawg_listen_port not in {int(p) for p in expand_single_ports(exclude_udp_ports)} and forward_all_udp:
        raise ConfigError("forward_all_udp would capture the AmneziaWG listen port; add it to exclude_udp_ports")

    table_family = get_option(parser, "advanced", "table_family", DEFAULT_TABLE_FAMILY)
    table_name = get_option(parser, "advanced", "table_name", DEFAULT_TABLE_NAME)
    if table_family not in {"ip", "inet"}:
        raise ConfigError("advanced.table_family must be ip or inet")
    if not table_name.replace("_", "").replace("-", "").isalnum():
        raise ConfigError("advanced.table_name may contain only letters, numbers, '-' and '_'")

    enable_ip_forward = parse_bool(
        get_option(parser, "advanced", "enable_ip_forward", "true"),
        field="advanced.enable_ip_forward",
    )
    masquerade = parse_bool(get_option(parser, "advanced", "masquerade", "true"), field="advanced.masquerade")

    return ForwardingConfig(
        public_interface=public_interface,
        vpn_interface=vpn_interface,
        vpn_subnet=vpn_subnet,
        target_ip=target_ip,
        amneziawg_listen_port=amneziawg_listen_port,
        ssh_port=ssh_port,
        preserve_ssh=preserve_ssh,
        disable_ufw=disable_ufw,
        profile=profile,
        tcp_ports=tcp_ports,
        udp_ports=udp_ports,
        forward_all_tcp=forward_all_tcp,
        forward_all_udp=forward_all_udp,
        exclude_tcp_ports=exclude_tcp_ports,
        exclude_udp_ports=exclude_udp_ports,
        table_family=table_family,
        table_name=table_name,
        enable_ip_forward=enable_ip_forward,
        masquerade=masquerade,
    )


def expand_single_ports(tokens: Iterable[str]) -> set[str]:
    singles: set[str] = set()
    for token in tokens:
        if "-" not in token:
            singles.add(token)
    return singles


def generate_nft_rules(config: ForwardingConfig) -> str:
    pub = nft_quote(config.public_interface)
    vpn = nft_quote(config.vpn_interface)
    target = str(config.target_ip)
    vpn_subnet = str(config.vpn_subnet)
    lines: list[str] = [
        "# Generated by awg-port-forward. Manual edits will be overwritten.",
        f"table {config.table_family} {config.table_name} {{",
    ]

    if config.tcp_ports and not config.forward_all_tcp:
        lines.extend(
            [
                "  set tcp_forward_ports {",
                "    type inet_service",
                "    flags interval",
                f"    elements = {nft_set(config.tcp_ports)}",
                "  }",
                "",
            ]
        )
    if config.udp_ports and not config.forward_all_udp:
        lines.extend(
            [
                "  set udp_forward_ports {",
                "    type inet_service",
                "    flags interval",
                f"    elements = {nft_set(config.udp_ports)}",
                "  }",
                "",
            ]
        )

    lines.extend(
        [
            "  chain prerouting {",
            "    type nat hook prerouting priority dstnat; policy accept;",
        ]
    )
    if config.needs_tcp_rules:
        match = protocol_match("tcp", forward_all=config.forward_all_tcp, ports=config.tcp_ports, excludes=config.exclude_tcp_ports)
        lines.append(f"    iifname {pub} ip protocol tcp {match} dnat ip to {target}".rstrip())
    if config.needs_udp_rules:
        match = protocol_match("udp", forward_all=config.forward_all_udp, ports=config.udp_ports, excludes=config.exclude_udp_ports)
        lines.append(f"    iifname {pub} ip protocol udp {match} dnat ip to {target}".rstrip())
    lines.extend(["  }", ""])

    lines.extend(
        [
            "  chain forward {",
            "    type filter hook forward priority -100; policy accept;",
        ]
    )
    if config.needs_tcp_rules:
        match = protocol_match("tcp", forward_all=config.forward_all_tcp, ports=config.tcp_ports, excludes=config.exclude_tcp_ports)
        lines.append(f"    iifname {pub} oifname {vpn} ip daddr {target} ip protocol tcp {match} accept".rstrip())
    if config.needs_udp_rules:
        match = protocol_match("udp", forward_all=config.forward_all_udp, ports=config.udp_ports, excludes=config.exclude_udp_ports)
        lines.append(f"    iifname {pub} oifname {vpn} ip daddr {target} ip protocol udp {match} accept".rstrip())
    lines.append(f"    iifname {vpn} oifname {pub} ip saddr {vpn_subnet} accept")
    lines.extend(["  }", ""])

    if config.masquerade:
        lines.extend(
            [
                "  chain postrouting {",
                "    type nat hook postrouting priority srcnat; policy accept;",
                f"    oifname {pub} ip saddr {vpn_subnet} masquerade",
                "  }",
            ]
        )

    lines.append("}")
    return "\n".join(lines) + "\n"


def run_command(args: Sequence[str], *, input_text: str | None = None, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        list(args),
        input=input_text,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=check,
    )


def apply_sysctl(config: ForwardingConfig) -> None:
    if not config.enable_ip_forward:
        return
    run_command(["sysctl", "-w", "net.ipv4.ip_forward=1"])


def maybe_disable_ufw(config: ForwardingConfig) -> None:
    if not config.disable_ufw:
        return
    if shutil.which("ufw") is None:
        return
    run_command(["ufw", "--force", "disable"])


def nft_apply(rules: str, *, check_only: bool = False) -> None:
    args = ["nft"]
    if check_only:
        args.append("--check")
    args.extend(["-f", "-"])
    run_command(args, input_text=rules)


def nft_clean(config: ForwardingConfig) -> None:
    result = run_command(
        ["nft", "delete", "table", config.table_family, config.table_name],
        check=False,
    )
    if result.returncode == 0:
        return
    missing_table = (
        "No such file or directory" in result.stderr
        or "No such process" in result.stderr
        or "does not exist" in result.stderr
    )
    if not missing_table:
        sys.stderr.write(result.stderr)
        raise SystemExit(result.returncode)


def cmd_print(args: argparse.Namespace) -> int:
    config = load_config(args.config)
    sys.stdout.write(generate_nft_rules(config))
    return 0


def cmd_check(args: argparse.Namespace) -> int:
    config = load_config(args.config)
    rules = generate_nft_rules(config)
    if args.nft:
        nft_apply(rules, check_only=True)
    print("configuration OK")
    return 0


def cmd_apply(args: argparse.Namespace) -> int:
    config = load_config(args.config)
    rules = generate_nft_rules(config)
    apply_sysctl(config)
    maybe_disable_ufw(config)
    nft_clean(config)
    nft_apply(rules)
    print(f"applied {config.table_family} {config.table_name} for {config.target_ip}")
    return 0


def cmd_clean(args: argparse.Namespace) -> int:
    config = load_config(args.config)
    nft_clean(config)
    print(f"removed {config.table_family} {config.table_name}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Manage nftables port forwarding for AmneziaWG single-peer OPEN NAT.",
    )
    parser.add_argument(
        "-c",
        "--config",
        default=DEFAULT_CONFIG_PATH,
        help=f"path to INI config (default: {DEFAULT_CONFIG_PATH})",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    print_parser = subparsers.add_parser("print", help="print generated nftables rules")
    print_parser.set_defaults(func=cmd_print)

    check_parser = subparsers.add_parser("check", help="validate config")
    check_parser.add_argument("--nft", action="store_true", help="also run nft --check")
    check_parser.set_defaults(func=cmd_check)

    apply_parser = subparsers.add_parser("apply", help="apply sysctl/ufw/nftables changes")
    apply_parser.set_defaults(func=cmd_apply)

    clean_parser = subparsers.add_parser("clean", help="delete generated nftables table")
    clean_parser.set_defaults(func=cmd_clean)
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return args.func(args)
    except ConfigError as exc:
        sys.stderr.write(f"config error: {exc}\n")
        return 2
    except FileNotFoundError as exc:
        sys.stderr.write(f"missing command: {exc.filename}\n")
        return 127
    except subprocess.CalledProcessError as exc:
        if exc.stdout:
            sys.stdout.write(exc.stdout)
        if exc.stderr:
            sys.stderr.write(exc.stderr)
        return exc.returncode


if __name__ == "__main__":
    raise SystemExit(main())
