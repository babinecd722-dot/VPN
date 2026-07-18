using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyAchievementDefinitionByAchievementIdOptionsInternal : ISettable<CopyAchievementDefinitionByAchievementIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AchievementId;

	public void Set(ref CopyAchievementDefinitionByAchievementIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.AchievementId, ref m_AchievementId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AchievementId);
	}
}
