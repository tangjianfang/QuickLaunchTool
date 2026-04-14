#pragma once

#include <string>
#include <windows.h>

namespace QuickLaunchTool {

struct AppInfo {
    std::wstring name;
    std::wstring fullPath;
    FILETIME     lastModified = {};
    int          useCount     = 0;
    HICON        hIcon        = nullptr;
    bool         isSelected   = false;
};

} // namespace QuickLaunchTool
