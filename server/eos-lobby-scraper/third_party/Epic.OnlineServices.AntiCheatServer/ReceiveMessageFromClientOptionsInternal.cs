using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct ReceiveMessageFromClientOptionsInternal : ISettable<ReceiveMessageFromClientOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	public void Set(ref ReceiveMessageFromClientOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_ClientHandle = other.ClientHandle;
		Helper.Set(other.Data, ref m_Data, out m_DataLengthBytes);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Data);
	}
}
