using System;

namespace Epic.OnlineServices.Auth;

internal struct LinkAccountOptionsInternal : ISettable<LinkAccountOptions>, IDisposable
{
	private int m_ApiVersion;

	private LinkAccountFlags m_LinkAccountFlags;

	private IntPtr m_ContinuanceToken;

	private IntPtr m_LocalUserId;

	public void Set(ref LinkAccountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_LinkAccountFlags = other.LinkAccountFlags;
		Helper.Set((Handle)other.ContinuanceToken, ref m_ContinuanceToken);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ContinuanceToken);
		Helper.Dispose(ref m_LocalUserId);
	}
}
