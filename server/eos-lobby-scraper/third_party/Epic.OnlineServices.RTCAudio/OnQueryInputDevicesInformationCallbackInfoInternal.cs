using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct OnQueryInputDevicesInformationCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnQueryInputDevicesInformationCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnQueryInputDevicesInformationCallbackInfo other)
	{
		other = default(OnQueryInputDevicesInformationCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
