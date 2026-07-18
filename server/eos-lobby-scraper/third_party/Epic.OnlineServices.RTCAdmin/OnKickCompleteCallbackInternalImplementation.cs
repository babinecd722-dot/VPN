namespace Epic.OnlineServices.RTCAdmin;

internal static class OnKickCompleteCallbackInternalImplementation
{
	private static OnKickCompleteCallbackInternal s_Delegate;

	public static OnKickCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnKickCompleteCallbackInternal))]
	public static void EntryPoint(ref KickCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<KickCompleteCallbackInfoInternal, OnKickCompleteCallback, KickCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
