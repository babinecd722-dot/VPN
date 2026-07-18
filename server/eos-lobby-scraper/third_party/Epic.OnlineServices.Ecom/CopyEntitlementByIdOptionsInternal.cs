using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyEntitlementByIdOptionsInternal : ISettable<CopyEntitlementByIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_EntitlementId;

	public void Set(ref CopyEntitlementByIdOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.EntitlementId, ref m_EntitlementId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EntitlementId);
	}
}
