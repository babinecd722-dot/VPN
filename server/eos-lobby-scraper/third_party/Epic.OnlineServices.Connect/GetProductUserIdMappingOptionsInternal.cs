using System;

namespace Epic.OnlineServices.Connect;

internal struct GetProductUserIdMappingOptionsInternal : ISettable<GetProductUserIdMappingOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private ExternalAccountType m_AccountIdType;

	private IntPtr m_TargetProductUserId;

	public void Set(ref GetProductUserIdMappingOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_AccountIdType = other.AccountIdType;
		Helper.Set((Handle)other.TargetProductUserId, ref m_TargetProductUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetProductUserId);
	}
}
