using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.P2P;

internal struct SocketIdInternal : IGettable<SocketId>, ISettable<SocketId>, IDisposable
{
	private int m_ApiVersion;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
	private byte[] m_SocketName;

	public void Get(out SocketId other)
	{
		other = default(SocketId);
		Helper.Get(m_SocketName, out Utf8String to);
		other.SocketName = to;
	}

	public void Set(ref SocketId other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.SocketName, ref m_SocketName, 33);
	}

	public void Dispose()
	{
	}
}
