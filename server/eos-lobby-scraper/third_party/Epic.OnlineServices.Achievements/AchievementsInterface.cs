using System;

namespace Epic.OnlineServices.Achievements;

public sealed class AchievementsInterface : Handle
{
	public const int ACHIEVEMENT_UNLOCKTIME_UNDEFINED = -1;

	public const int ADDNOTIFYACHIEVEMENTSUNLOCKEDV2_API_LATEST = 2;

	public const int ADDNOTIFYACHIEVEMENTSUNLOCKED_API_LATEST = 1;

	public const int COPYACHIEVEMENTDEFINITIONV2BYACHIEVEMENTID_API_LATEST = 2;

	public const int COPYACHIEVEMENTDEFINITIONV2BYINDEX_API_LATEST = 2;

	public const int COPYDEFINITIONBYACHIEVEMENTID_API_LATEST = 1;

	public const int COPYDEFINITIONBYINDEX_API_LATEST = 1;

	public const int COPYDEFINITIONV2BYACHIEVEMENTID_API_LATEST = 2;

	public const int COPYDEFINITIONV2BYINDEX_API_LATEST = 2;

	public const int COPYPLAYERACHIEVEMENTBYACHIEVEMENTID_API_LATEST = 2;

	public const int COPYPLAYERACHIEVEMENTBYINDEX_API_LATEST = 2;

	public const int COPYUNLOCKEDACHIEVEMENTBYACHIEVEMENTID_API_LATEST = 1;

	public const int COPYUNLOCKEDACHIEVEMENTBYINDEX_API_LATEST = 1;

	public const int DEFINITIONV2_API_LATEST = 2;

	public const int DEFINITION_API_LATEST = 1;

	public const int GETACHIEVEMENTDEFINITIONCOUNT_API_LATEST = 1;

	public const int GETPLAYERACHIEVEMENTCOUNT_API_LATEST = 1;

	public const int GETUNLOCKEDACHIEVEMENTCOUNT_API_LATEST = 1;

	public const int PLAYERACHIEVEMENT_API_LATEST = 2;

	public const int PLAYERSTATINFO_API_LATEST = 1;

	public const int QUERYDEFINITIONS_API_LATEST = 3;

	public const int QUERYPLAYERACHIEVEMENTS_API_LATEST = 2;

	public const int STATTHRESHOLDS_API_LATEST = 1;

	public const int STATTHRESHOLD_API_LATEST = 1;

	public const int UNLOCKACHIEVEMENTS_API_LATEST = 1;

	public const int UNLOCKEDACHIEVEMENT_API_LATEST = 1;

	public AchievementsInterface()
	{
	}

