using System;

namespace Epic.OnlineServices.Auth;

internal struct LinkAccountCallbackInfoInternal : ICallbackInfoInternal, IGettable<LinkAccountCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_PinGrantInfo;

	private IntPtr m_SelectedAccountId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LinkAccountCallbackInfo other)
	{
		other = default(LinkAccountCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get<PinGrantInfoInternal, PinGrantInfo>(m_PinGrantInfo, out PinGrantInfo? to3);
		other.PinGrantInfo = to3;
		Helper.Get(m_SelectedAccountId, out EpicAccountId to4);
		other.SelectedAccountId = to4;
	}
}
