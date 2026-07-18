namespace Epic.OnlineServices.KWS;

internal static class OnRequestPermissionsCallbackInternalImplementation
{
	private static OnRequestPermissionsCallbackInternal s_Delegate;

	public static OnRequestPermissionsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRequestPermissionsCallbackInternal))]
	public static void EntryPoint(ref RequestPermissionsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<RequestPermissionsCallbackInfoInternal, OnRequestPermissionsCallback, RequestPermissionsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
