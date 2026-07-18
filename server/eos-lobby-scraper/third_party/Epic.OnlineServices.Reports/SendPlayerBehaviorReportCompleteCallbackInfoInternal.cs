using System;

namespace Epic.OnlineServices.Reports;

internal struct SendPlayerBehaviorReportCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<SendPlayerBehaviorReportCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SendPlayerBehaviorReportCompleteCallbackInfo other)
	{
		other = default(SendPlayerBehaviorReportCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
