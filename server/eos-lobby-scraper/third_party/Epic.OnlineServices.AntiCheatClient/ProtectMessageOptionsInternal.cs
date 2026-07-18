using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct ProtectMessageOptionsInternal : ISettable<ProtectMessageOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	private uint m_OutBufferSizeBytes;

	public void Set(ref ProtectMessageOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Data, ref m_Data, out m_DataLengthBytes);
		m_OutBufferSizeBytes = other.OutBufferSizeBytes;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Data);
	}
}
