using System;

namespace Epic.OnlineServices.Friends;

internal struct AcceptInviteCallbackInfoInternal : ICallbackInfoInternal, IGettable<AcceptInviteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out AcceptInviteCallbackInfo other)
	{
		other = default(AcceptInviteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetUserId, out EpicAccountId to3);
		other.TargetUserId = to3;
	}
}
