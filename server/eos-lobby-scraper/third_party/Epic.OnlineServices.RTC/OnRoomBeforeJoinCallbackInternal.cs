using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTC;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnRoomBeforeJoinCallbackInternal(ref RoomBeforeJoinCallbackInfoInternal data);
