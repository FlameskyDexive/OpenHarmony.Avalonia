#include "napi/native_api.h"
#include <cassert>
#include <cstdlib>
#include <dlfcn.h>
#include <node_api.h>
#include "napi/native_api.h"
#include <thread>
#include <hilog/log.h>
#include <native_drawing/drawing_text_typography.h>

extern "C" __attribute__((constructor)) void RegisterEntryModule(void) {
    // stock .NET 10 musl 运行时在鸿蒙沙箱下的已知缓解项（dlopen 前生效）
    setenv("DOTNET_GCHeapHardLimit", "2147483648", 0);   // 限制 GC 虚拟预留，避免大 VA mmap 失败
    setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", 0); // 先排除 ICU 版本问题
    setenv("DOTNET_EnableWriteXorExecute", "0", 0);      // W^X 兼容
    auto handle = dlopen("libavalonia.so", RTLD_NOW);
    assert(handle != nullptr);
    auto func = (void (*)())dlsym(handle, "RegisterEntryModule");
    assert(func != nullptr);
    func();
}