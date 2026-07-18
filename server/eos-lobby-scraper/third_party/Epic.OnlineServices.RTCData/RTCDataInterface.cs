using System;

namespace Epic.OnlineServices.RTCData;

public sealed class RTCDataInterface : Handle
{
	public const int ADDNOTIFYDATARECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYPARTICIPANTUPDATED_API_LATEST = 1;

	public const int MAX_PACKET_SIZE = 1170;

	public const int SENDDATA_API_LATEST = 1;

	public const int UPDATERECEIVING_API_LATEST = 1;

	public const int UPDATESENDING_API_LATEST = 1;

	public RTCDataInterface()
	{
	}

	public RTCDataInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyDataReceived(ref AddNotifyDataReceivedOptions options, object clientData, OnDataReceivedCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyDataReceivedOptionsInternal options2 = default(AddNotifyDataReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCData_AddNotifyDataReceived(base.InnerHandle, ref options2, clientDataPointer, OnDataReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyParticipantUpdated(ref AddNotifyParticipantUpdatedOptions options, object clientData, OnParticipantUpdatedCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyParticipantUpdatedOptionsInternal options2 = default(AddNotifyParticipantUpdatedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCData_AddNotifyParticipantUpdated(base.InnerHandle, ref options2, clientDataPointer, OnParticipantUpdatedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public void RemoveNotifyDataReceived(ulong notificationId)
	{
		Bindings.EOS_RTCData_RemoveNotifyDataReceived(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyParticipantUpdated(ulong notificationId)
	{
		Bindings.EOS_RTCData_RemoveNotifyParticipantUpdated(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public Result SendData(ref SendDataOptions options)
	{
		SendDataOptionsInternal options2 = default(SendDataOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTCData_SendData(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void UpdateReceiving(ref UpdateReceivingOptions options, object clientData, OnUpdateReceivingCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateReceivingOptionsInternal options2 = default(UpdateReceivingOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCData_UpdateReceiving(base.InnerHandle, ref options2, clientDataPointer, OnUpdateReceivingCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateSending(ref UpdateSendingOptions options, object clientData, OnUpdateSendingCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateSendingOptionsInternal options2 = default(UpdateSendingOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCData_UpdateSending(base.InnerHandle, ref options2, clientDataPointer, OnUpdateSendingCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
