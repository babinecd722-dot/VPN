using System;

namespace Epic.OnlineServices.UserInfo;

internal struct GetLocalPlatformTypeOptionsInternal : ISettable<GetLocalPlatformTypeOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetLocalPlatformTypeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
