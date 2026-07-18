using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct QueryLeaderboardUserScoresOptionsInternal : ISettable<QueryLeaderboardUserScoresOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserIds;

	private uint m_UserIdsCount;

	private IntPtr m_StatInfo;

	private uint m_StatInfoCount;

	private long m_StartTime;

	private long m_EndTime;

	private IntPtr m_LocalUserId;

	public void Set(ref QueryLeaderboardUserScoresOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.UserIds, ref m_UserIds, out m_UserIdsCount, isArrayItemAllocated: false);
		Helper.Set<UserScoresQueryStatInfo, UserScoresQueryStatInfoInternal>(other.StatInfo, ref m_StatInfo, out m_StatInfoCount, isArrayItemAllocated: false);
		Helper.Set(other.StartTime, ref m_StartTime);
		Helper.Set(other.EndTime, ref m_EndTime);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserIds);
		Helper.Dispose(ref m_StatInfo);
		Helper.Dispose(ref m_LocalUserId);
	}
}
