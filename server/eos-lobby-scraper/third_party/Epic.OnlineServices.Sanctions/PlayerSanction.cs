namespace Epic.OnlineServices.Sanctions;

public struct PlayerSanction
{
	public long TimePlaced { get; set; }

	public Utf8String Action { get; set; }

	public long TimeExpires { get; set; }

	public Utf8String ReferenceId { get; set; }
}
