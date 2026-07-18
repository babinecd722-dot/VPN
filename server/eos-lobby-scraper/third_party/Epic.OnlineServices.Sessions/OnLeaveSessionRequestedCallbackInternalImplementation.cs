namespace Epic.OnlineServices.Sessions;

internal static class OnLeaveSessionRequestedCallbackInternalImplementation
{
	private static OnLeaveSessionRequestedCallbackInternal s_Delegate;

	public static OnLeaveSessionRequestedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLeaveSessionRequestedCallbackInternal))]
	public static void EntryPoint(ref LeaveSessionRequestedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LeaveSessionRequestedCallbackInfoInternal, OnLeaveSessionRequestedCallback, LeaveSessionRequestedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
