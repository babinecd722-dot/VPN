using System;

namespace Epic.OnlineServices.Ecom;

internal struct GetEntitlementsByNameCountOptionsInternal : ISettable<GetEntitlementsByNameCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_EntitlementName;

	public void Set(ref GetEntitlementsByNameCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.EntitlementName, ref m_EntitlementName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EntitlementName);
	}
}
