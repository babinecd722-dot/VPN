using System;

namespace Epic.OnlineServices.Achievements;

internal struct CopyPlayerAchievementByAchievementIdOptionsInternal : ISettable<CopyPlayerAchievementByAchievementIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private IntPtr m_AchievementId;

	private IntPtr m_LocalUserId;

	public void Set(ref CopyPlayerAchievementByAchievementIdOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set(other.AchievementId, ref m_AchievementId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
		Helper.Dispose(ref m_AchievementId);
		Helper.Dispose(ref m_LocalUserId);
	}
}
