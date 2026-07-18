namespace Epic.OnlineServices.Achievements;

internal static class OnUnlockAchievementsCompleteCallbackInternalImplementation
{
	private static OnUnlockAchievementsCompleteCallbackInternal s_Delegate;

	public static OnUnlockAchievementsCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUnlockAchievementsCompleteCallbackInternal))]
	public static void EntryPoint(ref OnUnlockAchievementsCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnUnlockAchievementsCompleteCallbackInfoInternal, OnUnlockAchievementsCompleteCallback, OnUnlockAchievementsCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
