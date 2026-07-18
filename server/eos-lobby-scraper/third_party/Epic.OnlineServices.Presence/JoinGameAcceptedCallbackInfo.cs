namespace Epic.OnlineServices.Presence;

public struct JoinGameAcceptedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public Utf8String JoinInfo { get; set; }

	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }

	public ulong UiEventId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
