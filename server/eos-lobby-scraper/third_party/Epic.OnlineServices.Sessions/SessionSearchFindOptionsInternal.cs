using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionSearchFindOptionsInternal : ISettable<SessionSearchFindOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	public void Set(ref SessionSearchFindOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
