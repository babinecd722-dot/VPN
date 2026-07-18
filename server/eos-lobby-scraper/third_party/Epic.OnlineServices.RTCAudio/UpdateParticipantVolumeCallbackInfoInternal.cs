using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct UpdateParticipantVolumeCallbackInfoInternal : ICallbackInfoInternal, IGettable<UpdateParticipantVolumeCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private float m_Volume;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out UpdateParticipantVolumeCallbackInfo other)
	{
		other = default(UpdateParticipantVolumeCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		Helper.Get(m_ParticipantId, out ProductUserId to4);
		other.ParticipantId = to4;
		other.Volume = m_Volume;
	}
}
