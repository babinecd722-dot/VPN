namespace Epic.OnlineServices.Presence;

public struct Info
{
	public Status Status { get; set; }

	public EpicAccountId UserId { get; set; }

	public Utf8String ProductId { get; set; }

	public Utf8String ProductVersion { get; set; }

	public Utf8String Platform { get; set; }

	public Utf8String RichText { get; set; }

	public DataRecord[] Records { get; set; }

	public Utf8String ProductName { get; set; }

	public Utf8String IntegratedPlatform { get; set; }
}
