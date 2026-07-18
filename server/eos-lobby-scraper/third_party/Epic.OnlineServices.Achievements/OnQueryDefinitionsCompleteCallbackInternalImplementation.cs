namespace Epic.OnlineServices.Achievements;

internal static class OnQueryDefinitionsCompleteCallbackInternalImplementation
{
	private static OnQueryDefinitionsCompleteCallbackInternal s_Delegate;

	public static OnQueryDefinitionsCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryDefinitionsCompleteCallbackInternal))]
	public static void EntryPoint(ref OnQueryDefinitionsCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryDefinitionsCompleteCallbackInfoInternal, OnQueryDefinitionsCompleteCallback, OnQueryDefinitionsCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
