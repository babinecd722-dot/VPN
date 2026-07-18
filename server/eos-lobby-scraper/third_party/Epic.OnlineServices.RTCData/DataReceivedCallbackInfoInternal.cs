using System;

namespace Epic.OnlineServices.RTCData;

internal struct DataReceivedCallbackInfoInternal : ICallbackInfoInternal, IGettable<DataReceivedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	private IntPtr m_ParticipantId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out DataReceivedCallbackInfo other)
	{
		other = default(DataReceivedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		Helper.Get(m_Data, out ArraySegment<byte> to4, m_DataLengthBytes);
		other.Data = to4;
		Helper.Get(m_ParticipantId, out ProductUserId to5);
		other.ParticipantId = to5;
	}
}
