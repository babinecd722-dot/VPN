using System;

namespace Epic.OnlineServices.Connect;

internal struct LinkAccountOptionsInternal : ISettable<LinkAccountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ContinuanceToken;

	public void Set(ref LinkAccountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.ContinuanceToken, ref m_ContinuanceToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ContinuanceToken);
	}
}
