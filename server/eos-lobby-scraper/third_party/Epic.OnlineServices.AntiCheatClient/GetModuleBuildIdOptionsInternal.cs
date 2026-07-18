using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct GetModuleBuildIdOptionsInternal : ISettable<GetModuleBuildIdOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetModuleBuildIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
