using System.Runtime.InteropServices;

namespace Epic.OnlineServices.UI;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void OnScreenKeyboardRequestedCallbackInternal(ref OnScreenKeyboardRequestedCallbackInfoInternal data);
