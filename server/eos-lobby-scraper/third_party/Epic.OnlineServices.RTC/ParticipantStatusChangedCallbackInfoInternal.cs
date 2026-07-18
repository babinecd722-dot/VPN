using System;

namespace Epic.OnlineServices.RTC;

internal struct ParticipantStatusChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<ParticipantStatusChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private RTCParticipantStatus m_ParticipantStatus;

	private uint m_ParticipantMetadataCount;

	private IntPtr m_ParticipantMetadata;

	private int m_ParticipantInBlocklist;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out ParticipantStatusChangedCallbackInfo other)
	{
		other = default(ParticipantStatusChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		Helper.Get(m_ParticipantId, out ProductUserId to4);
		other.ParticipantId = to4;
		other.ParticipantStatus = m_ParticipantStatus;
		Helper.Get<ParticipantMetadataInternal, ParticipantMetadata>(m_ParticipantMetadata, out var to5, m_ParticipantMetadataCount, isArrayItemAllocated: false);
		other.ParticipantMetadata = to5;
		Helper.Get(m_ParticipantInBlocklist, out bool to6);
		other.ParticipantInBlocklist = to6;
	}
}
