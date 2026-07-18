using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AddNotifyAudioInputStateOptionsInternal : ISettable<AddNotifyAudioInputStateOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	public void Set(ref AddNotifyAudioInputStateOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
	}
}
