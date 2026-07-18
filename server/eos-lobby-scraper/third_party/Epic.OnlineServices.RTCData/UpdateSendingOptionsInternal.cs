using System;

namespace Epic.OnlineServices.RTCData;

internal struct UpdateSendingOptionsInternal : ISettable<UpdateSendingOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private int m_DataEnabled;

	public void Set(ref UpdateSendingOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set(other.DataEnabled, ref m_DataEnabled);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
	}
}
