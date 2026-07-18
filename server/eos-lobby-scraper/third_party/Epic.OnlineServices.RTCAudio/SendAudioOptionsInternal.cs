using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct SendAudioOptionsInternal : ISettable<SendAudioOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_Buffer;

	public void Set(ref SendAudioOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set<AudioBuffer, AudioBufferInternal>(other.Buffer, ref m_Buffer);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_Buffer);
	}
}
