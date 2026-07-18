using System;

namespace Epic.OnlineServices.RTC;

internal struct JoinRoomOptionsInternal : ISettable<JoinRoomOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ClientBaseUrl;

	private IntPtr m_ParticipantToken;

	private IntPtr m_ParticipantId;

	private JoinRoomFlags m_Flags;

	private int m_ManualAudioInputEnabled;

	private int m_ManualAudioOutputEnabled;

	public void Set(ref JoinRoomOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set(other.ClientBaseUrl, ref m_ClientBaseUrl);
		Helper.Set(other.ParticipantToken, ref m_ParticipantToken);
		Helper.Set((Handle)other.ParticipantId, ref m_ParticipantId);
		m_Flags = other.Flags;
		Helper.Set(other.ManualAudioInputEnabled, ref m_ManualAudioInputEnabled);
		Helper.Set(other.ManualAudioOutputEnabled, ref m_ManualAudioOutputEnabled);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_ClientBaseUrl);
		Helper.Dispose(ref m_ParticipantToken);
		Helper.Dispose(ref m_ParticipantId);
	}
}
