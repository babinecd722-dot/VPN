namespace Epic.OnlineServices.AntiCheatClient;

internal static class OnMessageToServerCallbackInternalImplementation
{
	private static OnMessageToServerCallbackInternal s_Delegate;

	public static OnMessageToServerCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnMessageToServerCallbackInternal))]
	public static void EntryPoint(ref OnMessageToServerCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnMessageToServerCallbackInfoInternal, OnMessageToServerCallback, OnMessageToServerCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
