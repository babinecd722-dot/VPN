using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionDetailsCopySessionAttributeByKeyOptionsInternal : ISettable<SessionDetailsCopySessionAttributeByKeyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AttrKey;

	public void Set(ref SessionDetailsCopySessionAttributeByKeyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.AttrKey, ref m_AttrKey);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AttrKey);
	}
}
