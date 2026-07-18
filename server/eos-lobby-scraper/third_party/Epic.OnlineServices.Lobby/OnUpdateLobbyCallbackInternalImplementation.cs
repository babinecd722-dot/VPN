namespace Epic.OnlineServices.Lobby;

internal static class OnUpdateLobbyCallbackInternalImplementation
{
	private static OnUpdateLobbyCallbackInternal s_Delegate;

	public static OnUpdateLobbyCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateLobbyCallbackInternal))]
	public static void EntryPoint(ref UpdateLobbyCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateLobbyCallbackInfoInternal, OnUpdateLobbyCallback, UpdateLobbyCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
