using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct ParticipantUpdatedCallbackInfoInternal : ICallbackInfoInternal, IGettable<ParticipantUpdatedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private int m_Speaking;

	private RTCAudioStatus m_AudioStatus;

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
		Helper.Get(m_Speaking, out bool to5);
		other.Speaking = to5;
		other.AudioStatus = m_AudioStatus;
	}
}
