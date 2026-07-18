namespace Epic.OnlineServices.RTC;

internal static class OnLeaveRoomCallbackInternalImplementation
{
	private static OnLeaveRoomCallbackInternal s_Delegate;

	public static OnLeaveRoomCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLeaveRoomCallbackInternal))]
	public static void EntryPoint(ref LeaveRoomCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<LeaveRoomCallbackInfoInternal, OnLeaveRoomCallback, LeaveRoomCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
