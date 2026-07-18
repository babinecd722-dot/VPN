using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct OnQueryOutputDevicesInformationCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnQueryOutputDevicesInformationCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnQueryOutputDevicesInformationCallbackInfo other)
	{
		other = default(OnQueryOutputDevicesInformationCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
