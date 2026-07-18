using System;

namespace Epic.OnlineServices.Stats;

public sealed class StatsInterface : Handle
{
	public const int COPYSTATBYINDEX_API_LATEST = 1;

	public const int COPYSTATBYNAME_API_LATEST = 1;

	public const int GETSTATCOUNT_API_LATEST = 1;

	public const int GETSTATSCOUNT_API_LATEST = 1;

	public const int INGESTDATA_API_LATEST = 1;

	public const int INGESTSTAT_API_LATEST = 3;

	public const int MAX_INGEST_STATS = 3000;

	public const int MAX_QUERY_STATS = 1000;

	public const int QUERYSTATS_API_LATEST = 3;

	public const int STAT_API_LATEST = 1;

	public const int TIME_UNDEFINED = -1;

	public StatsInterface()
	{
	}

	public StatsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyStatByIndex(ref CopyStatByIndexOptions options, out Stat? outStat)
	{
		CopyStatByIndexOptionsInternal options2 = default(CopyStatByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outStat2 = IntPtr.Zero;
		Result result = Bindings.EOS_Stats_CopyStatByIndex(base.InnerHandle, ref options2, out outStat2);
		Helper.Dispose(ref options2);
		Helper.Get<StatInternal, Stat>(outStat2, out outStat);
		if (outStat2 != IntPtr.Zero)
		{
			Bindings.EOS_Stats_Stat_Release(outStat2);
		}
		return result;
	}

	public Result CopyStatByName(ref CopyStatByNameOptions options, out Stat? outStat)
	{
		CopyStatByNameOptionsInternal options2 = default(CopyStatByNameOptionsInternal);
		options2.Set(ref options);
		IntPtr outStat2 = IntPtr.Zero;
		Result result = Bindings.EOS_Stats_CopyStatByName(base.InnerHandle, ref options2, out outStat2);
		Helper.Dispose(ref options2);
		Helper.Get<StatInternal, Stat>(outStat2, out outStat);
		if (outStat2 != IntPtr.Zero)
		{
			Bindings.EOS_Stats_Stat_Release(outStat2);
		}
		return result;
	}

	public uint GetStatsCount(ref GetStatCountOptions options)
	{
		GetStatCountOptionsInternal options2 = default(GetStatCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Stats_GetStatsCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void IngestStat(ref IngestStatOptions options, object clientData, OnIngestStatCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		IngestStatOptionsInternal options2 = default(IngestStatOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Stats_IngestStat(base.InnerHandle, ref options2, clientDataPointer, OnIngestStatCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryStats(ref QueryStatsOptions options, object clientData, OnQueryStatsCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryStatsOptionsInternal options2 = default(QueryStatsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Stats_QueryStats(base.InnerHandle, ref options2, clientDataPointer, OnQueryStatsCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
