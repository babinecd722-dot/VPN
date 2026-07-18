using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AddNotifyAudioBeforeRenderOptionsInternal : ISettable<AddNotifyAudioBeforeRenderOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private int m_UnmixedAudio;

	public void Set(ref AddNotifyAudioBeforeRenderOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set(other.UnmixedAudio, ref m_UnmixedAudio);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
	}
}
