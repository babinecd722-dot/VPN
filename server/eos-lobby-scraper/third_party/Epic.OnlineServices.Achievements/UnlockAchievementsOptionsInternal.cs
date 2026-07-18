using System;

namespace Epic.OnlineServices.Achievements;

internal struct UnlockAchievementsOptionsInternal : ISettable<UnlockAchievementsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private IntPtr m_AchievementIds;

	private uint m_AchievementsCount;

	public void Set(ref UnlockAchievementsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.UserId, ref m_UserId);
		Helper.Set(other.AchievementIds, ref m_AchievementIds, out m_AchievementsCount, isArrayItemAllocated: true);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserId);
		Helper.Dispose(ref m_AchievementIds);
	}
}
