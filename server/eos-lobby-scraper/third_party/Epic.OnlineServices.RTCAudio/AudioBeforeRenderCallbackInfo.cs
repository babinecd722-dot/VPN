namespace Epic.OnlineServices.RTCAudio;

public struct AudioBeforeRenderCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public AudioBuffer? Buffer { get; set; }

	public ProductUserId ParticipantId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
