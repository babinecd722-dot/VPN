using System;

namespace Epic.OnlineServices.Lobby;

public sealed class LobbySearch : Handle
{
	public LobbySearch()
	{
	}

	public LobbySearch(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopySearchResultByIndex(ref LobbySearchCopySearchResultByIndexOptions options, out LobbyDetails outLobbyDetailsHandle)
	{
		LobbySearchCopySearchResultByIndexOptionsInternal options2 = default(LobbySearchCopySearchResultByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbyDetailsHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_LobbySearch_CopySearchResultByIndex(base.InnerHandle, ref options2, out outLobbyDetailsHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outLobbyDetailsHandle2, out outLobbyDetailsHandle);
		return result;
	}

	public void Find(ref LobbySearchFindOptions options, object clientData, LobbySearchOnFindCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		LobbySearchFindOptionsInternal options2 = default(LobbySearchFindOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_LobbySearch_Find(base.InnerHandle, ref options2, clientDataPointer, LobbySearchOnFindCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public uint GetSearchResultCount(ref LobbySearchGetSearchResultCountOptions options)
	{
		LobbySearchGetSearchResultCountOptionsInternal options2 = default(LobbySearchGetSearchResultCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_LobbySearch_GetSearchResultCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void Release()
	{
		Bindings.EOS_LobbySearch_Release(base.InnerHandle);
	}

	public Result RemoveParameter(ref LobbySearchRemoveParameterOptions options)
	{
		LobbySearchRemoveParameterOptionsInternal options2 = default(LobbySearchRemoveParameterOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_LobbySearch_RemoveParameter(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetLobbyId(ref LobbySearchSetLobbyIdOptions options)
	{
		LobbySearchSetLobbyIdOptionsInternal options2 = default(LobbySearchSetLobbyIdOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_LobbySearch_SetLobbyId(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetMaxResults(ref LobbySearchSetMaxResultsOptions options)
	{
		LobbySearchSetMaxResultsOptionsInternal options2 = default(LobbySearchSetMaxResultsOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_LobbySearch_SetMaxResults(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetParameter(ref LobbySearchSetParameterOptions options)
	{
		LobbySearchSetParameterOptionsInternal options2 = default(LobbySearchSetParameterOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_LobbySearch_SetParameter(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetTargetUserId(ref LobbySearchSetTargetUserIdOptions options)
	{
		LobbySearchSetTargetUserIdOptionsInternal options2 = default(LobbySearchSetTargetUserIdOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_LobbySearch_SetTargetUserId(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}
}
