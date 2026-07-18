using System;

namespace Epic.OnlineServices.Mods;

internal struct EnumerateModsOptionsInternal : ISettable<EnumerateModsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private ModEnumerationType m_Type;

	public void Set(ref EnumerateModsOptions other)
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
