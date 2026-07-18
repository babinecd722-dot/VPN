namespace Epic.OnlineServices.Achievements;

internal static class OnQueryPlayerAchievementsCompleteCallbackInternalImplementation
{
	private static OnQueryPlayerAchievementsCompleteCallbackInternal s_Delegate;

	public static OnQueryPlayerAchievementsCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryPlayerAchievementsCompleteCallbackInternal))]
	public static void EntryPoint(ref OnQueryPlayerAchievementsCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryPlayerAchievementsCompleteCallbackInfoInternal, OnQueryPlayerAchievementsCompleteCallback, OnQueryPlayerAchievementsCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
