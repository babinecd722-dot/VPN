namespace Epic.OnlineServices.RTC;

internal static class OnDisconnectedCallbackInternalImplementation
{
	private static OnDisconnectedCallbackInternal s_Delegate;

	public static OnDisconnectedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDisconnectedCallbackInternal))]
	public static void EntryPoint(ref DisconnectedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<DisconnectedCallbackInfoInternal, OnDisconnectedCallback, DisconnectedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
