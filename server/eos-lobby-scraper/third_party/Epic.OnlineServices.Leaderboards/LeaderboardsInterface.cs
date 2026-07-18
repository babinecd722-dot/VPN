using System;

namespace Epic.OnlineServices.Leaderboards;

public sealed class LeaderboardsInterface : Handle
{
	public const int COPYLEADERBOARDDEFINITIONBYINDEX_API_LATEST = 1;

	public const int COPYLEADERBOARDDEFINITIONBYLEADERBOARDID_API_LATEST = 1;

	public const int COPYLEADERBOARDRECORDBYINDEX_API_LATEST = 2;

	public const int COPYLEADERBOARDRECORDBYUSERID_API_LATEST = 2;

	public const int COPYLEADERBOARDUSERSCOREBYINDEX_API_LATEST = 1;

	public const int COPYLEADERBOARDUSERSCOREBYUSERID_API_LATEST = 1;

	public const int DEFINITION_API_LATEST = 1;

	public const int GETLEADERBOARDDEFINITIONCOUNT_API_LATEST = 1;

	public const int GETLEADERBOARDRECORDCOUNT_API_LATEST = 1;

	public const int GETLEADERBOARDUSERSCORECOUNT_API_LATEST = 1;

	public const int LEADERBOARDRECORD_API_LATEST = 2;

	public const int LEADERBOARDUSERSCORE_API_LATEST = 1;

	public const int QUERYLEADERBOARDDEFINITIONS_API_LATEST = 2;

	public const int QUERYLEADERBOARDRANKS_API_LATEST = 2;

	public const int QUERYLEADERBOARDUSERSCORES_API_LATEST = 2;

	public const int TIME_UNDEFINED = -1;

	public const int USERSCORESQUERYSTATINFO_API_LATEST = 1;

	public LeaderboardsInterface()
	{
	}

