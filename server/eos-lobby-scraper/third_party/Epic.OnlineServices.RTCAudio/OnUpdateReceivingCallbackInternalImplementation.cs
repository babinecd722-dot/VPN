namespace Epic.OnlineServices.RTCAudio;

internal static class OnUpdateReceivingCallbackInternalImplementation
{
	private static OnUpdateReceivingCallbackInternal s_Delegate;

	public static OnUpdateReceivingCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateReceivingCallbackInternal))]
	public static void EntryPoint(ref UpdateReceivingCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateReceivingCallbackInfoInternal, OnUpdateReceivingCallback, UpdateReceivingCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
