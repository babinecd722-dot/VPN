using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct OnUnregisterPlatformUserCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnUnregisterPlatformUserCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_PlatformUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnUnregisterPlatformUserCallbackInfo other)
	{
		other = default(OnUnregisterPlatformUserCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_PlatformUserId, out Utf8String to2);
		other.PlatformUserId = to2;
	}
}
