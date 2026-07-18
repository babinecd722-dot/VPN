namespace Epic.OnlineServices.KWS;

internal static class OnQueryPermissionsCallbackInternalImplementation
{
	private static OnQueryPermissionsCallbackInternal s_Delegate;

	public static OnQueryPermissionsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryPermissionsCallbackInternal))]
	public static void EntryPoint(ref QueryPermissionsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryPermissionsCallbackInfoInternal, OnQueryPermissionsCallback, QueryPermissionsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
