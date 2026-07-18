using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerSpawnOptionsInternal : ISettable<LogPlayerSpawnOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SpawnedPlayerHandle;

	private uint m_TeamId;

	private uint m_CharacterId;

	public void Set(ref LogPlayerSpawnOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_SpawnedPlayerHandle = other.SpawnedPlayerHandle;
		m_TeamId = other.TeamId;
		m_CharacterId = other.CharacterId;
	}

	public void Dispose()
	{
	}
}
