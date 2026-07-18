using System;

namespace Epic.OnlineServices.RTCData;

public struct DataReceivedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public ArraySegment<byte> Data { get; set; }

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
