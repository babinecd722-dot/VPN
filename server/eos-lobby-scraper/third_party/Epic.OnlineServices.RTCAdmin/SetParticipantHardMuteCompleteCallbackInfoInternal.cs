using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct SetParticipantHardMuteCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<SetParticipantHardMuteCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SetParticipantHardMuteCompleteCallbackInfo other)
	{
		other = default(SetParticipantHardMuteCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
