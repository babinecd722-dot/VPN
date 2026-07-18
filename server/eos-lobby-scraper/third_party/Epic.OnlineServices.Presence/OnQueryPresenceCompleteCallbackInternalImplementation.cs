namespace Epic.OnlineServices.Presence;

internal static class OnQueryPresenceCompleteCallbackInternalImplementation
{
	private static OnQueryPresenceCompleteCallbackInternal s_Delegate;

	public static OnQueryPresenceCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryPresenceCompleteCallbackInternal))]
	public static void EntryPoint(ref QueryPresenceCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryPresenceCallbackInfoInternal, OnQueryPresenceCompleteCallback, QueryPresenceCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
