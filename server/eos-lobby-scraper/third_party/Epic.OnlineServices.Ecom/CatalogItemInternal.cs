using System;

namespace Epic.OnlineServices.Ecom;

internal struct CatalogItemInternal : IGettable<CatalogItem>
{
	private int m_ApiVersion;

	private IntPtr m_CatalogNamespace;

	private IntPtr m_Id;

	private IntPtr m_EntitlementName;

	private IntPtr m_TitleText;

	private IntPtr m_DescriptionText;

	private IntPtr m_LongDescriptionText;

	private IntPtr m_TechnicalDetailsText;

	private IntPtr m_DeveloperText;

	private EcomItemType m_ItemType;

	private long m_EntitlementEndTimestamp;

	public void Get(out CatalogItem other)
	{
		other = default(CatalogItem);
		Helper.Get(m_CatalogNamespace, out Utf8String to);
		other.CatalogNamespace = to;
		Helper.Get(m_Id, out Utf8String to2);
		other.Id = to2;
		Helper.Get(m_EntitlementName, out Utf8String to3);
		other.EntitlementName = to3;
		Helper.Get(m_TitleText, out Utf8String to4);
		other.TitleText = to4;
		Helper.Get(m_DescriptionText, out Utf8String to5);
		other.DescriptionText = to5;
		Helper.Get(m_LongDescriptionText, out Utf8String to6);
		other.LongDescriptionText = to6;
		Helper.Get(m_TechnicalDetailsText, out Utf8String to7);
		other.TechnicalDetailsText = to7;
		Helper.Get(m_DeveloperText, out Utf8String to8);
		other.DeveloperText = to8;
		other.ItemType = m_ItemType;
		other.EntitlementEndTimestamp = m_EntitlementEndTimestamp;
	}
}
