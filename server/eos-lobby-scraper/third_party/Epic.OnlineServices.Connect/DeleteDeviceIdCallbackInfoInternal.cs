using System;

namespace Epic.OnlineServices.Connect;

internal struct DeleteDeviceIdCallbackInfoInternal : ICallbackInfoInternal, IGettable<DeleteDeviceIdCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out DeleteDeviceIdCallbackInfo other)
	{
		other = default(DeleteDeviceIdCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
