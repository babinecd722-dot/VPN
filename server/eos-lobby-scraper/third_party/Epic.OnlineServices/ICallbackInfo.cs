namespace Epic.OnlineServices;

internal interface ICallbackInfo
{
	object GetClientData();

	Result? GetResultCode();
}
