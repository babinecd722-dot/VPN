namespace Epic.OnlineServices.Achievements;

public struct DefinitionV2
{
	public Utf8String AchievementId { get; set; }

	public Utf8String UnlockedDisplayName { get; set; }

	public Utf8String UnlockedDescription { get; set; }

	public Utf8String LockedDisplayName { get; set; }

	public Utf8String LockedDescription { get; set; }

	public Utf8String FlavorText { get; set; }

	public Utf8String UnlockedIconURL { get; set; }

	public Utf8String LockedIconURL { get; set; }

	public bool IsHidden { get; set; }

	public StatThresholds[] StatThresholds { get; set; }
}
