namespace Epic.OnlineServices.Lobby;

internal static class OnPromoteMemberCallbackInternalImplementation
{
	private static OnPromoteMemberCallbackInternal s_Delegate;

	public static OnPromoteMemberCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnPromoteMemberCallbackInternal))]
	public static void EntryPoint(ref PromoteMemberCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<PromoteMemberCallbackInfoInternal, OnPromoteMemberCallback, PromoteMemberCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
