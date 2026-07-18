using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyAchievementDefinitionByIndexOptionsInternal : ISettable<CopyAchievementDefinitionByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_AchievementIndex;

	public void Set(ref CopyAchievementDefinitionByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_AchievementIndex = other.AchievementIndex;
	}

	public void Dispose()
	{
	}
}
