namespace Epic.OnlineServices.RTCData;

internal static class OnUpdateSendingCallbackInternalImplementation
{
	private static OnUpdateSendingCallbackInternal s_Delegate;

	public static OnUpdateSendingCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateSendingCallbackInternal))]
	public static void EntryPoint(ref UpdateSendingCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateSendingCallbackInfoInternal, OnUpdateSendingCallback, UpdateSendingCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
