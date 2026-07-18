namespace Epic.OnlineServices.Presence;

internal static class OnJoinGameAcceptedCallbackInternalImplementation
{
	private static OnJoinGameAcceptedCallbackInternal s_Delegate;

	public static OnJoinGameAcceptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinGameAcceptedCallbackInternal))]
	public static void EntryPoint(ref JoinGameAcceptedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<JoinGameAcceptedCallbackInfoInternal, OnJoinGameAcceptedCallback, JoinGameAcceptedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
