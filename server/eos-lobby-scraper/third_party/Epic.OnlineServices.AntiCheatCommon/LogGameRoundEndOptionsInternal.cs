using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogGameRoundEndOptionsInternal : ISettable<LogGameRoundEndOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_WinningTeamId;

	public void Set(ref LogGameRoundEndOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_WinningTeamId = other.WinningTeamId;
	}

	public void Dispose()
	{
	}
}
