namespace Epic.OnlineServices.Sessions;

public struct UpdateSessionCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public Utf8String SessionName { get; set; }

	public Utf8String SessionId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
