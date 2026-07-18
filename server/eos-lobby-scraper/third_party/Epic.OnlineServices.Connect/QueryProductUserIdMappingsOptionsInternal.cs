using System;

namespace Epic.OnlineServices.Connect;

internal struct QueryProductUserIdMappingsOptionsInternal : ISettable<QueryProductUserIdMappingsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private ExternalAccountType m_AccountIdType_DEPRECATED;

	private IntPtr m_ProductUserIds;

	private uint m_ProductUserIdCount;

	public void Set(ref QueryProductUserIdMappingsOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_AccountIdType_DEPRECATED = other.AccountIdType_DEPRECATED;
		Helper.Set(other.ProductUserIds, ref m_ProductUserIds, out m_ProductUserIdCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ProductUserIds);
	}
}
