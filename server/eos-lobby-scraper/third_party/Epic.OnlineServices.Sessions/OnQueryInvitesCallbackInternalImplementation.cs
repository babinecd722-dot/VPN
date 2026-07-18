namespace Epic.OnlineServices.Sessions;

internal static class OnQueryInvitesCallbackInternalImplementation
{
	private static OnQueryInvitesCallbackInternal s_Delegate;

	public static OnQueryInvitesCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryInvitesCallbackInternal))]
	public static void EntryPoint(ref QueryInvitesCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryInvitesCallbackInfoInternal, OnQueryInvitesCallback, QueryInvitesCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
