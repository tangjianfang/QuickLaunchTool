#pragma once

#include <string>
#include <windows.h>

namespace QuickLaunchTool {

struct AppInfo {
    std::wstring name;
    std::wstring fullPath;
    std::wstring args;        // launch arguments (e.g. PWA: --profile-directory=Default --app-id=xxx)
    FILETIME     lastModified = {};
    int          useCount     = 0;
    HICON        hIcon        = nullptr;
    bool         isSelected   = false;
};

} // namespace QuickLaunchTool
