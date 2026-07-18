using System;

namespace Epic.OnlineServices.P2P;

public sealed class P2PInterface : Handle
{
	public const int ACCEPTCONNECTION_API_LATEST = 1;

	public const int ADDNOTIFYINCOMINGPACKETQUEUEFULL_API_LATEST = 1;

	public const int ADDNOTIFYPEERCONNECTIONCLOSED_API_LATEST = 1;

	public const int ADDNOTIFYPEERCONNECTIONESTABLISHED_API_LATEST = 1;

	public const int ADDNOTIFYPEERCONNECTIONINTERRUPTED_API_LATEST = 1;

	public const int ADDNOTIFYPEERCONNECTIONREQUEST_API_LATEST = 1;

	public const int CLEARPACKETQUEUE_API_LATEST = 1;

	public const int CLOSECONNECTIONS_API_LATEST = 1;

	public const int CLOSECONNECTION_API_LATEST = 1;

	public const int GETNATTYPE_API_LATEST = 1;

	public const int GETNEXTRECEIVEDPACKETSIZE_API_LATEST = 2;

	public const int GETPACKETQUEUEINFO_API_LATEST = 1;

	public const int GETPORTRANGE_API_LATEST = 1;

	public const int GETRELAYCONTROL_API_LATEST = 1;

	public const int MAX_CONNECTIONS = 32;

	public const int MAX_PACKET_SIZE = 1170;

	public const int MAX_QUEUE_SIZE_UNLIMITED = 0;

	public const int QUERYNATTYPE_API_LATEST = 1;

	public const int RECEIVEPACKET_API_LATEST = 2;

	public const int SENDPACKET_API_LATEST = 3;

	public const int SETPACKETQUEUESIZE_API_LATEST = 1;

	public const int SETPORTRANGE_API_LATEST = 1;

	public const int SETRELAYCONTROL_API_LATEST = 1;

	public const int SOCKETID_API_LATEST = 1;

	public const int SOCKETID_SOCKETNAME_SIZE = 33;

	public Result ReceivePacket(ref ReceivePacketOptions options, ref ProductUserId outPeerId, ref SocketId outSocketId, out byte outChannel, ArraySegment<byte> outData, out uint outBytesWritten)
	{
		bool wasCacheValid = outSocketId.PrepareForUpdate();
		IntPtr value = Helper.AddPinnedBuffer(outSocketId.m_AllBytes);
		IntPtr value2 = Helper.AddPinnedBuffer(outData);
		ReceivePacketOptionsInternal options2 = new ReceivePacketOptionsInternal(ref options);
		try
		{
			IntPtr outPeerId2 = IntPtr.Zero;
			outChannel = 0;
			outBytesWritten = 0u;
			Result result = Bindings.EOS_P2P_ReceivePacket(base.InnerHandle, ref options2, out outPeerId2, value, out outChannel, value2, out outBytesWritten);
			if (outPeerId == null)
			{
				Helper.Get(outPeerId2, out outPeerId);
			}
			else if (outPeerId.InnerHandle != outPeerId2)
			{
				outPeerId.InnerHandle = outPeerId2;
			}
			outSocketId.CheckIfChanged(wasCacheValid);
			return result;
		}
		finally
		{
			Helper.Dispose(ref value);
			Helper.Dispose(ref value2);
			options2.Dispose();
		}
	}

	public P2PInterface()
	{
	}

