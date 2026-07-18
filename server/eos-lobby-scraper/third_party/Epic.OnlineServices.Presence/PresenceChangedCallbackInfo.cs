namespace Epic.OnlineServices.Presence;

public struct PresenceChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId PresenceUserId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
