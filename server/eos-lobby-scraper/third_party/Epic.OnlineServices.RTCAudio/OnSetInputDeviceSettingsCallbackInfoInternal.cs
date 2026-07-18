using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct OnSetInputDeviceSettingsCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnSetInputDeviceSettingsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_RealDeviceId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnSetInputDeviceSettingsCallbackInfo other)
	{
		other = default(OnSetInputDeviceSettingsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_RealDeviceId, out Utf8String to2);
		other.RealDeviceId = to2;
	}
}
