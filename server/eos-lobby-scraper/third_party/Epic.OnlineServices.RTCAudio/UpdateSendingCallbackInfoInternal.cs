using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct UpdateSendingCallbackInfoInternal : ICallbackInfoInternal, IGettable<UpdateSendingCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private RTCAudioStatus m_AudioStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out UpdateSendingCallbackInfo other)
	{
		other = default(UpdateSendingCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		other.AudioStatus = m_AudioStatus;
	}
}
