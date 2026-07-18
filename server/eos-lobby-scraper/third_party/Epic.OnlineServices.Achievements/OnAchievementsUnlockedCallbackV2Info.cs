using System;

namespace Epic.OnlineServices.Achievements;

public struct OnAchievementsUnlockedCallbackV2Info : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId UserId { get; set; }

	public Utf8String AchievementId { get; set; }

	public DateTimeOffset? UnlockTime { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
