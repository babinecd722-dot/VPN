namespace Epic.OnlineServices.Lobby;

internal static class OnLeaveLobbyRequestedCallbackInternalImplementation
{
	private static OnLeaveLobbyRequestedCallbackInternal s_Delegate;

	public static OnLeaveLobbyRequestedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLeaveLobbyRequestedCallbackInternal))]
	public static void EntryPoint(ref LeaveLobbyRequestedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LeaveLobbyRequestedCallbackInfoInternal, OnLeaveLobbyRequestedCallback, LeaveLobbyRequestedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
