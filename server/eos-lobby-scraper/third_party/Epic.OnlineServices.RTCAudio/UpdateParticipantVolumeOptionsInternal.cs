using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct UpdateParticipantVolumeOptionsInternal : ISettable<UpdateParticipantVolumeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private float m_Volume;

	public void Set(ref UpdateParticipantVolumeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set((Handle)other.ParticipantId, ref m_ParticipantId);
		m_Volume = other.Volume;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_ParticipantId);
	}
}
