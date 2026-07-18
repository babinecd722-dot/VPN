using System;

namespace Epic.OnlineServices.Ecom;

internal struct EntitlementInternal : IGettable<Entitlement>
{
	private int m_ApiVersion;

	private IntPtr m_EntitlementName;

	private IntPtr m_EntitlementId;

	private IntPtr m_CatalogItemId;

	private int m_ServerIndex;

	private int m_Redeemed;

	private long m_EndTimestamp;

	public void Get(out Entitlement other)
	{
		other = default(Entitlement);
		Helper.Get(m_EntitlementName, out Utf8String to);
		other.EntitlementName = to;
		Helper.Get(m_EntitlementId, out Utf8String to2);
		other.EntitlementId = to2;
		Helper.Get(m_CatalogItemId, out Utf8String to3);
		other.CatalogItemId = to3;
		other.ServerIndex = m_ServerIndex;
		Helper.Get(m_Redeemed, out bool to4);
		other.Redeemed = to4;
		other.EndTimestamp = m_EndTimestamp;
	}
}
