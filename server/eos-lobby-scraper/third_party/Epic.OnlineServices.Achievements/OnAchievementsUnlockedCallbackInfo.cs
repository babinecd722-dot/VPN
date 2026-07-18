namespace Epic.OnlineServices.Achievements;

public struct OnAchievementsUnlockedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId UserId { get; set; }

	public Utf8String[] AchievementIds { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
