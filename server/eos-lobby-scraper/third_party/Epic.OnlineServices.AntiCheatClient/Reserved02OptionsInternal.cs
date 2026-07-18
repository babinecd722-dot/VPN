using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct Reserved02OptionsInternal : ISettable<Reserved02Options>, IDisposable
{
	private int m_ApiVersion;

	private long m_Reserved1;

	private uint m_Reserved2;

	private uint m_Reserved3;

	private IntPtr m_Reserved4;

	public void Set(ref Reserved02Options other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_Reserved1 = other.Reserved1;
		m_Reserved2 = other.Reserved2;
		m_Reserved3 = other.Reserved3;
		m_Reserved4 = other.Reserved4;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Reserved4);
	}
}
