namespace Epic.OnlineServices.KWS;

internal static class OnQueryAgeGateCallbackInternalImplementation
{
	private static OnQueryAgeGateCallbackInternal s_Delegate;

	public static OnQueryAgeGateCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryAgeGateCallbackInternal))]
	public static void EntryPoint(ref QueryAgeGateCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryAgeGateCallbackInfoInternal, OnQueryAgeGateCallback, QueryAgeGateCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
