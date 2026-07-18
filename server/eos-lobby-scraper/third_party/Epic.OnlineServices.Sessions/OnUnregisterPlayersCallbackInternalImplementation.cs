namespace Epic.OnlineServices.Sessions;

internal static class OnUnregisterPlayersCallbackInternalImplementation
{
	private static OnUnregisterPlayersCallbackInternal s_Delegate;

	public static OnUnregisterPlayersCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUnregisterPlayersCallbackInternal))]
	public static void EntryPoint(ref UnregisterPlayersCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UnregisterPlayersCallbackInfoInternal, OnUnregisterPlayersCallback, UnregisterPlayersCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
