using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct GetOutputDevicesCountOptionsInternal : ISettable<GetOutputDevicesCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetOutputDevicesCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
