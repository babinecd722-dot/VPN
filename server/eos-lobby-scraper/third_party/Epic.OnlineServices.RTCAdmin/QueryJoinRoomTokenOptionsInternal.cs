using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct QueryJoinRoomTokenOptionsInternal : ISettable<QueryJoinRoomTokenOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_TargetUserIds;

	private uint m_TargetUserIdsCount;

	private IntPtr m_TargetUserIpAddresses;

	public void Set(ref QueryJoinRoomTokenOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set(other.TargetUserIds, ref m_TargetUserIds, out m_TargetUserIdsCount, isArrayItemAllocated: false);
		Helper.Set(other.TargetUserIpAddresses, ref m_TargetUserIpAddresses);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_TargetUserIds);
		Helper.Dispose(ref m_TargetUserIpAddresses);
	}
}
