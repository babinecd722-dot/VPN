using System;

namespace Epic.OnlineServices.P2P;

internal struct OnQueryNATTypeCompleteInfoInternal : ICallbackInfoInternal, IGettable<OnQueryNATTypeCompleteInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private NATType m_NATType;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnQueryNATTypeCompleteInfo other)
	{
		other = default(OnQueryNATTypeCompleteInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.NATType = m_NATType;
	}
}
