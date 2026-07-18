namespace Epic.OnlineServices.Sanctions;

internal static class CreatePlayerSanctionAppealCallbackInternalImplementation
{
	private static CreatePlayerSanctionAppealCallbackInternal s_Delegate;

	public static CreatePlayerSanctionAppealCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(CreatePlayerSanctionAppealCallbackInternal))]
	public static void EntryPoint(ref CreatePlayerSanctionAppealCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<CreatePlayerSanctionAppealCallbackInfoInternal, CreatePlayerSanctionAppealCallback, CreatePlayerSanctionAppealCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
