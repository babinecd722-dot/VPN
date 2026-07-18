namespace Epic.OnlineServices.Ecom;

internal static class OnRedeemEntitlementsCallbackInternalImplementation
{
	private static OnRedeemEntitlementsCallbackInternal s_Delegate;

	public static OnRedeemEntitlementsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRedeemEntitlementsCallbackInternal))]
	public static void EntryPoint(ref RedeemEntitlementsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<RedeemEntitlementsCallbackInfoInternal, OnRedeemEntitlementsCallback, RedeemEntitlementsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
