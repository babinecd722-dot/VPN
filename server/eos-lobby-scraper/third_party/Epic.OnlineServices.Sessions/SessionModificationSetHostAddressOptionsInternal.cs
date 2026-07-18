using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationSetHostAddressOptionsInternal : ISettable<SessionModificationSetHostAddressOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_HostAddress;

	public void Set(ref SessionModificationSetHostAddressOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.HostAddress, ref m_HostAddress);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_HostAddress);
	}
}
