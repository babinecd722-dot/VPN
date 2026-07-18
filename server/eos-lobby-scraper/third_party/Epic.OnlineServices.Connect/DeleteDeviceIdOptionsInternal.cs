using System;

namespace Epic.OnlineServices.Connect;

internal struct DeleteDeviceIdOptionsInternal : ISettable<DeleteDeviceIdOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref DeleteDeviceIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
