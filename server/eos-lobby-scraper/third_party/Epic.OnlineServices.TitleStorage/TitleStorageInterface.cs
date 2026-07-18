using System;

namespace Epic.OnlineServices.TitleStorage;

public sealed class TitleStorageInterface : Handle
{
	public const int COPYFILEMETADATAATINDEXOPTIONS_API_LATEST = 1;

	public const int COPYFILEMETADATAATINDEX_API_LATEST = 1;

	public const int COPYFILEMETADATABYFILENAMEOPTIONS_API_LATEST = 1;

	public const int COPYFILEMETADATABYFILENAME_API_LATEST = 1;

	public const int DELETECACHEOPTIONS_API_LATEST = 1;

	public const int DELETECACHE_API_LATEST = 1;

	public const int FILEMETADATA_API_LATEST = 2;

	public const int FILENAME_MAX_LENGTH_BYTES = 64;

	public const int GETFILEMETADATACOUNTOPTIONS_API_LATEST = 1;

	public const int GETFILEMETADATACOUNT_API_LATEST = 1;

	public const int QUERYFILELISTOPTIONS_API_LATEST = 1;

	public const int QUERYFILELIST_API_LATEST = 1;

	public const int QUERYFILEOPTIONS_API_LATEST = 1;

	public const int QUERYFILE_API_LATEST = 1;

	public const int READFILEOPTIONS_API_LATEST = 2;

	public const int READFILE_API_LATEST = 2;

	public TitleStorageInterface()
	{
	}

	public TitleStorageInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyFileMetadataAtIndex(ref CopyFileMetadataAtIndexOptions options, out FileMetadata? outMetadata)
	{
		CopyFileMetadataAtIndexOptionsInternal options2 = default(CopyFileMetadataAtIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outMetadata2 = IntPtr.Zero;
		Result result = Bindings.EOS_TitleStorage_CopyFileMetadataAtIndex(base.InnerHandle, ref options2, out outMetadata2);
		Helper.Dispose(ref options2);
		Helper.Get<FileMetadataInternal, FileMetadata>(outMetadata2, out outMetadata);
		if (outMetadata2 != IntPtr.Zero)
		{
			Bindings.EOS_TitleStorage_FileMetadata_Release(outMetadata2);
		}
		return result;
	}

	public Result CopyFileMetadataByFilename(ref CopyFileMetadataByFilenameOptions options, out FileMetadata? outMetadata)
	{
		CopyFileMetadataByFilenameOptionsInternal options2 = default(CopyFileMetadataByFilenameOptionsInternal);
		options2.Set(ref options);
		IntPtr outMetadata2 = IntPtr.Zero;
		Result result = Bindings.EOS_TitleStorage_CopyFileMetadataByFilename(base.InnerHandle, ref options2, out outMetadata2);
		Helper.Dispose(ref options2);
		Helper.Get<FileMetadataInternal, FileMetadata>(outMetadata2, out outMetadata);
		if (outMetadata2 != IntPtr.Zero)
		{
			Bindings.EOS_TitleStorage_FileMetadata_Release(outMetadata2);
		}
		return result;
	}

	public Result DeleteCache(ref DeleteCacheOptions options, object clientData, OnDeleteCacheCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		DeleteCacheOptionsInternal options2 = default(DeleteCacheOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Result result = Bindings.EOS_TitleStorage_DeleteCache(base.InnerHandle, ref options2, clientDataPointer, OnDeleteCacheCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetFileMetadataCount(ref GetFileMetadataCountOptions options)
	{
		GetFileMetadataCountOptionsInternal options2 = default(GetFileMetadataCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_TitleStorage_GetFileMetadataCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryFile(ref QueryFileOptions options, object clientData, OnQueryFileCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		QueryFileOptionsInternal options2 = default(QueryFileOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Bindings.EOS_TitleStorage_QueryFile(base.InnerHandle, ref options2, clientDataPointer, OnQueryFileCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryFileList(ref QueryFileListOptions options, object clientData, OnQueryFileListCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		QueryFileListOptionsInternal options2 = default(QueryFileListOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Bindings.EOS_TitleStorage_QueryFileList(base.InnerHandle, ref options2, clientDataPointer, OnQueryFileListCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public TitleStorageFileTransferRequest ReadFile(ref ReadFileOptions options, object clientData, OnReadFileCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		ReadFileOptionsInternal options2 = default(ReadFileOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Helper.AddCallback(clientDataPointer, options.ReadFileDataCallback);
		Helper.AddCallback(clientDataPointer, options.FileTransferProgressCallback);
		IntPtr intPtr = Bindings.EOS_TitleStorage_ReadFile(base.InnerHandle, ref options2, clientDataPointer, OnReadFileCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.Get(intPtr, out TitleStorageFileTransferRequest to);
		return to;
	}
}
