using System;

namespace Epic.OnlineServices.Ecom;

internal struct CatalogOfferInternal : IGettable<CatalogOffer>
{
	private int m_ApiVersion;

	private int m_ServerIndex;

	private IntPtr m_CatalogNamespace;

	private IntPtr m_Id;

	private IntPtr m_TitleText;

	private IntPtr m_DescriptionText;

	private IntPtr m_LongDescriptionText;

	private IntPtr m_TechnicalDetailsText_DEPRECATED;

	private IntPtr m_CurrencyCode;

	private Result m_PriceResult;

	private uint m_OriginalPrice_DEPRECATED;

	private uint m_CurrentPrice_DEPRECATED;

	private byte m_DiscountPercentage;

	private long m_ExpirationTimestamp;

	private uint m_PurchasedCount_DEPRECATED;

	private int m_PurchaseLimit;

	private int m_AvailableForPurchase;

	private ulong m_OriginalPrice64;

	private ulong m_CurrentPrice64;

	private uint m_DecimalPoint;

	private long m_ReleaseDateTimestamp;

	private long m_EffectiveDateTimestamp;

	public void Get(out CatalogOffer other)
	{
		other = default(CatalogOffer);
		other.ServerIndex = m_ServerIndex;
		Helper.Get(m_CatalogNamespace, out Utf8String to);
		other.CatalogNamespace = to;
		Helper.Get(m_Id, out Utf8String to2);
		other.Id = to2;
		Helper.Get(m_TitleText, out Utf8String to3);
		other.TitleText = to3;
		Helper.Get(m_DescriptionText, out Utf8String to4);
		other.DescriptionText = to4;
		Helper.Get(m_LongDescriptionText, out Utf8String to5);
		other.LongDescriptionText = to5;
		Helper.Get(m_TechnicalDetailsText_DEPRECATED, out Utf8String to6);
		other.TechnicalDetailsText_DEPRECATED = to6;
		Helper.Get(m_CurrencyCode, out Utf8String to7);
		other.CurrencyCode = to7;
		other.PriceResult = m_PriceResult;
		other.OriginalPrice_DEPRECATED = m_OriginalPrice_DEPRECATED;
		other.CurrentPrice_DEPRECATED = m_CurrentPrice_DEPRECATED;
		other.DiscountPercentage = m_DiscountPercentage;
		other.ExpirationTimestamp = m_ExpirationTimestamp;
		other.PurchasedCount_DEPRECATED = m_PurchasedCount_DEPRECATED;
		other.PurchaseLimit = m_PurchaseLimit;
		Helper.Get(m_AvailableForPurchase, out bool to8);
		other.AvailableForPurchase = to8;
		other.OriginalPrice64 = m_OriginalPrice64;
		other.CurrentPrice64 = m_CurrentPrice64;
		other.DecimalPoint = m_DecimalPoint;
		other.ReleaseDateTimestamp = m_ReleaseDateTimestamp;
		other.EffectiveDateTimestamp = m_EffectiveDateTimestamp;
	}
}
