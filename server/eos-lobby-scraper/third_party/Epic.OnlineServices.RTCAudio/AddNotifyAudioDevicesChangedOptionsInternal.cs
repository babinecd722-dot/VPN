using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AddNotifyAudioDevicesChangedOptionsInternal : ISettable<AddNotifyAudioDevicesChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyAudioDevicesChangedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
