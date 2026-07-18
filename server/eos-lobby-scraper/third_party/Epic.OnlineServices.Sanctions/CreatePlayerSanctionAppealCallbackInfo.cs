namespace Epic.OnlineServices.Sanctions;

public struct CreatePlayerSanctionAppealCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public Utf8String ReferenceId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
