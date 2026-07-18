using System;

namespace Epic.OnlineServices.Achievements;

internal struct PlayerAchievementInternal : IGettable<PlayerAchievement>
{
	private int m_ApiVersion;

	private IntPtr m_AchievementId;

	private double m_Progress;

	private long m_UnlockTime;

	private int m_StatInfoCount;

	private IntPtr m_StatInfo;

	private IntPtr m_DisplayName;

	private IntPtr m_Description;

	private IntPtr m_IconURL;

	private IntPtr m_FlavorText;

	public void Get(out PlayerAchievement other)
	{
		other = default(PlayerAchievement);
		Helper.Get(m_AchievementId, out Utf8String to);
		other.AchievementId = to;
		other.Progress = m_Progress;
		Helper.Get(m_UnlockTime, out DateTimeOffset? to2);
		other.UnlockTime = to2;
		Helper.Get<PlayerStatInfoInternal, PlayerStatInfo>(m_StatInfo, out var to3, m_StatInfoCount, isArrayItemAllocated: false);
		other.StatInfo = to3;
		Helper.Get(m_DisplayName, out Utf8String to4);
		other.DisplayName = to4;
		Helper.Get(m_Description, out Utf8String to5);
		other.Description = to5;
		Helper.Get(m_IconURL, out Utf8String to6);
		other.IconURL = to6;
		Helper.Get(m_FlavorText, out Utf8String to7);
		other.FlavorText = to7;
	}
}
