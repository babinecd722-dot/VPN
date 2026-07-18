namespace Epic.OnlineServices.KWS;

internal static class OnPermissionsUpdateReceivedCallbackInternalImplementation
{
	private static OnPermissionsUpdateReceivedCallbackInternal s_Delegate;

	public static OnPermissionsUpdateReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnPermissionsUpdateReceivedCallbackInternal))]
	public static void EntryPoint(ref PermissionsUpdateReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<PermissionsUpdateReceivedCallbackInfoInternal, OnPermissionsUpdateReceivedCallback, PermissionsUpdateReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
