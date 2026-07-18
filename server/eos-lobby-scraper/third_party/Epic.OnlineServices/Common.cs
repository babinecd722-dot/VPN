using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices;

public sealed class Common
{
	public const string LIBRARY_NAME = "EOSSDK-Win64-Shipping.dll";

	public const CallingConvention LIBRARY_CALLING_CONVENTION = CallingConvention.Cdecl;

	public const int EPICACCOUNTID_MAX_LENGTH = 32;

	public const ulong INVALID_NOTIFICATIONID = 0uL;

	public static readonly Utf8String IPT_UNKNOWN;

	public const int OPT_EPIC = 100;

	public const int OPT_UNKNOWN = 0;

	public const int PRODUCTUSERID_MAX_LENGTH = 32;

	public const int WINDOWS_STEAM_OPT = 4000;

	public static Result ToString(ArraySegment<byte> byteArray, out Utf8String outBuffer)
	{
		IntPtr to = IntPtr.Zero;
		Helper.Set(byteArray, ref to, out var arrayLength);
		uint inOutBufferLength = 1024u;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_ByteArray_ToString(to, arrayLength, value, ref inOutBufferLength);
		Helper.Dispose(ref to);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public static Utf8String ToString(ArraySegment<byte> byteArray)
	{
		ToString(byteArray, out var outBuffer);
		return outBuffer;
	}

	public static bool IsOperationComplete(Result result)
	{
		Helper.Get(Bindings.EOS_EResult_IsOperationComplete(result), out bool to);
		return to;
	}

	public static Utf8String ToString(Result result)
	{
		Helper.Get(Bindings.EOS_EResult_ToString(result), out Utf8String to);
		return to;
	}
}
