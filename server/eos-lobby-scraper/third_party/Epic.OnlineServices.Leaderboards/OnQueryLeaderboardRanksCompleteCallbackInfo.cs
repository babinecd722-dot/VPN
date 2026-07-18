namespace Epic.OnlineServices.Leaderboards;

public struct OnQueryLeaderboardRanksCompleteCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public Utf8String LeaderboardId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
