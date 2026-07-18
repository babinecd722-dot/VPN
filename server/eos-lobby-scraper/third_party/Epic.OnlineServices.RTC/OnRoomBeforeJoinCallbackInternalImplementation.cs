namespace Epic.OnlineServices.RTC;

internal static class OnRoomBeforeJoinCallbackInternalImplementation
{
	private static OnRoomBeforeJoinCallbackInternal s_Delegate;

	public static OnRoomBeforeJoinCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRoomBeforeJoinCallbackInternal))]
	public static void EntryPoint(ref RoomBeforeJoinCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<RoomBeforeJoinCallbackInfoInternal, OnRoomBeforeJoinCallback, RoomBeforeJoinCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
