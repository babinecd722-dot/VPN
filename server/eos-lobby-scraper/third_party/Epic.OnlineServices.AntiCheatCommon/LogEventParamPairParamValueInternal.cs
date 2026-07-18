using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.AntiCheatCommon;

[StructLayout(LayoutKind.Explicit)]
internal struct LogEventParamPairParamValueInternal : ISettable<LogEventParamPairParamValue>, IDisposable
{
	[FieldOffset(0)]
	private IntPtr m_ClientHandle;

	[FieldOffset(0)]
	private IntPtr m_String;

	[FieldOffset(0)]
	private uint m_UInt32;

	[FieldOffset(0)]
	private int m_Int32;

	[FieldOffset(0)]
	private ulong m_UInt64;

	[FieldOffset(0)]
	private long m_Int64;

	[FieldOffset(0)]
	private Vec3fInternal m_Vec3f;

	[FieldOffset(0)]
	private QuatInternal m_Quat;

	[FieldOffset(0)]
	private float m_Float;

	public void Set(ref LogEventParamPairParamValue other)
	{
		Dispose();
		if (other.ParamValueType == AntiCheatCommonEventParamType.ClientHandle)
		{
			Helper.Set(other.ClientHandle, ref m_ClientHandle);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.String)
		{
			Helper.Set(other.String, ref m_String);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.UInt32)
		{
			Helper.Set<uint>(other.UInt32, ref m_UInt32);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.Int32)
		{
			Helper.Set<int>(other.Int32, ref m_Int32);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.UInt64)
		{
			Helper.Set<ulong>(other.UInt64, ref m_UInt64);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.Int64)
		{
			Helper.Set<long>(other.Int64, ref m_Int64);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.Vector3f)
		{
			Helper.Set(other.Vec3f, ref m_Vec3f);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.Quat)
		{
			Helper.Set(other.Quat, ref m_Quat);
		}
		if (other.ParamValueType == AntiCheatCommonEventParamType.Float)
		{
			Helper.Set<float>(other.Float, ref m_Float);
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_String);
		Helper.Dispose(ref m_Vec3f);
		Helper.Dispose(ref m_Quat);
	}
}
