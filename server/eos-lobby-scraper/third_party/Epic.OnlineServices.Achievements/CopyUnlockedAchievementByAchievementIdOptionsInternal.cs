using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyUnlockedAchievementByAchievementIdOptionsInternal : ISettable<CopyUnlockedAchievementByAchievementIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private IntPtr m_AchievementId;

	public void Set(ref CopyUnlockedAchievementByAchievementIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.UserId, ref m_UserId);
		Helper.Set(other.AchievementId, ref m_AchievementId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserId);
		Helper.Dispose(ref m_AchievementId);
	}
}
