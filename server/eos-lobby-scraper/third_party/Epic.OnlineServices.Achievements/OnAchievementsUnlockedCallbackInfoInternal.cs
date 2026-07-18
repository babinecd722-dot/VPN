using System;

namespace Epic.OnlineServices.Achievements;

internal struct OnAchievementsUnlockedCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnAchievementsUnlockedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_UserId;

	private uint m_AchievementsCount;

	private IntPtr m_AchievementIds;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnAchievementsUnlockedCallbackInfo other)
	{
		other = default(OnAchievementsUnlockedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_UserId, out ProductUserId to2);
		other.UserId = to2;
		Helper.Get(m_AchievementIds, out var to3, m_AchievementsCount, isArrayItemAllocated: true);
		other.AchievementIds = to3;
	}
}
