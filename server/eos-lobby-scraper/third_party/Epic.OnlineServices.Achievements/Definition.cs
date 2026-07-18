namespace Epic.OnlineServices.Achievements;

public struct Definition
{
	public Utf8String AchievementId { get; set; }

	public Utf8String DisplayName { get; set; }

	public Utf8String Description { get; set; }

	public Utf8String LockedDisplayName { get; set; }

	public Utf8String LockedDescription { get; set; }

	public Utf8String HiddenDescription { get; set; }

	public Utf8String CompletionDescription { get; set; }

	public Utf8String UnlockedIconId { get; set; }

	public Utf8String LockedIconId { get; set; }

	public bool IsHidden { get; set; }

	public StatThresholds[] StatThresholds { get; set; }
}
