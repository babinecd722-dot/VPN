using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyPlayerAchievementByIndexOptionsInternal : ISettable<CopyPlayerAchievementByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private uint m_AchievementIndex;

	private IntPtr m_LocalUserId;

	public void Set(ref CopyPlayerAchievementByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_AchievementIndex = other.AchievementIndex;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
		Helper.Dispose(ref m_LocalUserId);
	}
}
