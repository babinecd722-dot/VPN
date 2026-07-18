using System;

namespace Epic.OnlineServices.Platform;

internal struct InitializeThreadAffinityInternal : ISettable<InitializeThreadAffinity>, IDisposable
{
	private int m_ApiVersion;

	private ulong m_NetworkWork;

	private ulong m_StorageIo;

	private ulong m_WebSocketIo;

	private ulong m_P2PIo;

	private ulong m_HttpRequestIo;

	private ulong m_RTCIo;

	private ulong m_EmbeddedOverlayMainThread;

	private ulong m_EmbeddedOverlayWorkerThreads;

	private ulong m_CryptographyWork;

	public void Set(ref InitializeThreadAffinity other)
	{
		Dispose();
		m_ApiVersion = 4;
		m_NetworkWork = other.NetworkWork;
		m_StorageIo = other.StorageIo;
		m_WebSocketIo = other.WebSocketIo;
		m_P2PIo = other.P2PIo;
		m_HttpRequestIo = other.HttpRequestIo;
		m_RTCIo = other.RTCIo;
		m_EmbeddedOverlayMainThread = other.EmbeddedOverlayMainThread;
		m_EmbeddedOverlayWorkerThreads = other.EmbeddedOverlayWorkerThreads;
		m_CryptographyWork = other.CryptographyWork;
	}

	public void Dispose()
	{
	}
}
