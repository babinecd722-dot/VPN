namespace Epic.OnlineServices.Connect;

internal static class OnUnlinkAccountCallbackInternalImplementation
{
	private static OnUnlinkAccountCallbackInternal s_Delegate;

	public static OnUnlinkAccountCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUnlinkAccountCallbackInternal))]
	public static void EntryPoint(ref UnlinkAccountCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UnlinkAccountCallbackInfoInternal, OnUnlinkAccountCallback, UnlinkAccountCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
