using System;

namespace Epic.OnlineServices.Achievements;

internal struct QueryDefinitionsOptionsInternal : ISettable<QueryDefinitionsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_EpicUserId_DEPRECATED;

	private IntPtr m_HiddenAchievementIds_DEPRECATED;

	private uint m_HiddenAchievementsCount_DEPRECATED;

	public void Set(ref QueryDefinitionsOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.EpicUserId_DEPRECATED, ref m_EpicUserId_DEPRECATED);
		Helper.Set(other.HiddenAchievementIds_DEPRECATED, ref m_HiddenAchievementIds_DEPRECATED, out m_HiddenAchievementsCount_DEPRECATED, isArrayItemAllocated: true);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EpicUserId_DEPRECATED);
		Helper.Dispose(ref m_HiddenAchievementIds_DEPRECATED);
	}
}
