using System;

namespace Epic.OnlineServices.Connect;

internal struct QueryExternalAccountMappingsOptionsInternal : ISettable<QueryExternalAccountMappingsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private ExternalAccountType m_AccountIdType;

	private IntPtr m_ExternalAccountIds;

	private uint m_ExternalAccountIdCount;

	public void Set(ref QueryExternalAccountMappingsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_AccountIdType = other.AccountIdType;
		Helper.Set(other.ExternalAccountIds, ref m_ExternalAccountIds, out m_ExternalAccountIdCount, isArrayItemAllocated: true);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ExternalAccountIds);
	}
}
