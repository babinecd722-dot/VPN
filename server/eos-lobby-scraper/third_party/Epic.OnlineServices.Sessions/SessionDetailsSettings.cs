namespace Epic.OnlineServices.Sessions;

public struct SessionDetailsSettings
{
	public Utf8String BucketId { get; set; }

	public uint NumPublicConnections { get; set; }

	public bool AllowJoinInProgress { get; set; }

	public OnlineSessionPermissionLevel PermissionLevel { get; set; }

	public bool InvitesAllowed { get; set; }

	public bool SanctionsEnabled { get; set; }

	public uint[] AllowedPlatformIds { get; set; }
}
