namespace Epic.OnlineServices.CustomInvites;

internal static class OnRequestToJoinAcceptedCallbackInternalImplementation
{
	private static OnRequestToJoinAcceptedCallbackInternal s_Delegate;

	public static OnRequestToJoinAcceptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRequestToJoinAcceptedCallbackInternal))]
	public static void EntryPoint(ref OnRequestToJoinAcceptedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnRequestToJoinAcceptedCallbackInfoInternal, OnRequestToJoinAcceptedCallback, OnRequestToJoinAcceptedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
