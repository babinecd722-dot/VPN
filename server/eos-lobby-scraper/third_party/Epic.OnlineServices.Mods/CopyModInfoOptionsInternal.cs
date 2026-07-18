using System;

namespace Epic.OnlineServices.Mods;

internal struct CopyModInfoOptionsInternal : ISettable<CopyModInfoOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private ModEnumerationType m_Type;

	public void Set(ref CopyModInfoOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_Type = other.Type;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
