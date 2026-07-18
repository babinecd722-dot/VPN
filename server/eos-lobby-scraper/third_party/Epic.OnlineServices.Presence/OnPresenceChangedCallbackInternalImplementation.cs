namespace Epic.OnlineServices.Presence;

internal static class OnPresenceChangedCallbackInternalImplementation
{
	private static OnPresenceChangedCallbackInternal s_Delegate;

	public static OnPresenceChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnPresenceChangedCallbackInternal))]
	public static void EntryPoint(ref PresenceChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<PresenceChangedCallbackInfoInternal, OnPresenceChangedCallback, PresenceChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
