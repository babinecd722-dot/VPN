using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct GetProtectMessageOutputLengthOptionsInternal : ISettable<GetProtectMessageOutputLengthOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_DataLengthBytes;

	public void Set(ref GetProtectMessageOutputLengthOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_DataLengthBytes = other.DataLengthBytes;
	}

	public void Dispose()
	{
	}
}
