using System;

namespace Epic.OnlineServices.Achievements;

internal struct AddNotifyAchievementsUnlockedV2OptionsInternal : ISettable<AddNotifyAchievementsUnlockedV2Options>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyAchievementsUnlockedV2Options other)
	{
		Dispose();
		m_ApiVersion = 2;
	}

	public void Dispose()
	{
	}
}
