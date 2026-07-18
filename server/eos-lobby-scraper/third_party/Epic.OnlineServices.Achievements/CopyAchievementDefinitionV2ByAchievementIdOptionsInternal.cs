using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyAchievementDefinitionV2ByAchievementIdOptionsInternal : ISettable<CopyAchievementDefinitionV2ByAchievementIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AchievementId;

	public void Set(ref CopyAchievementDefinitionV2ByAchievementIdOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.AchievementId, ref m_AchievementId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AchievementId);
	}
}
