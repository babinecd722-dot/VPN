import importlib.util
from pathlib import Path
import sys
import unittest


MODULE_PATH = Path(__file__).resolve().parents[1] / "bin" / "awg-port-forward.py"
SPEC = importlib.util.spec_from_file_location("awg_port_forward", MODULE_PATH)
awg_port_forward = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = awg_port_forward
SPEC.loader.exec_module(awg_port_forward)


def write_config(tmp_path, body):
    path = tmp_path / "config.ini"
    path.write_text(body, encoding="utf-8")
    return path


class AwgPortForwardTests(unittest.TestCase):
    def test_gta_profile_forwards_tcp_and_udp_defaults(self):
        path = write_config(
            Path(self.enterContext(temp_dir())),
            """
[network]
public_interface = ens3
vpn_interface = awg0
vpn_subnet = 10.66.66.0/24
target_ip = 10.66.66.2
amneziawg_listen_port = 51820
ssh_port = 22

[forwarding]
profile = gta
""",
        )

        config = awg_port_forward.load_config(path)
        rules = awg_port_forward.generate_nft_rules(config)

        self.assertIn("elements = { 80, 443 }", rules)
        self.assertIn("elements = { 6672, 61455-61458 }", rules)
        self.assertIn('iifname "ens3" ip protocol tcp tcp dport @tcp_forward_ports dnat ip to 10.66.66.2', rules)
        self.assertIn('iifname "ens3" ip protocol udp udp dport @udp_forward_ports dnat ip to 10.66.66.2', rules)
        self.assertIn('oifname "ens3" ip saddr 10.66.66.0/24 masquerade', rules)


    def test_all_profile_excludes_ssh_and_amneziawg_ports(self):
        path = write_config(
            Path(self.enterContext(temp_dir())),
            """
[network]
public_interface = eth0
vpn_interface = awg0
vpn_subnet = 10.77.0.0/24
target_ip = 10.77.0.10
amneziawg_listen_port = 44444
ssh_port = 2222

[forwarding]
profile = all
""",
        )

        config = awg_port_forward.load_config(path)
        rules = awg_port_forward.generate_nft_rules(config)

        self.assertIn('tcp dport != { 2222 } dnat ip to 10.77.0.10', rules)
        self.assertIn('udp dport != { 44444 } dnat ip to 10.77.0.10', rules)
        self.assertNotIn("tcp_forward_ports", rules)
        self.assertNotIn("udp_forward_ports", rules)


    def test_rejects_all_udp_without_vpn_port_exclusion(self):
        path = write_config(
            Path(self.enterContext(temp_dir())),
            """
[network]
amneziawg_listen_port = 51820

[forwarding]
profile = all
exclude_udp_ports =
""",
        )

        with self.assertRaisesRegex(awg_port_forward.ConfigError, "AmneziaWG listen port"):
            awg_port_forward.load_config(path)


    def test_rejects_target_outside_vpn_subnet(self):
        path = write_config(
            Path(self.enterContext(temp_dir())),
            """
[network]
vpn_subnet = 10.66.66.0/24
target_ip = 10.66.67.2
""",
        )

        with self.assertRaisesRegex(awg_port_forward.ConfigError, "outside"):
            awg_port_forward.load_config(path)


class temp_dir:
    def __enter__(self):
        import tempfile

        self._tmp = tempfile.TemporaryDirectory()
        return self._tmp.__enter__()

    def __exit__(self, exc_type, exc_value, traceback):
        return self._tmp.__exit__(exc_type, exc_value, traceback)


if __name__ == "__main__":
    unittest.main()
