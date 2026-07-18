using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct ProtectMessageOptionsInternal : ISettable<ProtectMessageOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	private uint m_OutBufferSizeBytes;

	public void Set(ref ProtectMessageOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_ClientHandle = other.ClientHandle;
		Helper.Set(other.Data, ref m_Data, out m_DataLengthBytes);
		m_OutBufferSizeBytes = other.OutBufferSizeBytes;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Data);
	}
}
