using System;

namespace Epic.OnlineServices.Achievements;

internal struct DefinitionV2Internal : IGettable<DefinitionV2>
{
	private int m_ApiVersion;

	private IntPtr m_AchievementId;

	private IntPtr m_UnlockedDisplayName;

	private IntPtr m_UnlockedDescription;

	private IntPtr m_LockedDisplayName;

	private IntPtr m_LockedDescription;

	private IntPtr m_FlavorText;

	private IntPtr m_UnlockedIconURL;

	private IntPtr m_LockedIconURL;

	private int m_IsHidden;

	private uint m_StatThresholdsCount;

	private IntPtr m_StatThresholds;

	public void Get(out DefinitionV2 other)
	{
		other = default(DefinitionV2);
		Helper.Get(m_AchievementId, out Utf8String to);
		other.AchievementId = to;
		Helper.Get(m_UnlockedDisplayName, out Utf8String to2);
		other.UnlockedDisplayName = to2;
		Helper.Get(m_UnlockedDescription, out Utf8String to3);
		other.UnlockedDescription = to3;
		Helper.Get(m_LockedDisplayName, out Utf8String to4);
		other.LockedDisplayName = to4;
		Helper.Get(m_LockedDescription, out Utf8String to5);
		other.LockedDescription = to5;
		Helper.Get(m_FlavorText, out Utf8String to6);
		other.FlavorText = to6;
		Helper.Get(m_UnlockedIconURL, out Utf8String to7);
		other.UnlockedIconURL = to7;
		Helper.Get(m_LockedIconURL, out Utf8String to8);
		other.LockedIconURL = to8;
		Helper.Get(m_IsHidden, out bool to9);
		other.IsHidden = to9;
		Helper.Get<StatThresholdsInternal, StatThresholds>(m_StatThresholds, out var to10, m_StatThresholdsCount, isArrayItemAllocated: false);
		other.StatThresholds = to10;
	}
}
