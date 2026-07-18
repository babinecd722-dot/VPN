namespace Epic.OnlineServices.Platform;

public struct InitializeThreadAffinity
{
	public ulong NetworkWork { get; set; }

	public ulong StorageIo { get; set; }

	public ulong WebSocketIo { get; set; }

	public ulong P2PIo { get; set; }

	public ulong HttpRequestIo { get; set; }

	public ulong RTCIo { get; set; }

	public ulong EmbeddedOverlayMainThread { get; set; }

	public ulong EmbeddedOverlayWorkerThreads { get; set; }

	public ulong CryptographyWork { get; set; }
}
