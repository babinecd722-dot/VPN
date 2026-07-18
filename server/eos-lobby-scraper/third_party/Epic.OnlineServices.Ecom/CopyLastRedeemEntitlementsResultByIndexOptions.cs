namespace Epic.OnlineServices.Ecom;

public struct CopyLastRedeemEntitlementsResultByIndexOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public uint EntitlementIndex { get; set; }

	public RedeemEntitlementsResultListType ResultType { get; set; }
}
