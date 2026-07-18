namespace Epic.OnlineServices.Sessions;

internal static class OnRegisterPlayersCallbackInternalImplementation
{
	private static OnRegisterPlayersCallbackInternal s_Delegate;

	public static OnRegisterPlayersCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRegisterPlayersCallbackInternal))]
	public static void EntryPoint(ref RegisterPlayersCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<RegisterPlayersCallbackInfoInternal, OnRegisterPlayersCallback, RegisterPlayersCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