	public AchievementsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyAchievementsUnlocked(ref AddNotifyAchievementsUnlockedOptions options, object clientData, OnAchievementsUnlockedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyAchievementsUnlockedOptionsInternal options2 = default(AddNotifyAchievementsUnlockedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Achievements_AddNotifyAchievementsUnlocked(base.InnerHandle, ref options2, clientDataPointer, OnAchievementsUnlockedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyAchievementsUnlockedV2(ref AddNotifyAchievementsUnlockedV2Options options, object clientData, OnAchievementsUnlockedCallbackV2 notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyAchievementsUnlockedV2OptionsInternal options2 = default(AddNotifyAchievementsUnlockedV2OptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Achievements_AddNotifyAchievementsUnlockedV2(base.InnerHandle, ref options2, clientDataPointer, OnAchievementsUnlockedCallbackV2InternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyAchievementDefinitionByAchievementId(ref CopyAchievementDefinitionByAchievementIdOptions options, out Definition? outDefinition)
	{
		CopyAchievementDefinitionByAchievementIdOptionsInternal options2 = default(CopyAchievementDefinitionByAchievementIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outDefinition2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyAchievementDefinitionByAchievementId(base.InnerHandle, ref options2, out outDefinition2);
		Helper.Dispose(ref options2);
		Helper.Get<DefinitionInternal, Definition>(outDefinition2, out outDefinition);
		if (outDefinition2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_Definition_Release(outDefinition2);
		}
		return result;
	}

	public Result CopyAchievementDefinitionByIndex(ref CopyAchievementDefinitionByIndexOptions options, out Definition? outDefinition)
	{
		CopyAchievementDefinitionByIndexOptionsInternal options2 = default(CopyAchievementDefinitionByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outDefinition2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyAchievementDefinitionByIndex(base.InnerHandle, ref options2, out outDefinition2);
		Helper.Dispose(ref options2);
		Helper.Get<DefinitionInternal, Definition>(outDefinition2, out outDefinition);
		if (outDefinition2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_Definition_Release(outDefinition2);
		}
		return result;
	}

	public Result CopyAchievementDefinitionV2ByAchievementId(ref CopyAchievementDefinitionV2ByAchievementIdOptions options, out DefinitionV2? outDefinition)
	{
		CopyAchievementDefinitionV2ByAchievementIdOptionsInternal options2 = default(CopyAchievementDefinitionV2ByAchievementIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outDefinition2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId(base.InnerHandle, ref options2, out outDefinition2);
		Helper.Dispose(ref options2);
		Helper.Get<DefinitionV2Internal, DefinitionV2>(outDefinition2, out outDefinition);
		if (outDefinition2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_DefinitionV2_Release(outDefinition2);
		}
		return result;
	}

	public Result CopyAchievementDefinitionV2ByIndex(ref CopyAchievementDefinitionV2ByIndexOptions options, out DefinitionV2? outDefinition)
	{
		CopyAchievementDefinitionV2ByIndexOptionsInternal options2 = default(CopyAchievementDefinitionV2ByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outDefinition2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyAchievementDefinitionV2ByIndex(base.InnerHandle, ref options2, out outDefinition2);
		Helper.Dispose(ref options2);
		Helper.Get<DefinitionV2Internal, DefinitionV2>(outDefinition2, out outDefinition);
		if (outDefinition2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_DefinitionV2_Release(outDefinition2);
		}
		return result;
	}

	public Result CopyPlayerAchievementByAchievementId(ref CopyPlayerAchievementByAchievementIdOptions options, out PlayerAchievement? outAchievement)
	{
		CopyPlayerAchievementByAchievementIdOptionsInternal options2 = default(CopyPlayerAchievementByAchievementIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outAchievement2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyPlayerAchievementByAchievementId(base.InnerHandle, ref options2, out outAchievement2);
		Helper.Dispose(ref options2);
		Helper.Get<PlayerAchievementInternal, PlayerAchievement>(outAchievement2, out outAchievement);
		if (outAchievement2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_PlayerAchievement_Release(outAchievement2);
		}
		return result;
	}

	public Result CopyPlayerAchievementByIndex(ref CopyPlayerAchievementByIndexOptions options, out PlayerAchievement? outAchievement)
	{
		CopyPlayerAchievementByIndexOptionsInternal options2 = default(CopyPlayerAchievementByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outAchievement2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyPlayerAchievementByIndex(base.InnerHandle, ref options2, out outAchievement2);
		Helper.Dispose(ref options2);
		Helper.Get<PlayerAchievementInternal, PlayerAchievement>(outAchievement2, out outAchievement);
		if (outAchievement2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_PlayerAchievement_Release(outAchievement2);
		}
		return result;
	}

	public Result CopyUnlockedAchievementByAchievementId(ref CopyUnlockedAchievementByAchievementIdOptions options, out UnlockedAchievement? outAchievement)
	{
		CopyUnlockedAchievementByAchievementIdOptionsInternal options2 = default(CopyUnlockedAchievementByAchievementIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outAchievement2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyUnlockedAchievementByAchievementId(base.InnerHandle, ref options2, out outAchievement2);
		Helper.Dispose(ref options2);
		Helper.Get<UnlockedAchievementInternal, UnlockedAchievement>(outAchievement2, out outAchievement);
		if (outAchievement2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_UnlockedAchievement_Release(outAchievement2);
		}
		return result;
	}

	public Result CopyUnlockedAchievementByIndex(ref CopyUnlockedAchievementByIndexOptions options, out UnlockedAchievement? outAchievement)
	{
		CopyUnlockedAchievementByIndexOptionsInternal options2 = default(CopyUnlockedAchievementByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outAchievement2 = IntPtr.Zero;
		Result result = Bindings.EOS_Achievements_CopyUnlockedAchievementByIndex(base.InnerHandle, ref options2, out outAchievement2);
		Helper.Dispose(ref options2);
		Helper.Get<UnlockedAchievementInternal, UnlockedAchievement>(outAchievement2, out outAchievement);
		if (outAchievement2 != IntPtr.Zero)
		{
			Bindings.EOS_Achievements_UnlockedAchievement_Release(outAchievement2);
		}
		return result;
	}

	public uint GetAchievementDefinitionCount(ref GetAchievementDefinitionCountOptions options)
	{
		GetAchievementDefinitionCountOptionsInternal options2 = default(GetAchievementDefinitionCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Achievements_GetAchievementDefinitionCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetPlayerAchievementCount(ref GetPlayerAchievementCountOptions options)
	{
		GetPlayerAchievementCountOptionsInternal options2 = default(GetPlayerAchievementCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Achievements_GetPlayerAchievementCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetUnlockedAchievementCount(ref GetUnlockedAchievementCountOptions options)
	{
		GetUnlockedAchievementCountOptionsInternal options2 = default(GetUnlockedAchievementCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Achievements_GetUnlockedAchievementCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryDefinitions(ref QueryDefinitionsOptions options, object clientData, OnQueryDefinitionsCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryDefinitionsOptionsInternal options2 = default(QueryDefinitionsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Achievements_QueryDefinitions(base.InnerHandle, ref options2, clientDataPointer, OnQueryDefinitionsCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryPlayerAchievements(ref QueryPlayerAchievementsOptions options, object clientData, OnQueryPlayerAchievementsCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryPlayerAchievementsOptionsInternal options2 = default(QueryPlayerAchievementsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Achievements_QueryPlayerAchievements(base.InnerHandle, ref options2, clientDataPointer, OnQueryPlayerAchievementsCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyAchievementsUnlocked(ulong inId)
	{
		Bindings.EOS_Achievements_RemoveNotifyAchievementsUnlocked(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void UnlockAchievements(ref UnlockAchievementsOptions options, object clientData, OnUnlockAchievementsCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UnlockAchievementsOptionsInternal options2 = default(UnlockAchievementsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Achievements_UnlockAchievements(base.InnerHandle, ref options2, clientDataPointer, OnUnlockAchievementsCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
