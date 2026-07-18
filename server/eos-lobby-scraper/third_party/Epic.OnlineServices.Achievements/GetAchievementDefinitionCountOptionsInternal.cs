using System;

namespace Epic.OnlineServices.Achievements;

internal struct GetAchievementDefinitionCountOptionsInternal : ISettable<GetAchievementDefinitionCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetAchievementDefinitionCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
