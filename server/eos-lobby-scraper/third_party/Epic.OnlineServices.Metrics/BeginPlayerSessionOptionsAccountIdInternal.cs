using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Metrics;

[StructLayout(LayoutKind.Explicit)]
internal struct BeginPlayerSessionOptionsAccountIdInternal : ISettable<BeginPlayerSessionOptionsAccountId>, IDisposable
{
	[FieldOffset(0)]
	private IntPtr m_Epic;

	[FieldOffset(0)]
	private IntPtr m_External;

	public void Set(ref BeginPlayerSessionOptionsAccountId other)
	{
		Dispose();
		if (other.AccountIdType == MetricsAccountIdType.Epic)
		{
			Helper.Set((Handle)other.Epic, ref m_Epic);
		}
		if (other.AccountIdType == MetricsAccountIdType.External)
		{
			Helper.Set(other.External, ref m_External);
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Epic);
		Helper.Dispose(ref m_External);
	}
}
