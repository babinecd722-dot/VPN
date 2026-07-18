using System;

namespace Epic.OnlineServices.Achievements;

internal struct UnlockedAchievementInternal : IGettable<UnlockedAchievement>
{
	private int m_ApiVersion;

	private IntPtr m_AchievementId;

	private long m_UnlockTime;

	public void Get(out UnlockedAchievement other)
	{
		other = default(UnlockedAchievement);
		Helper.Get(m_AchievementId, out Utf8String to);
		other.AchievementId = to;
		Helper.Get(m_UnlockTime, out DateTimeOffset? to2);
		other.UnlockTime = to2;
	}
}
