using System;

namespace Epic.OnlineServices.ProgressionSnapshot;

public sealed class ProgressionSnapshotInterface : Handle
{
	public const int ADDPROGRESSION_API_LATEST = 1;

	public const int BEGINSNAPSHOT_API_LATEST = 1;

	public const int DELETESNAPSHOT_API_LATEST = 1;

	public const int ENDSNAPSHOT_API_LATEST = 1;

	public const int INVALID_PROGRESSIONSNAPSHOTID = 0;

	public const int SUBMITSNAPSHOT_API_LATEST = 1;

	public ProgressionSnapshotInterface()
	{
	}

	public ProgressionSnapshotInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result AddProgression(ref AddProgressionOptions options)
	{
		AddProgressionOptionsInternal options2 = default(AddProgressionOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_ProgressionSnapshot_AddProgression(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result BeginSnapshot(ref BeginSnapshotOptions options, out uint outSnapshotId)
	{
		BeginSnapshotOptionsInternal options2 = default(BeginSnapshotOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_ProgressionSnapshot_BeginSnapshot(base.InnerHandle, ref options2, out outSnapshotId);
		Helper.Dispose(ref options2);
		return result;
	}

	public void DeleteSnapshot(ref DeleteSnapshotOptions options, object clientData, OnDeleteSnapshotCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		DeleteSnapshotOptionsInternal options2 = default(DeleteSnapshotOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_ProgressionSnapshot_DeleteSnapshot(base.InnerHandle, ref options2, clientDataPointer, OnDeleteSnapshotCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result EndSnapshot(ref EndSnapshotOptions options)
	{
		EndSnapshotOptionsInternal options2 = default(EndSnapshotOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_ProgressionSnapshot_EndSnapshot(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void SubmitSnapshot(ref SubmitSnapshotOptions options, object clientData, OnSubmitSnapshotCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SubmitSnapshotOptionsInternal options2 = default(SubmitSnapshotOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_ProgressionSnapshot_SubmitSnapshot(base.InnerHandle, ref options2, clientDataPointer, OnSubmitSnapshotCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
