using System;

namespace Epic.OnlineServices.Achievements;

internal struct OnUnlockAchievementsCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnUnlockAchievementsCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_UserId;

	private uint m_AchievementsCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnUnlockAchievementsCompleteCallbackInfo other)
	{
		other = default(OnUnlockAchievementsCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_UserId, out ProductUserId to2);
		other.UserId = to2;
		other.AchievementsCount = m_AchievementsCount;
	}
}
