using System;

namespace Epic.OnlineServices.Stats;

internal struct OnQueryStatsCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnQueryStatsCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnQueryStatsCompleteCallbackInfo other)
	{
		other = default(OnQueryStatsCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetUserId, out ProductUserId to3);
		other.TargetUserId = to3;
	}
}
