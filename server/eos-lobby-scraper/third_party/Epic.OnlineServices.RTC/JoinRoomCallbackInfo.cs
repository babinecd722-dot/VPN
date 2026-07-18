namespace Epic.OnlineServices.RTC;

public struct JoinRoomCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public Option[] RoomOptions { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
