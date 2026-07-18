using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Presence;

[StructLayout(LayoutKind.Explicit)]
internal struct PresenceModificationSetTemplateDataOptionsValueInternal : ISettable<PresenceModificationSetTemplateDataOptionsValue>, IDisposable
{
	[FieldOffset(0)]
	private int m_AsInt32;

	[FieldOffset(0)]
	private IntPtr m_AsStringId;

	public void Set(ref PresenceModificationSetTemplateDataOptionsValue other)
	{
		Dispose();
		if (other.ValueType == PresenceModificationTemplateType.Int)
		{
			Helper.Set<int>(other.AsInt32, ref m_AsInt32);
		}
		if (other.ValueType == PresenceModificationTemplateType.String)
		{
			Helper.Set(other.AsStringId, ref m_AsStringId);
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AsStringId);
	}
}
