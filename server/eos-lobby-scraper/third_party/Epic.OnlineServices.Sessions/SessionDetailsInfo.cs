namespace Epic.OnlineServices.Sessions;

public struct SessionDetailsInfo
{
	public Utf8String SessionId { get; set; }

	public Utf8String HostAddress { get; set; }

	public uint NumOpenPublicConnections { get; set; }

	public SessionDetailsSettings? Settings { get; set; }

	public ProductUserId OwnerUserId { get; set; }

	public Utf8String OwnerServerClientId { get; set; }
}
