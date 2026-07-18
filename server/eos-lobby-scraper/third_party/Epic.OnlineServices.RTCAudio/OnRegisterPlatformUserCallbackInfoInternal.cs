using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct OnRegisterPlatformUserCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnRegisterPlatformUserCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_PlatformUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnRegisterPlatformUserCallbackInfo other)
	{
		other = default(OnRegisterPlatformUserCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_PlatformUserId, out Utf8String to2);
		other.PlatformUserId = to2;
	}
}
