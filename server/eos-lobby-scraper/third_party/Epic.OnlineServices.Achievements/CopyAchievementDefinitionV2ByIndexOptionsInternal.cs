using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyAchievementDefinitionV2ByIndexOptionsInternal : ISettable<CopyAchievementDefinitionV2ByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_AchievementIndex;

	public void Set(ref CopyAchievementDefinitionV2ByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		m_AchievementIndex = other.AchievementIndex;
	}

	public void Dispose()
	{
	}
}
