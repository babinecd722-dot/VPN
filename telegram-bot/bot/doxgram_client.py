from __future__ import annotations

import time
from typing import Any

import httpx

from bot.config import Settings


class DoxgramError(Exception):
    def __init__(self, message: str, status: int | None = None) -> None:
        super().__init__(message)
        self.status = status


class DoxgramClient:
    def __init__(self, settings: Settings) -> None:
        self.settings = settings
        self._headers = {
            "UserAuthToken": settings.doxgram_token,
            "User-Agent": "Doxgram-Android",
        }
        self._vpn_headers = {
            "UserAuthToken": settings.doxgram_token,
            "User-Agent": "Doxgram-Android-VPN",
        }

    async def _request(
        self,
        method: str,
        url: str,
        *,
        headers: dict[str, str] | None = None,
        params: dict[str, Any] | None = None,
        json: dict[str, Any] | None = None,
        timeout: float = 60.0,
    ) -> Any:
        async with httpx.AsyncClient(timeout=timeout) as client:
            response = await client.request(
                method,
                url,
                headers=headers or self._headers,
                params=params,
                json=json,
            )
        if response.status_code >= 400:
            detail = response.text[:500]
            raise DoxgramError(detail, response.status_code)
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
            json=payload,
            timeout=30.0,
        )

    async def send_chat_message(self, conversation_id: int, content: str) -> dict[str, Any]:
        return await self._request(
            "POST",
            f"https://api.doxgram.com/gpt-service/conversations/{conversation_id}/messages",
            json={"content": content},
            timeout=120.0,
        )

    async def get_vpn_key(self, node_id: int | None = None) -> dict[str, Any]:
        params = {"tier": "PREMIUM"}
        if node_id is not None:
            params["nodeId"] = str(node_id)
        return await self._request(
            "GET",
            "https://api.doxgram.com/vpn-service/key/get",
            headers=self._vpn_headers,
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
