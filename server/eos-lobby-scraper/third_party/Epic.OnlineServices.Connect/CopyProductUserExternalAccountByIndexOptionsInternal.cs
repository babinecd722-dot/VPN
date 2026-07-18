using System;

namespace Epic.OnlineServices.Connect;

internal struct CopyProductUserExternalAccountByIndexOptionsInternal : ISettable<CopyProductUserExternalAccountByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private uint m_ExternalAccountInfoIndex;

	public void Set(ref CopyProductUserExternalAccountByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_ExternalAccountInfoIndex = other.ExternalAccountInfoIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
	}
}
