from __future__ import annotations

import json
from typing import Any

import httpx

from bot.config import Settings
from bot.errors import humanize_doxgram_error


class DoxgramError(Exception):
    def __init__(self, message: str, status: int | None = None, raw: str = "") -> None:
        super().__init__(message)
        self.status = status
        self.raw = raw


class DoxgramClient:
    def __init__(self, settings: Settings) -> None:
        self.settings = settings
        self._token = settings.doxgram_token
        self._refresh = settings.doxgram_refresh_token
        self._vpn_headers_base = {"User-Agent": "Doxgram-Android-VPN"}

    def _auth_headers(self) -> dict[str, str]:
        return {
            "UserAuthToken": self._token,
            "User-Agent": "Doxgram-Android",
        }

    def _vpn_headers(self) -> dict[str, str]:
        headers = dict(self._vpn_headers_base)
        headers["UserAuthToken"] = self._token
        return headers

    async def _refresh_token(self) -> bool:
        if not self._refresh:
            return False
        async with httpx.AsyncClient(timeout=20.0) as client:
            response = await client.get(
                "https://api.doxgram.com/identity-service/auth/refresh",
                params={"refreshToken": self._refresh},
                headers={"User-Agent": "Doxgram-Android"},
            )
        if response.status_code >= 400:
            return False
        try:
            data = response.json()
        except json.JSONDecodeError:
            return False
        token = data.get("token")
        refresh = data.get("refreshToken")
        if not token:
            return False
        self._token = token
        if refresh:
            self._refresh = refresh
        return True

    async def _request(
        self,
        method: str,
        url: str,
        *,
        headers: dict[str, str] | None = None,
        params: dict[str, Any] | None = None,
        json_body: dict[str, Any] | None = None,
        timeout: float = 60.0,
        retry_on_401: bool = True,
    ) -> Any:
        async with httpx.AsyncClient(timeout=timeout) as client:
            response = await client.request(
                method,
                url,
                headers=headers or self._auth_headers(),
                params=params,
                json=json_body,
            )

        if response.status_code == 401 and retry_on_401 and await self._refresh_token():
            async with httpx.AsyncClient(timeout=timeout) as client:
                response = await client.request(
                    method,
                    url,
                    headers=headers or self._auth_headers(),
                    params=params,
                    json=json_body,
                )

        if response.status_code >= 400:
            raw = response.text[:500]
            raise DoxgramError(humanize_doxgram_error(response.status_code, raw), response.status_code, raw)

        if not response.content:
            return None
        return response.json()

    async def osint_search(self, query: str) -> dict[str, Any]:
        return await self._request(
            "GET",
            "https://api.doxgram.com/osint-service/osint/search",
            params={"query": query},
            timeout=90.0,
        )

    async def create_conversation(self, title: str = "Telegram") -> dict[str, Any]:
        payload: dict[str, Any] = {}
        if title:
            payload["title"] = title
        return await self._request(
            "POST",
            "https://api.doxgram.com/gpt-service/conversations",
            json_body=payload,
            timeout=30.0,
        )

    async def send_chat_message(self, conversation_id: int, content: str) -> dict[str, Any]:
        return await self._request(
            "POST",
            f"https://api.doxgram.com/gpt-service/conversations/{conversation_id}/messages",
            json_body={"content": content},
            timeout=120.0,
        )

    async def get_vpn_key(self, node_id: int | None = None) -> dict[str, Any]:
        params = {"tier": "PREMIUM"}
        if node_id is not None:
            params["nodeId"] = str(node_id)
        return await self._request(
            "GET",
            "https://api.doxgram.com/vpn-service/key/get",
            headers=self._vpn_headers(),
            params=params,
            timeout=30.0,
        )

    async def get_free_vpn_key(self) -> dict[str, Any]:
        headers = {
            "User-Agent": "Doxgram-Android-VPN",
            "X-Doxgram-App-Install-Id": "telegram-bot-proxy",
            "X-Doxgram-App-Package": "com.doxgram.messenger",
            "X-Doxgram-App-Version": "12.4.31 (35251931)",
            "X-Doxgram-App-Signing-Sha256": "AD554999E99E1EB13D6E9578F07A26D993ABE9570187908096A6A6040017D1B9",
        }
        return await self._request(
            "GET",
            "https://api.doxgram.com/vpn-service/key/free",
            headers=headers,
            timeout=30.0,
            retry_on_401=False,
        )


VPN_SERVERS: list[dict[str, Any]] = [
    {
        "key": "pl",
        "title": "Польша · Варшава",
        "flag": "🇵🇱",
        "node_id": 10,
        "host": "185.241.208.155",
        "tier": "premium",
    },
    {
        "key": "fr",
        "title": "Франция · Бавилье",
        "flag": "🇫🇷",
        "node_id": 2,
        "host": "2.58.56.152",
        "tier": "premium",
    },
    {
        "key": "nl",
        "title": "Нидерланды · Амстердам",
        "flag": "🇳🇱",
        "node_id": None,
        "host": "195.63.143.178",
        "tier": "free",
    },
]


def server_by_key(key: str) -> dict[str, Any] | None:
    for server in VPN_SERVERS:
        if server["key"] == key:
            return server
    return None
