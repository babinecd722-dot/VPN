using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct ReceiveMessageFromPeerOptionsInternal : ISettable<ReceiveMessageFromPeerOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PeerHandle;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	public void Set(ref ReceiveMessageFromPeerOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_PeerHandle = other.PeerHandle;
		Helper.Set(other.Data, ref m_Data, out m_DataLengthBytes);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Data);
	}
}
