namespace Epic.OnlineServices.Connect;

public struct Credentials
{
	public Utf8String Token { get; set; }

	public ExternalCredentialType Type { get; set; }
}
