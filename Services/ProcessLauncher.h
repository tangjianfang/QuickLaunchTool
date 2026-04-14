#pragma once

#include <string>
#include <windows.h>
#include <shellapi.h>
#include <shlwapi.h>

#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "shlwapi.lib")

namespace QuickLaunchTool {

class ProcessLauncher {
public:
    static bool Launch(const std::wstring& path, const std::wstring& args = L"") {
        HINSTANCE r = ShellExecuteW(nullptr, L"open", path.c_str(),
                                    args.empty() ? nullptr : args.c_str(),
                                    nullptr, SW_SHOWNORMAL);
        return reinterpret_cast<INT_PTR>(r) > 32;
    }

    static bool LaunchAsAdmin(const std::wstring& path, const std::wstring& args = L"") {
        SHELLEXECUTEINFOW sei = {};
        sei.cbSize       = sizeof(sei);
        sei.lpVerb       = L"runas";
        sei.lpFile       = path.c_str();
        sei.lpParameters = args.empty() ? nullptr : args.c_str();
        sei.nShow        = SW_SHOWNORMAL;
        return ShellExecuteExW(&sei) != FALSE;
    }

    static bool OpenLocation(const std::wstring& path) {
        std::wstring args = L"/select,\"" + path + L"\"";
        HINSTANCE r = ShellExecuteW(nullptr, L"open", L"explorer.exe",
                                    args.c_str(), nullptr, SW_SHOWNORMAL);
        return reinterpret_cast<INT_PTR>(r) > 32;
    }

    // Extract just the filename
    static std::wstring FileName(const std::wstring& path) {
        size_t p = path.find_last_of(L"\\/");
        return p != std::wstring::npos ? path.substr(p + 1) : path;
    }

    // Strip extension
    static std::wstring BaseName(const std::wstring& path) {
        std::wstring fn = FileName(path);
        size_t dot = fn.find_last_of(L'.');
        return dot != std::wstring::npos ? fn.substr(0, dot) : fn;
    }
};

} // namespace QuickLaunchTool
