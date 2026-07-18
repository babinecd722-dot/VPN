namespace Epic.OnlineServices.CustomInvites;

internal static class OnSendCustomInviteCallbackInternalImplementation
{
	private static OnSendCustomInviteCallbackInternal s_Delegate;

	public static OnSendCustomInviteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSendCustomInviteCallbackInternal))]
	public static void EntryPoint(ref SendCustomInviteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SendCustomInviteCallbackInfoInternal, OnSendCustomInviteCallback, SendCustomInviteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
