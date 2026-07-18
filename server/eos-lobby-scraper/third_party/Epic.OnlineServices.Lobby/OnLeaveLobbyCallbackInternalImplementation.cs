namespace Epic.OnlineServices.Lobby;

internal static class OnLeaveLobbyCallbackInternalImplementation
{
	private static OnLeaveLobbyCallbackInternal s_Delegate;

	public static OnLeaveLobbyCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLeaveLobbyCallbackInternal))]
	public static void EntryPoint(ref LeaveLobbyCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<LeaveLobbyCallbackInfoInternal, OnLeaveLobbyCallback, LeaveLobbyCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
