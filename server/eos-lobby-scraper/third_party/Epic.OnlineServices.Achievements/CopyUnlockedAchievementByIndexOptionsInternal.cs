using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyUnlockedAchievementByIndexOptionsInternal : ISettable<CopyUnlockedAchievementByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private uint m_AchievementIndex;

	public void Set(ref CopyUnlockedAchievementByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.UserId, ref m_UserId);
		m_AchievementIndex = other.AchievementIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserId);
	}
}
