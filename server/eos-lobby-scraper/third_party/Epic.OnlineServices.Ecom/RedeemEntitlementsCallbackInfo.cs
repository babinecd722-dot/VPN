namespace Epic.OnlineServices.Ecom;

public struct RedeemEntitlementsCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public EpicAccountId LocalUserId { get; set; }

	public uint RedeemedEntitlementIdsCount { get; set; }

	public uint PreviouslyRedeemedEntitlementIdsCount { get; set; }

	public uint InvalidEntitlementIdsCount { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
