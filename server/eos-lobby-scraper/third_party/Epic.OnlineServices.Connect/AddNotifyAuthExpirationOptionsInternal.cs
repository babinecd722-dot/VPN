using System;

namespace Epic.OnlineServices.Connect;

internal struct AddNotifyAuthExpirationOptionsInternal : ISettable<AddNotifyAuthExpirationOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyAuthExpirationOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
