using System;

namespace Epic.OnlineServices.Connect;

internal struct GetExternalAccountMappingsOptionsInternal : ISettable<GetExternalAccountMappingsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private ExternalAccountType m_AccountIdType;

	private IntPtr m_TargetExternalUserId;

	public void Set(ref GetExternalAccountMappingsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_AccountIdType = other.AccountIdType;
		Helper.Set(other.TargetExternalUserId, ref m_TargetExternalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetExternalUserId);
	}
}
