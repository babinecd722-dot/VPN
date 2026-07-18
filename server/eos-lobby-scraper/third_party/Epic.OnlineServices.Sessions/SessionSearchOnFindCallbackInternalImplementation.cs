namespace Epic.OnlineServices.Sessions;

internal static class SessionSearchOnFindCallbackInternalImplementation
{
	private static SessionSearchOnFindCallbackInternal s_Delegate;

	public static SessionSearchOnFindCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(SessionSearchOnFindCallbackInternal))]
	public static void EntryPoint(ref SessionSearchFindCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SessionSearchFindCallbackInfoInternal, SessionSearchOnFindCallback, SessionSearchFindCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
