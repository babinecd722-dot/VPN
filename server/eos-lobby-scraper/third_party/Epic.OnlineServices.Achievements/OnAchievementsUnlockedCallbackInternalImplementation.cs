namespace Epic.OnlineServices.Achievements;

internal static class OnAchievementsUnlockedCallbackInternalImplementation
{
	private static OnAchievementsUnlockedCallbackInternal s_Delegate;

	public static OnAchievementsUnlockedCallbackInternal Delegate
	{
		get
		{
			if (s_Delegate == null)
			{
				s_Delegate = EntryPoint;
			}
			return s_Delegate;
		}
	}

	[MonoPInvokeCallback(typeof(OnAchievementsUnlockedCallbackInternal))]
	public static void EntryPoint(ref OnAchievementsUnlockedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnAchievementsUnlockedCallbackInfoInternal, OnAchievementsUnlockedCallback, OnAchievementsUnlockedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
