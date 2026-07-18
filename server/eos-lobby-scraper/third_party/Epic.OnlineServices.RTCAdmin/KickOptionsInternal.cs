using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct KickOptionsInternal : ISettable<KickOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_RoomName;

	private IntPtr m_TargetUserId;

	public void Set(ref KickOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_TargetUserId);
	}
}
