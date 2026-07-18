using System;

namespace Epic.OnlineServices.Friends;

internal struct OnBlockedUsersUpdateInfoInternal : ICallbackInfoInternal, IGettable<OnBlockedUsersUpdateInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private int m_Blocked;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnBlockedUsersUpdateInfo other)
	{
		other = default(OnBlockedUsersUpdateInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetUserId, out EpicAccountId to3);
		other.TargetUserId = to3;
		Helper.Get(m_Blocked, out bool to4);
		other.Blocked = to4;
	}
}
