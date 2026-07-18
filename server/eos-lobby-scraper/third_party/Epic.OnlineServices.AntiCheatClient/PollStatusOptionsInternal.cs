using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct PollStatusOptionsInternal : ISettable<PollStatusOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_OutMessageLength;

	public void Set(ref PollStatusOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_OutMessageLength = other.OutMessageLength;
	}

	public void Dispose()
	{
	}
}
