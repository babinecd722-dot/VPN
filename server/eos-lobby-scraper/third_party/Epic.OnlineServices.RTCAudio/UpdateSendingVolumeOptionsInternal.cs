using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct UpdateSendingVolumeOptionsInternal : ISettable<UpdateSendingVolumeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private float m_Volume;

	public void Set(ref UpdateSendingVolumeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		m_Volume = other.Volume;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
	}
}
