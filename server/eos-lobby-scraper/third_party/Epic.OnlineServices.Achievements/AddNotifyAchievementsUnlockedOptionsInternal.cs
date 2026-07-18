using System;

namespace Epic.OnlineServices.Achievements;

internal struct AddNotifyAchievementsUnlockedOptionsInternal : ISettable<AddNotifyAchievementsUnlockedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyAchievementsUnlockedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
