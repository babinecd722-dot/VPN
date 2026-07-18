namespace Epic.OnlineServices.Ecom;

public struct GetLastRedeemEntitlementsResultCountOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public RedeemEntitlementsResultListType ResultType { get; set; }
}
