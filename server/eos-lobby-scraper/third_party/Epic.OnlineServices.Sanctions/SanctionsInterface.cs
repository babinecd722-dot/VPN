using System;

namespace Epic.OnlineServices.Sanctions;

public sealed class SanctionsInterface : Handle
{
	public const int COPYPLAYERSANCTIONBYINDEX_API_LATEST = 1;

	public const int CREATEPLAYERSANCTIONAPPEAL_API_LATEST = 1;

	public const int GETPLAYERSANCTIONCOUNT_API_LATEST = 1;

	public const int PLAYERSANCTION_API_LATEST = 2;

	public const int QUERYACTIVEPLAYERSANCTIONS_API_LATEST = 2;

	public SanctionsInterface()
	{
	}

	public SanctionsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyPlayerSanctionByIndex(ref CopyPlayerSanctionByIndexOptions options, out PlayerSanction? outSanction)
	{
		CopyPlayerSanctionByIndexOptionsInternal options2 = default(CopyPlayerSanctionByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outSanction2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sanctions_CopyPlayerSanctionByIndex(base.InnerHandle, ref options2, out outSanction2);
		Helper.Dispose(ref options2);
		Helper.Get<PlayerSanctionInternal, PlayerSanction>(outSanction2, out outSanction);
		if (outSanction2 != IntPtr.Zero)
		{
			Bindings.EOS_Sanctions_PlayerSanction_Release(outSanction2);
		}
		return result;
	}

	public void CreatePlayerSanctionAppeal(ref CreatePlayerSanctionAppealOptions options, object clientData, CreatePlayerSanctionAppealCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		CreatePlayerSanctionAppealOptionsInternal options2 = default(CreatePlayerSanctionAppealOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sanctions_CreatePlayerSanctionAppeal(base.InnerHandle, ref options2, clientDataPointer, CreatePlayerSanctionAppealCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public uint GetPlayerSanctionCount(ref GetPlayerSanctionCountOptions options)
	{
		GetPlayerSanctionCountOptionsInternal options2 = default(GetPlayerSanctionCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Sanctions_GetPlayerSanctionCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryActivePlayerSanctions(ref QueryActivePlayerSanctionsOptions options, object clientData, OnQueryActivePlayerSanctionsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryActivePlayerSanctionsOptionsInternal options2 = default(QueryActivePlayerSanctionsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sanctions_QueryActivePlayerSanctions(base.InnerHandle, ref options2, clientDataPointer, OnQueryActivePlayerSanctionsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
