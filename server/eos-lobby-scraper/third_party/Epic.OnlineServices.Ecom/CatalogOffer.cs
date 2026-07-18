namespace Epic.OnlineServices.Ecom;

public struct CatalogOffer
{
	public int ServerIndex { get; set; }

	public Utf8String CatalogNamespace { get; set; }

	public Utf8String Id { get; set; }

	public Utf8String TitleText { get; set; }

	public Utf8String DescriptionText { get; set; }

	public Utf8String LongDescriptionText { get; set; }

	public Utf8String TechnicalDetailsText_DEPRECATED { get; set; }

	public Utf8String CurrencyCode { get; set; }

	public Result PriceResult { get; set; }

	public uint OriginalPrice_DEPRECATED { get; set; }

	public uint CurrentPrice_DEPRECATED { get; set; }

	public byte DiscountPercentage { get; set; }

	public long ExpirationTimestamp { get; set; }

	public uint PurchasedCount_DEPRECATED { get; set; }

	public int PurchaseLimit { get; set; }

	public bool AvailableForPurchase { get; set; }

	public ulong OriginalPrice64 { get; set; }

	public ulong CurrentPrice64 { get; set; }

	public uint DecimalPoint { get; set; }

	public long ReleaseDateTimestamp { get; set; }

	public long EffectiveDateTimestamp { get; set; }
}
