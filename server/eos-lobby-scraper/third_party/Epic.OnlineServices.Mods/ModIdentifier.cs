namespace Epic.OnlineServices.Mods;

public struct ModIdentifier
{
	public Utf8String NamespaceId { get; set; }

	public Utf8String ItemId { get; set; }

	public Utf8String ArtifactId { get; set; }

	public Utf8String Title { get; set; }

	public Utf8String Version { get; set; }
}
