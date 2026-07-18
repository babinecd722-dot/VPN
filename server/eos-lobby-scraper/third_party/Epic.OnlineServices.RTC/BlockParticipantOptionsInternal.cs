using System;

namespace Epic.OnlineServices.RTC;

internal struct BlockParticipantOptionsInternal : ISettable<BlockParticipantOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private int m_Blocked;

	public void Set(ref BlockParticipantOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set((Handle)other.ParticipantId, ref m_ParticipantId);
		Helper.Set(other.Blocked, ref m_Blocked);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_ParticipantId);
	}
}
