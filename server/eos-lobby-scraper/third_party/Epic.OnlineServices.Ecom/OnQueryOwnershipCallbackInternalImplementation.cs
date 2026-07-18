namespace Epic.OnlineServices.Ecom;

internal static class OnQueryOwnershipCallbackInternalImplementation
{
	private static OnQueryOwnershipCallbackInternal s_Delegate;

	public static OnQueryOwnershipCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryOwnershipCallbackInternal))]
	public static void EntryPoint(ref QueryOwnershipCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryOwnershipCallbackInfoInternal, OnQueryOwnershipCallback, QueryOwnershipCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
