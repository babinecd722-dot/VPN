using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionSearchSetSessionIdOptionsInternal : ISettable<SessionSearchSetSessionIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionId;

	public void Set(ref SessionSearchSetSessionIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.SessionId, ref m_SessionId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionId);
	}
}
