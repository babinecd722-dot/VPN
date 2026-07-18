using System;

namespace Epic.OnlineServices.Lobby;

internal struct RejectInviteCallbackInfoInternal : ICallbackInfoInternal, IGettable<RejectInviteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_InviteId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out RejectInviteCallbackInfo other)
	{
		other = default(RejectInviteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_InviteId, out Utf8String to2);
		other.InviteId = to2;
	}
}
