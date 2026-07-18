using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct SetAudioOutputSettingsOptionsInternal : ISettable<SetAudioOutputSettingsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_DeviceId;

	private float m_Volume;

	public void Set(ref SetAudioOutputSettingsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.DeviceId, ref m_DeviceId);
		m_Volume = other.Volume;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_DeviceId);
	}
}
