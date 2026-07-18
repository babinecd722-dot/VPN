namespace Epic.OnlineServices.RTC;

public struct ParticipantStatusChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public ProductUserId ParticipantId { get; set; }

	public RTCParticipantStatus ParticipantStatus { get; set; }

	public ParticipantMetadata[] ParticipantMetadata { get; set; }

	public bool ParticipantInBlocklist { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
