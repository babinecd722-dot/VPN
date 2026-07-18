using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct QueryOutputDevicesInformationOptionsInternal : ISettable<QueryOutputDevicesInformationOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref QueryOutputDevicesInformationOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
