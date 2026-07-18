using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct LeaderboardUserScoreInternal : IGettable<LeaderboardUserScore>
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private int m_Score;

	public void Get(out LeaderboardUserScore other)
	{
		other = default(LeaderboardUserScore);
		Helper.Get(m_UserId, out ProductUserId to);
		other.UserId = to;
		other.Score = m_Score;
	}
}