	public LeaderboardsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyLeaderboardDefinitionByIndex(ref CopyLeaderboardDefinitionByIndexOptions options, out Definition? outLeaderboardDefinition)
	{
		CopyLeaderboardDefinitionByIndexOptionsInternal options2 = default(CopyLeaderboardDefinitionByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outLeaderboardDefinition2 = IntPtr.Zero;
		Result result = Bindings.EOS_Leaderboards_CopyLeaderboardDefinitionByIndex(base.InnerHandle, ref options2, out outLeaderboardDefinition2);
		Helper.Dispose(ref options2);
		Helper.Get<DefinitionInternal, Definition>(outLeaderboardDefinition2, out outLeaderboardDefinition);
		if (outLeaderboardDefinition2 != IntPtr.Zero)
		{
			Bindings.EOS_Leaderboards_Definition_Release(outLeaderboardDefinition2);
		}
		return result;
	}

	public Result CopyLeaderboardDefinitionByLeaderboardId(ref CopyLeaderboardDefinitionByLeaderboardIdOptions options, out Definition? outLeaderboardDefinition)
	{
		CopyLeaderboardDefinitionByLeaderboardIdOptionsInternal options2 = default(CopyLeaderboardDefinitionByLeaderboardIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outLeaderboardDefinition2 = IntPtr.Zero;
		Result result = Bindings.EOS_Leaderboards_CopyLeaderboardDefinitionByLeaderboardId(base.InnerHandle, ref options2, out outLeaderboardDefinition2);
		Helper.Dispose(ref options2);
		Helper.Get<DefinitionInternal, Definition>(outLeaderboardDefinition2, out outLeaderboardDefinition);
		if (outLeaderboardDefinition2 != IntPtr.Zero)
		{
			Bindings.EOS_Leaderboards_Definition_Release(outLeaderboardDefinition2);
		}
		return result;
	}

	public Result CopyLeaderboardRecordByIndex(ref CopyLeaderboardRecordByIndexOptions options, out LeaderboardRecord? outLeaderboardRecord)
	{
		CopyLeaderboardRecordByIndexOptionsInternal options2 = default(CopyLeaderboardRecordByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outLeaderboardRecord2 = IntPtr.Zero;
		Result result = Bindings.EOS_Leaderboards_CopyLeaderboardRecordByIndex(base.InnerHandle, ref options2, out outLeaderboardRecord2);
		Helper.Dispose(ref options2);
		Helper.Get<LeaderboardRecordInternal, LeaderboardRecord>(outLeaderboardRecord2, out outLeaderboardRecord);
		if (outLeaderboardRecord2 != IntPtr.Zero)
		{
			Bindings.EOS_Leaderboards_LeaderboardRecord_Release(outLeaderboardRecord2);
		}
		return result;
	}

	public Result CopyLeaderboardRecordByUserId(ref CopyLeaderboardRecordByUserIdOptions options, out LeaderboardRecord? outLeaderboardRecord)
	{
		CopyLeaderboardRecordByUserIdOptionsInternal options2 = default(CopyLeaderboardRecordByUserIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outLeaderboardRecord2 = IntPtr.Zero;
		Result result = Bindings.EOS_Leaderboards_CopyLeaderboardRecordByUserId(base.InnerHandle, ref options2, out outLeaderboardRecord2);
		Helper.Dispose(ref options2);
		Helper.Get<LeaderboardRecordInternal, LeaderboardRecord>(outLeaderboardRecord2, out outLeaderboardRecord);
		if (outLeaderboardRecord2 != IntPtr.Zero)
		{
			Bindings.EOS_Leaderboards_LeaderboardRecord_Release(outLeaderboardRecord2);
		}
		return result;
	}

	public Result CopyLeaderboardUserScoreByIndex(ref CopyLeaderboardUserScoreByIndexOptions options, out LeaderboardUserScore? outLeaderboardUserScore)
	{
		CopyLeaderboardUserScoreByIndexOptionsInternal options2 = default(CopyLeaderboardUserScoreByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outLeaderboardUserScore2 = IntPtr.Zero;
		Result result = Bindings.EOS_Leaderboards_CopyLeaderboardUserScoreByIndex(base.InnerHandle, ref options2, out outLeaderboardUserScore2);
		Helper.Dispose(ref options2);
		Helper.Get<LeaderboardUserScoreInternal, LeaderboardUserScore>(outLeaderboardUserScore2, out outLeaderboardUserScore);
		if (outLeaderboardUserScore2 != IntPtr.Zero)
		{
			Bindings.EOS_Leaderboards_LeaderboardUserScore_Release(outLeaderboardUserScore2);
		}
		return result;
	}

	public Result CopyLeaderboardUserScoreByUserId(ref CopyLeaderboardUserScoreByUserIdOptions options, out LeaderboardUserScore? outLeaderboardUserScore)
	{
		CopyLeaderboardUserScoreByUserIdOptionsInternal options2 = default(CopyLeaderboardUserScoreByUserIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outLeaderboardUserScore2 = IntPtr.Zero;
		Result result = Bindings.EOS_Leaderboards_CopyLeaderboardUserScoreByUserId(base.InnerHandle, ref options2, out outLeaderboardUserScore2);
		Helper.Dispose(ref options2);
		Helper.Get<LeaderboardUserScoreInternal, LeaderboardUserScore>(outLeaderboardUserScore2, out outLeaderboardUserScore);
		if (outLeaderboardUserScore2 != IntPtr.Zero)
		{
			Bindings.EOS_Leaderboards_LeaderboardUserScore_Release(outLeaderboardUserScore2);
		}
		return result;
	}

	public uint GetLeaderboardDefinitionCount(ref GetLeaderboardDefinitionCountOptions options)
	{
		GetLeaderboardDefinitionCountOptionsInternal options2 = default(GetLeaderboardDefinitionCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Leaderboards_GetLeaderboardDefinitionCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetLeaderboardRecordCount(ref GetLeaderboardRecordCountOptions options)
	{
		GetLeaderboardRecordCountOptionsInternal options2 = default(GetLeaderboardRecordCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Leaderboards_GetLeaderboardRecordCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetLeaderboardUserScoreCount(ref GetLeaderboardUserScoreCountOptions options)
	{
		GetLeaderboardUserScoreCountOptionsInternal options2 = default(GetLeaderboardUserScoreCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Leaderboards_GetLeaderboardUserScoreCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryLeaderboardDefinitions(ref QueryLeaderboardDefinitionsOptions options, object clientData, OnQueryLeaderboardDefinitionsCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryLeaderboardDefinitionsOptionsInternal options2 = default(QueryLeaderboardDefinitionsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Leaderboards_QueryLeaderboardDefinitions(base.InnerHandle, ref options2, clientDataPointer, OnQueryLeaderboardDefinitionsCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryLeaderboardRanks(ref QueryLeaderboardRanksOptions options, object clientData, OnQueryLeaderboardRanksCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryLeaderboardRanksOptionsInternal options2 = default(QueryLeaderboardRanksOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Leaderboards_QueryLeaderboardRanks(base.InnerHandle, ref options2, clientDataPointer, OnQueryLeaderboardRanksCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryLeaderboardUserScores(ref QueryLeaderboardUserScoresOptions options, object clientData, OnQueryLeaderboardUserScoresCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryLeaderboardUserScoresOptionsInternal options2 = default(QueryLeaderboardUserScoresOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Leaderboards_QueryLeaderboardUserScores(base.InnerHandle, ref options2, clientDataPointer, OnQueryLeaderboardUserScoresCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
