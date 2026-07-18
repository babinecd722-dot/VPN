using System;

namespace Epic.OnlineServices.Connect;

internal struct CreateDeviceIdCallbackInfoInternal : ICallbackInfoInternal, IGettable<CreateDeviceIdCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out CreateDeviceIdCallbackInfo other)
	{
		other = default(CreateDeviceIdCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
