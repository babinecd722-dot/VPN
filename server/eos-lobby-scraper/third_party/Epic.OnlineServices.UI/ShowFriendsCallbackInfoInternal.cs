using System;

namespace Epic.OnlineServices.UI;

internal struct ShowFriendsCallbackInfoInternal : ICallbackInfoInternal, IGettable<ShowFriendsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out ShowFriendsCallbackInfo other)
	{
		other = default(ShowFriendsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
	}
}
