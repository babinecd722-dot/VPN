using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct AddNotifyClientIntegrityViolatedOptionsInternal : ISettable<AddNotifyClientIntegrityViolatedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyClientIntegrityViolatedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
