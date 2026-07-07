load("@build_bazel_rules_swift//swift:swift.bzl", "swift_library")

swift_library(
    name = "AorusGram",
    module_name = "AorusGram",
    srcs = glob([
        "Sources/**/*.swift",
    ]),
    # Telegram’s deployment target is below iOS 14; SwiftUI here uses .toolbar / ToolbarItem (14+).
    copts = ["-Xfrontend", "-disable-availability-checking"],
    deps = [
        "//submodules/Display:Display",
        "//submodules/AsyncDisplayKit:AsyncDisplayKit",
        "//submodules/SSignalKit/SwiftSignalKit:SwiftSignalKit",
        "//submodules/MediaResources:MediaResources",
        "//submodules/AnimatedStickerNode:AnimatedStickerNode",
    ],
    visibility = [
        "//visibility:public",
    ],
)
