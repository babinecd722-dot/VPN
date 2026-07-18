namespace Epic.OnlineServices.RTCAdmin;

public struct UserToken
{
	public ProductUserId ProductUserId { get; set; }

	public Utf8String Token { get; set; }
}
