using System;

namespace Epic.OnlineServices.Sessions;

public sealed class SessionSearch : Handle
{
	public SessionSearch()
	{
	}

	public SessionSearch(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopySearchResultByIndex(ref SessionSearchCopySearchResultByIndexOptions options, out SessionDetails outSessionHandle)
	{
		SessionSearchCopySearchResultByIndexOptionsInternal options2 = default(SessionSearchCopySearchResultByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_SessionSearch_CopySearchResultByIndex(base.InnerHandle, ref options2, out outSessionHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionHandle2, out outSessionHandle);
		return result;
	}

	public void Find(ref SessionSearchFindOptions options, object clientData, SessionSearchOnFindCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SessionSearchFindOptionsInternal options2 = default(SessionSearchFindOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_SessionSearch_Find(base.InnerHandle, ref options2, clientDataPointer, SessionSearchOnFindCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public uint GetSearchResultCount(ref SessionSearchGetSearchResultCountOptions options)
	{
		SessionSearchGetSearchResultCountOptionsInternal options2 = default(SessionSearchGetSearchResultCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_SessionSearch_GetSearchResultCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void Release()
	{
		Bindings.EOS_SessionSearch_Release(base.InnerHandle);
	}

	public Result RemoveParameter(ref SessionSearchRemoveParameterOptions options)
	{
		SessionSearchRemoveParameterOptionsInternal options2 = default(SessionSearchRemoveParameterOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_SessionSearch_RemoveParameter(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetMaxResults(ref SessionSearchSetMaxResultsOptions options)
	{
		SessionSearchSetMaxResultsOptionsInternal options2 = default(SessionSearchSetMaxResultsOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_SessionSearch_SetMaxResults(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetParameter(ref SessionSearchSetParameterOptions options)
	{
		SessionSearchSetParameterOptionsInternal options2 = default(SessionSearchSetParameterOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_SessionSearch_SetParameter(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetSessionId(ref SessionSearchSetSessionIdOptions options)
	{
		SessionSearchSetSessionIdOptionsInternal options2 = default(SessionSearchSetSessionIdOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_SessionSearch_SetSessionId(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetTargetUserId(ref SessionSearchSetTargetUserIdOptions options)
	{
		SessionSearchSetTargetUserIdOptionsInternal options2 = default(SessionSearchSetTargetUserIdOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_SessionSearch_SetTargetUserId(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}
}
