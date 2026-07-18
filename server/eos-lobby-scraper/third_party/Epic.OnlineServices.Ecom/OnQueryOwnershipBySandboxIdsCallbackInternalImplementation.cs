namespace Epic.OnlineServices.Ecom;

internal static class OnQueryOwnershipBySandboxIdsCallbackInternalImplementation
{
	private static OnQueryOwnershipBySandboxIdsCallbackInternal s_Delegate;

	public static OnQueryOwnershipBySandboxIdsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryOwnershipBySandboxIdsCallbackInternal))]
	public static void EntryPoint(ref QueryOwnershipBySandboxIdsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryOwnershipBySandboxIdsCallbackInfoInternal, OnQueryOwnershipBySandboxIdsCallback, QueryOwnershipBySandboxIdsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
