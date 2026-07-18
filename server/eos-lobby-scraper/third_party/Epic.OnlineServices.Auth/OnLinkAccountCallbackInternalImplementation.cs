namespace Epic.OnlineServices.Auth;

internal static class OnLinkAccountCallbackInternalImplementation
{
	private static OnLinkAccountCallbackInternal s_Delegate;

	public static OnLinkAccountCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLinkAccountCallbackInternal))]
	public static void EntryPoint(ref LinkAccountCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<LinkAccountCallbackInfoInternal, OnLinkAccountCallback, LinkAccountCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
