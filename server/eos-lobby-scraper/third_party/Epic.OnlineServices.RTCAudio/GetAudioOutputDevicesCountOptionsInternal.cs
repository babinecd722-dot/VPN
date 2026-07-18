using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct GetAudioOutputDevicesCountOptionsInternal : ISettable<GetAudioOutputDevicesCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetAudioOutputDevicesCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
