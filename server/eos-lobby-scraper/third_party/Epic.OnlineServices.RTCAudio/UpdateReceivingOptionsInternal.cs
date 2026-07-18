using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct UpdateReceivingOptionsInternal : ISettable<UpdateReceivingOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private int m_AudioEnabled;

	public void Set(ref UpdateReceivingOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set((Handle)other.ParticipantId, ref m_ParticipantId);
		Helper.Set(other.AudioEnabled, ref m_AudioEnabled);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_ParticipantId);
	}
}
