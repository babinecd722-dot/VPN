namespace Epic.OnlineServices.Auth;

public struct LoginCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public EpicAccountId LocalUserId { get; set; }

	public PinGrantInfo? PinGrantInfo { get; set; }

	public ContinuanceToken ContinuanceToken { get; set; }

	public AccountFeatureRestrictedInfo? AccountFeatureRestrictedInfo_DEPRECATED { get; set; }

	public EpicAccountId SelectedAccountId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
