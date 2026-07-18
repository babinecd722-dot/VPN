namespace Epic.OnlineServices.RTCAdmin;

public struct QueryJoinRoomTokenCompleteCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public Utf8String RoomName { get; set; }

	public Utf8String ClientBaseUrl { get; set; }

	public uint QueryId { get; set; }

	public uint TokenCount { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
