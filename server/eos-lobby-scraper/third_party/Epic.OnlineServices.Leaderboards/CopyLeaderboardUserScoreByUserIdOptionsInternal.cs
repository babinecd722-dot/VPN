using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct CopyLeaderboardUserScoreByUserIdOptionsInternal : ISettable<CopyLeaderboardUserScoreByUserIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private IntPtr m_StatName;

	public void Set(ref CopyLeaderboardUserScoreByUserIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.UserId, ref m_UserId);
		Helper.Set(other.StatName, ref m_StatName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserId);
		Helper.Dispose(ref m_StatName);
	}
}
