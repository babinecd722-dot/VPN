namespace Epic.OnlineServices.RTC;

internal static class OnJoinRoomCallbackInternalImplementation
{
	private static OnJoinRoomCallbackInternal s_Delegate;

	public static OnJoinRoomCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinRoomCallbackInternal))]
	public static void EntryPoint(ref JoinRoomCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<JoinRoomCallbackInfoInternal, OnJoinRoomCallback, JoinRoomCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
