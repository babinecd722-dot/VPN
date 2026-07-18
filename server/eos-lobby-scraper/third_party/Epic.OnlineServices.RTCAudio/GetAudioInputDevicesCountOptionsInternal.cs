using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct GetAudioInputDevicesCountOptionsInternal : ISettable<GetAudioInputDevicesCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetAudioInputDevicesCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
