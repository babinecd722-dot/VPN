namespace Epic.OnlineServices.Sanctions;

internal static class OnQueryActivePlayerSanctionsCallbackInternalImplementation
{
	private static OnQueryActivePlayerSanctionsCallbackInternal s_Delegate;

	public static OnQueryActivePlayerSanctionsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryActivePlayerSanctionsCallbackInternal))]
	public static void EntryPoint(ref QueryActivePlayerSanctionsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryActivePlayerSanctionsCallbackInfoInternal, OnQueryActivePlayerSanctionsCallback, QueryActivePlayerSanctionsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
