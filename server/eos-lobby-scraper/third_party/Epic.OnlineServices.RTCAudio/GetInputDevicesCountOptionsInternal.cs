using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct GetInputDevicesCountOptionsInternal : ISettable<GetInputDevicesCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetInputDevicesCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
