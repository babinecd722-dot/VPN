using System;

namespace Epic.OnlineServices.Presence;

internal struct SetPresenceCallbackInfoInternal : ICallbackInfoInternal, IGettable<SetPresenceCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private Result m_RichPresenceResultCode;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SetPresenceCallbackInfo other)
	{
		other = default(SetPresenceCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		other.RichPresenceResultCode = m_RichPresenceResultCode;
	}
}
