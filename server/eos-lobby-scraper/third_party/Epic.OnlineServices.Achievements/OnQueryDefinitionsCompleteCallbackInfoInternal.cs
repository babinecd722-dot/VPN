using System;

namespace Epic.OnlineServices.Achievements;

internal struct OnQueryDefinitionsCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnQueryDefinitionsCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnQueryDefinitionsCompleteCallbackInfo other)
	{
		other = default(OnQueryDefinitionsCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
