namespace Epic.OnlineServices.Achievements;

internal static class OnAchievementsUnlockedCallbackV2InternalImplementation
{
	private static OnAchievementsUnlockedCallbackV2Internal s_Delegate;

	public static OnAchievementsUnlockedCallbackV2Internal Delegate
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

	[MonoPInvokeCallback(typeof(OnAchievementsUnlockedCallbackV2Internal))]
	public static void EntryPoint(ref OnAchievementsUnlockedCallbackV2InfoInternal data)
	{
		if (Helper.TryGetCallback<OnAchievementsUnlockedCallbackV2InfoInternal, OnAchievementsUnlockedCallbackV2, OnAchievementsUnlockedCallbackV2Info>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
