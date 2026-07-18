using System;

namespace Epic.OnlineServices.Friends;

internal struct OnFriendsUpdateInfoInternal : ICallbackInfoInternal, IGettable<OnFriendsUpdateInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private FriendsStatus m_PreviousStatus;

	private FriendsStatus m_CurrentStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnFriendsUpdateInfo other)
	{
		other = default(OnFriendsUpdateInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetUserId, out EpicAccountId to3);
		other.TargetUserId = to3;
		other.PreviousStatus = m_PreviousStatus;
		other.CurrentStatus = m_CurrentStatus;
	}
}
