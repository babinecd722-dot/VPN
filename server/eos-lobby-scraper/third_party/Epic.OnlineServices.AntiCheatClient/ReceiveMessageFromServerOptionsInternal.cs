using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct ReceiveMessageFromServerOptionsInternal : ISettable<ReceiveMessageFromServerOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	public void Set(ref ReceiveMessageFromServerOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Data, ref m_Data, out m_DataLengthBytes);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Data);
	}
}