	public P2PInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result AcceptConnection(ref AcceptConnectionOptions options)
	{
		AcceptConnectionOptionsInternal options2 = default(AcceptConnectionOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_AcceptConnection(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public ulong AddNotifyIncomingPacketQueueFull(ref AddNotifyIncomingPacketQueueFullOptions options, object clientData, OnIncomingPacketQueueFullCallback incomingPacketQueueFullHandler)
	{
		if (incomingPacketQueueFullHandler == null)
		{
			throw new ArgumentNullException("incomingPacketQueueFullHandler");
		}
		AddNotifyIncomingPacketQueueFullOptionsInternal options2 = default(AddNotifyIncomingPacketQueueFullOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, incomingPacketQueueFullHandler);
		ulong num = Bindings.EOS_P2P_AddNotifyIncomingPacketQueueFull(base.InnerHandle, ref options2, clientDataPointer, OnIncomingPacketQueueFullCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyPeerConnectionClosed(ref AddNotifyPeerConnectionClosedOptions options, object clientData, OnRemoteConnectionClosedCallback connectionClosedHandler)
	{
		if (connectionClosedHandler == null)
		{
			throw new ArgumentNullException("connectionClosedHandler");
		}
		AddNotifyPeerConnectionClosedOptionsInternal options2 = default(AddNotifyPeerConnectionClosedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, connectionClosedHandler);
		ulong num = Bindings.EOS_P2P_AddNotifyPeerConnectionClosed(base.InnerHandle, ref options2, clientDataPointer, OnRemoteConnectionClosedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyPeerConnectionEstablished(ref AddNotifyPeerConnectionEstablishedOptions options, object clientData, OnPeerConnectionEstablishedCallback connectionEstablishedHandler)
	{
		if (connectionEstablishedHandler == null)
		{
			throw new ArgumentNullException("connectionEstablishedHandler");
		}
		AddNotifyPeerConnectionEstablishedOptionsInternal options2 = default(AddNotifyPeerConnectionEstablishedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, connectionEstablishedHandler);
		ulong num = Bindings.EOS_P2P_AddNotifyPeerConnectionEstablished(base.InnerHandle, ref options2, clientDataPointer, OnPeerConnectionEstablishedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyPeerConnectionInterrupted(ref AddNotifyPeerConnectionInterruptedOptions options, object clientData, OnPeerConnectionInterruptedCallback connectionInterruptedHandler)
	{
		if (connectionInterruptedHandler == null)
		{
			throw new ArgumentNullException("connectionInterruptedHandler");
		}
		AddNotifyPeerConnectionInterruptedOptionsInternal options2 = default(AddNotifyPeerConnectionInterruptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, connectionInterruptedHandler);
		ulong num = Bindings.EOS_P2P_AddNotifyPeerConnectionInterrupted(base.InnerHandle, ref options2, clientDataPointer, OnPeerConnectionInterruptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyPeerConnectionRequest(ref AddNotifyPeerConnectionRequestOptions options, object clientData, OnIncomingConnectionRequestCallback connectionRequestHandler)
	{
		if (connectionRequestHandler == null)
		{
			throw new ArgumentNullException("connectionRequestHandler");
		}
		AddNotifyPeerConnectionRequestOptionsInternal options2 = default(AddNotifyPeerConnectionRequestOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, connectionRequestHandler);
		ulong num = Bindings.EOS_P2P_AddNotifyPeerConnectionRequest(base.InnerHandle, ref options2, clientDataPointer, OnIncomingConnectionRequestCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result ClearPacketQueue(ref ClearPacketQueueOptions options)
	{
		ClearPacketQueueOptionsInternal options2 = default(ClearPacketQueueOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_ClearPacketQueue(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result CloseConnection(ref CloseConnectionOptions options)
	{
		CloseConnectionOptionsInternal options2 = default(CloseConnectionOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_CloseConnection(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result CloseConnections(ref CloseConnectionsOptions options)
	{
		CloseConnectionsOptionsInternal options2 = default(CloseConnectionsOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_CloseConnections(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetNATType(ref GetNATTypeOptions options, out NATType outNATType)
	{
		GetNATTypeOptionsInternal options2 = default(GetNATTypeOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_GetNATType(base.InnerHandle, ref options2, out outNATType);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetNextReceivedPacketSize(ref GetNextReceivedPacketSizeOptions options, out uint outPacketSizeBytes)
	{
		GetNextReceivedPacketSizeOptionsInternal options2 = default(GetNextReceivedPacketSizeOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_GetNextReceivedPacketSize(base.InnerHandle, ref options2, out outPacketSizeBytes);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetPacketQueueInfo(ref GetPacketQueueInfoOptions options, out PacketQueueInfo outPacketQueueInfo)
	{
		GetPacketQueueInfoOptionsInternal options2 = default(GetPacketQueueInfoOptionsInternal);
		options2.Set(ref options);
		PacketQueueInfoInternal outPacketQueueInfo2 = default(PacketQueueInfoInternal);
		Result result = Bindings.EOS_P2P_GetPacketQueueInfo(base.InnerHandle, ref options2, out outPacketQueueInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<PacketQueueInfoInternal, PacketQueueInfo>(ref outPacketQueueInfo2, out outPacketQueueInfo);
		return result;
	}

	public Result GetPortRange(ref GetPortRangeOptions options, out ushort outPort, out ushort outNumAdditionalPortsToTry)
	{
		GetPortRangeOptionsInternal options2 = default(GetPortRangeOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_GetPortRange(base.InnerHandle, ref options2, out outPort, out outNumAdditionalPortsToTry);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetRelayControl(ref GetRelayControlOptions options, out RelayControl outRelayControl)
	{
		GetRelayControlOptionsInternal options2 = default(GetRelayControlOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_GetRelayControl(base.InnerHandle, ref options2, out outRelayControl);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryNATType(ref QueryNATTypeOptions options, object clientData, OnQueryNATTypeCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryNATTypeOptionsInternal options2 = default(QueryNATTypeOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_P2P_QueryNATType(base.InnerHandle, ref options2, clientDataPointer, OnQueryNATTypeCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyIncomingPacketQueueFull(ulong notificationId)
	{
		Bindings.EOS_P2P_RemoveNotifyIncomingPacketQueueFull(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyPeerConnectionClosed(ulong notificationId)
	{
		Bindings.EOS_P2P_RemoveNotifyPeerConnectionClosed(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyPeerConnectionEstablished(ulong notificationId)
	{
		Bindings.EOS_P2P_RemoveNotifyPeerConnectionEstablished(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyPeerConnectionInterrupted(ulong notificationId)
	{
		Bindings.EOS_P2P_RemoveNotifyPeerConnectionInterrupted(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyPeerConnectionRequest(ulong notificationId)
	{
		Bindings.EOS_P2P_RemoveNotifyPeerConnectionRequest(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public Result SendPacket(ref SendPacketOptions options)
	{
		SendPacketOptionsInternal options2 = default(SendPacketOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_SendPacket(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetPacketQueueSize(ref SetPacketQueueSizeOptions options)
	{
		SetPacketQueueSizeOptionsInternal options2 = default(SetPacketQueueSizeOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_SetPacketQueueSize(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetPortRange(ref SetPortRangeOptions options)
	{
		SetPortRangeOptionsInternal options2 = default(SetPortRangeOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_SetPortRange(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetRelayControl(ref SetRelayControlOptions options)
	{
		SetRelayControlOptionsInternal options2 = default(SetRelayControlOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_P2P_SetRelayControl(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}
}
