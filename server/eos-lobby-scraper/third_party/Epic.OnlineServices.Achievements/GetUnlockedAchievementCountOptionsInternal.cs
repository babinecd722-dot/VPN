using System;

namespace Epic.OnlineServices.Achievements;

internal struct GetUnlockedAchievementCountOptionsInternal : ISettable<GetUnlockedAchievementCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	public void Set(ref GetUnlockedAchievementCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.UserId, ref m_UserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserId);
	}
}
