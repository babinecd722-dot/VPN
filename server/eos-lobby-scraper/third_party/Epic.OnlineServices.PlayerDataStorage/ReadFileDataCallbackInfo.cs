using System;

namespace Epic.OnlineServices.PlayerDataStorage;

public struct ReadFileDataCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String Filename { get; set; }

	public uint TotalFileSizeBytes { get; set; }

	public bool IsLastChunk { get; set; }

	public ArraySegment<byte> DataChunk { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
