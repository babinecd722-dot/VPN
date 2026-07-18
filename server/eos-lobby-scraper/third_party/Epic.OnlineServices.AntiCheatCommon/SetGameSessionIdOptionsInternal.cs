using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct SetGameSessionIdOptionsInternal : ISettable<SetGameSessionIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_GameSessionId;

	public void Set(ref SetGameSessionIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.GameSessionId, ref m_GameSessionId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_GameSessionId);
	}
}
