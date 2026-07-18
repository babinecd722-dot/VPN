using System;

namespace Epic.OnlineServices.Auth;

public struct Credentials
{
	public Utf8String Id { get; set; }

	public Utf8String Token { get; set; }

	public LoginCredentialType Type { get; set; }

	public IntPtr SystemAuthCredentialsOptions { get; set; }

	public ExternalCredentialType ExternalType { get; set; }
}
