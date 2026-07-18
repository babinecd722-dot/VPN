using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct LeaderboardRecordInternal : IGettable<LeaderboardRecord>
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private uint m_Rank;

	private int m_Score;

	private IntPtr m_UserDisplayName;

	public void Get(out LeaderboardRecord other)
	{
		other = default(LeaderboardRecord);
		Helper.Get(m_UserId, out ProductUserId to);
		other.UserId = to;
		other.Rank = m_Rank;
		other.Score = m_Score;
		Helper.Get(m_UserDisplayName, out Utf8String to2);
		other.UserDisplayName = to2;
	}
}
