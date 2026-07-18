namespace Epic.OnlineServices.CustomInvites;

internal static class OnCustomInviteRejectedCallbackInternalImplementation
{
	private static OnCustomInviteRejectedCallbackInternal s_Delegate;

	public static OnCustomInviteRejectedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnCustomInviteRejectedCallbackInternal))]
	public static void EntryPoint(ref CustomInviteRejectedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<CustomInviteRejectedCallbackInfoInternal, OnCustomInviteRejectedCallback, CustomInviteRejectedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
