using System;

namespace Epic.OnlineServices.RTCData;

internal struct ParticipantUpdatedCallbackInfoInternal : ICallbackInfoInternal, IGettable<ParticipantUpdatedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private RTCDataStatus m_DataStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out ParticipantUpdatedCallbackInfo other)
	{
		other = default(ParticipantUpdatedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		Helper.Get(m_ParticipantId, out ProductUserId to4);
		other.ParticipantId = to4;
		other.DataStatus = m_DataStatus;
	}
}
