using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct SetParticipantHardMuteOptionsInternal : ISettable<SetParticipantHardMuteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_RoomName;

	private IntPtr m_TargetUserId;

	private int m_Mute;

	public void Set(ref SetParticipantHardMuteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set(other.Mute, ref m_Mute);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_TargetUserId);
	}
}
