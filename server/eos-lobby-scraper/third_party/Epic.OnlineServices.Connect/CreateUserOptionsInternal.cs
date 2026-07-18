using System;

namespace Epic.OnlineServices.Connect;

internal struct CreateUserOptionsInternal : ISettable<CreateUserOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ContinuanceToken;

	public void Set(ref CreateUserOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.ContinuanceToken, ref m_ContinuanceToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ContinuanceToken);
	}
}
