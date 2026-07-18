using System;

namespace Epic.OnlineServices.Achievements;

internal struct DefinitionInternal : IGettable<Definition>
{
	private int m_ApiVersion;

	private IntPtr m_AchievementId;

	private IntPtr m_DisplayName;

	private IntPtr m_Description;

	private IntPtr m_LockedDisplayName;

	private IntPtr m_LockedDescription;

	private IntPtr m_HiddenDescription;

	private IntPtr m_CompletionDescription;

	private IntPtr m_UnlockedIconId;

	private IntPtr m_LockedIconId;

	private int m_IsHidden;

	private int m_StatThresholdsCount;

	private IntPtr m_StatThresholds;

	public void Get(out Definition other)
	{
		other = default(Definition);
		Helper.Get(m_AchievementId, out Utf8String to);
		other.AchievementId = to;
		Helper.Get(m_DisplayName, out Utf8String to2);
		other.DisplayName = to2;
		Helper.Get(m_Description, out Utf8String to3);
		other.Description = to3;
		Helper.Get(m_LockedDisplayName, out Utf8String to4);
		other.LockedDisplayName = to4;
		Helper.Get(m_LockedDescription, out Utf8String to5);
		other.LockedDescription = to5;
		Helper.Get(m_HiddenDescription, out Utf8String to6);
		other.HiddenDescription = to6;
		Helper.Get(m_CompletionDescription, out Utf8String to7);
		other.CompletionDescription = to7;
		Helper.Get(m_UnlockedIconId, out Utf8String to8);
		other.UnlockedIconId = to8;
		Helper.Get(m_LockedIconId, out Utf8String to9);
		other.LockedIconId = to9;
		Helper.Get(m_IsHidden, out bool to10);
		other.IsHidden = to10;
		Helper.Get<StatThresholdsInternal, StatThresholds>(m_StatThresholds, out var to11, m_StatThresholdsCount, isArrayItemAllocated: false);
		other.StatThresholds = to11;
	}
}
