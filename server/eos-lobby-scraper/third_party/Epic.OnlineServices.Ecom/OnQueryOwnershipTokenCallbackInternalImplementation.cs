namespace Epic.OnlineServices.Ecom;

internal static class OnQueryOwnershipTokenCallbackInternalImplementation
{
	private static OnQueryOwnershipTokenCallbackInternal s_Delegate;

	public static OnQueryOwnershipTokenCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryOwnershipTokenCallbackInternal))]
	public static void EntryPoint(ref QueryOwnershipTokenCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryOwnershipTokenCallbackInfoInternal, OnQueryOwnershipTokenCallback, QueryOwnershipTokenCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
