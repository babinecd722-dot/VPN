namespace Epic.OnlineServices.Lobby;

internal static class OnJoinRTCRoomCallbackInternalImplementation
{
	private static OnJoinRTCRoomCallbackInternal s_Delegate;

	public static OnJoinRTCRoomCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinRTCRoomCallbackInternal))]
	public static void EntryPoint(ref JoinRTCRoomCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<JoinRTCRoomCallbackInfoInternal, OnJoinRTCRoomCallback, JoinRTCRoomCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
