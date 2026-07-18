using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct QueryInputDevicesInformationOptionsInternal : ISettable<QueryInputDevicesInformationOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref QueryInputDevicesInformationOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
