namespace Epic.OnlineServices.Presence;

internal static class SetPresenceCompleteCallbackInternalImplementation
{
	private static SetPresenceCompleteCallbackInternal s_Delegate;

	public static SetPresenceCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(SetPresenceCompleteCallbackInternal))]
	public static void EntryPoint(ref SetPresenceCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SetPresenceCallbackInfoInternal, SetPresenceCompleteCallback, SetPresenceCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
