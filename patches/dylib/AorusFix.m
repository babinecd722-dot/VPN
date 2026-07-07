// AorusFix.dylib — clean App Group container fix for sideloaded/re-signed AorusGram.
//
// Problem: the app + its extensions (NotificationService, NotificationContent, Share)
// bake the App Group identifier "group.com.aorusgram" at build time. When the IPA is
// re-signed with a *personal* certificate (ESign), that certificate is entitled to a
// DIFFERENT App Group (e.g. group.<random>.N). -[NSFileManager
// containerURLForSecurityApplicationGroupIdentifier:] then returns nil for the baked
// identifier, so the app and the NotificationService extension no longer share a
// container -> push payloads cannot be decrypted and notifications silently break.
//
// Fix: swizzle containerURLForSecurityApplicationGroupIdentifier:. If the requested
// group resolves to nil, fall back to whatever App Group THIS binary is actually
// entitled to (read from its own code-signature entitlements at runtime). Because the
// app and every extension are all injected with this dylib and are all signed with the
// same certificate, they all fall back to the SAME container -> sharing is restored,
// regardless of which personal certificate was used to re-sign.
//
// This is the same mechanism as TeleDark's TelegramNotificationFix.dylib, but clean:
// no ads, no popups, no third-party dependencies — only the container redirect.

#import <Foundation/Foundation.h>
#import <Security/Security.h>
#import <objc/runtime.h>

// SecTask entitlement APIs are available on iOS but not in the public headers.
extern SecTaskRef SecTaskCreateFromSelf(CFAllocatorRef allocator);
extern CFTypeRef SecTaskCopyValueForEntitlement(SecTaskRef task, CFStringRef entitlement, CFErrorRef *error);

// The first App Group this process is actually entitled to (or nil), computed once.
static NSString *AorusFirstEntitledAppGroup(void) {
    static NSString *cached = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        SecTaskRef task = SecTaskCreateFromSelf(kCFAllocatorDefault);
        if (task) {
            CFTypeRef value = SecTaskCopyValueForEntitlement(task, CFSTR("com.apple.security.application-groups"), NULL);
            if (value) {
                if (CFGetTypeID(value) == CFArrayGetTypeID()) {
                    NSArray *groups = (__bridge NSArray *)value;
                    if (groups.count > 0 && [groups.firstObject isKindOfClass:[NSString class]]) {
                        cached = [(NSString *)groups.firstObject copy];
                    }
                }
                CFRelease(value);
            }
            CFRelease(task);
        }
    });
    return cached;
}

@implementation NSFileManager (AorusAppGroupFix)

+ (void)load {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        SEL origSel = @selector(containerURLForSecurityApplicationGroupIdentifier:);
        SEL newSel = @selector(aorus_containerURLForSecurityApplicationGroupIdentifier:);
        Method origMethod = class_getInstanceMethod(self, origSel);
        Method newMethod = class_getInstanceMethod(self, newSel);
        if (origMethod && newMethod) {
            method_exchangeImplementations(origMethod, newMethod);
        }
    });
}

// After the swap, calling aorus_... invokes the ORIGINAL implementation.
- (NSURL *)aorus_containerURLForSecurityApplicationGroupIdentifier:(NSString *)groupIdentifier {
    NSURL *url = [self aorus_containerURLForSecurityApplicationGroupIdentifier:groupIdentifier];
    if (url != nil) {
        return url;
    }
    NSString *entitled = AorusFirstEntitledAppGroup();
    if (entitled != nil && ![entitled isEqualToString:groupIdentifier]) {
        NSURL *fallback = [self aorus_containerURLForSecurityApplicationGroupIdentifier:entitled];
        if (fallback != nil) {
            return fallback;
        }
    }
    return url;
}

@end
