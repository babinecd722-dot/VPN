using System;

namespace Epic.OnlineServices.Lobby;

internal struct LocalRTCOptionsInternal : ISettable<LocalRTCOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_Flags;

	private int m_UseManualAudioInput;

	private int m_UseManualAudioOutput;

	private int m_LocalAudioDeviceInputStartsMuted;

	private IntPtr m_Reserved;

	public void Set(ref LocalRTCOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		m_Flags = other.Flags;
		Helper.Set(other.UseManualAudioInput, ref m_UseManualAudioInput);
		Helper.Set(other.UseManualAudioOutput, ref m_UseManualAudioOutput);
		Helper.Set(other.LocalAudioDeviceInputStartsMuted, ref m_LocalAudioDeviceInputStartsMuted);
		m_Reserved = other.Reserved;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Reserved);
	}
}
