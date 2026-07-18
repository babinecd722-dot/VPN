using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct CopyLeaderboardRecordByUserIdOptionsInternal : ISettable<CopyLeaderboardRecordByUserIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	public void Set(ref CopyLeaderboardRecordByUserIdOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.UserId, ref m_UserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserId);
	}
}
