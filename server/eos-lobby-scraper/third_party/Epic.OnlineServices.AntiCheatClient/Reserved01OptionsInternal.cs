using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct Reserved01OptionsInternal : ISettable<Reserved01Options>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref Reserved01Options other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
