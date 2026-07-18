namespace Epic.OnlineServices.Friends;

public struct OnFriendsUpdateInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }

	public FriendsStatus PreviousStatus { get; set; }

	public FriendsStatus CurrentStatus { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
