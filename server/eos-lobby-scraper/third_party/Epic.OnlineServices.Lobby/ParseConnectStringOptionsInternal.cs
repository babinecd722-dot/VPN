using System;

namespace Epic.OnlineServices.Lobby;

internal struct ParseConnectStringOptionsInternal : ISettable<ParseConnectStringOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ConnectString;

	public void Set(ref ParseConnectStringOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.ConnectString, ref m_ConnectString);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ConnectString);
	}
}
