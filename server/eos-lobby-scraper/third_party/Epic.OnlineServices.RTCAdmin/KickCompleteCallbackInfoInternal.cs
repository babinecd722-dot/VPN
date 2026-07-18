using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct KickCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<KickCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out KickCompleteCallbackInfo other)
	{
		other = default(KickCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
