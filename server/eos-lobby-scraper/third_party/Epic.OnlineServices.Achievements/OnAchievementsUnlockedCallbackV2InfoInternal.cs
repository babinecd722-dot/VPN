using System;

namespace Epic.OnlineServices.Achievements;

internal struct OnAchievementsUnlockedCallbackV2InfoInternal : ICallbackInfoInternal, IGettable<OnAchievementsUnlockedCallbackV2Info>
{
	private IntPtr m_ClientData;

	private IntPtr m_UserId;

	private IntPtr m_AchievementId;

	private long m_UnlockTime;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnAchievementsUnlockedCallbackV2Info other)
	{
		other = default(OnAchievementsUnlockedCallbackV2Info);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_UserId, out ProductUserId to2);
		other.UserId = to2;
		Helper.Get(m_AchievementId, out Utf8String to3);
		other.AchievementId = to3;
		Helper.Get(m_UnlockTime, out DateTimeOffset? to4);
		other.UnlockTime = to4;
	}
}
