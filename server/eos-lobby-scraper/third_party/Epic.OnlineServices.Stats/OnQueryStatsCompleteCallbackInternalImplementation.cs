namespace Epic.OnlineServices.Stats;

internal static class OnQueryStatsCompleteCallbackInternalImplementation
{
	private static OnQueryStatsCompleteCallbackInternal s_Delegate;

	public static OnQueryStatsCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryStatsCompleteCallbackInternal))]
	public static void EntryPoint(ref OnQueryStatsCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryStatsCompleteCallbackInfoInternal, OnQueryStatsCompleteCallback, OnQueryStatsCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
