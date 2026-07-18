using System;

namespace Epic.OnlineServices.RTCData;

internal struct SendDataOptionsInternal : ISettable<SendDataOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	public void Set(ref SendDataOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set(other.Data, ref m_Data, out m_DataLengthBytes);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_Data);
	}
}
