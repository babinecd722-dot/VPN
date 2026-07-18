using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerDespawnOptionsInternal : ISettable<LogPlayerDespawnOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_DespawnedPlayerHandle;

	public void Set(ref LogPlayerDespawnOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_DespawnedPlayerHandle = other.DespawnedPlayerHandle;
	}

	public void Dispose()
	{
	}
}
