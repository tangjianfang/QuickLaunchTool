#pragma once

#include <string>
#include <vector>
#include <algorithm>
#include <windows.h>
#include <shlobj.h>
#include <objbase.h>
#include <shobjidl.h>

#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "shell32.lib")

namespace QuickLaunchTool {

// Full information resolved from a .lnk shortcut.
struct LnkInfo {
    std::wstring path;  // target executable path
    std::wstring args;  // launch arguments (e.g. PWA --app-id=xxx)
    std::wstring name;  // display name derived from the .lnk filename
};

class FileScanner {
public:
    // Recursively scan a folder for .exe / .bat / .cmd files.
    static std::vector<std::wstring> ScanFolder(const std::wstring& folder, bool recursive) {
        std::vector<std::wstring> results;
        ScanDir(folder, recursive, results);
        return results;
    }

    // Return full info (path + args + name) for pinned apps.
    // Scans two locations:
    //   1. Taskbar pinned folder  (%APPDATA%\...\Quick Launch\User Pinned\TaskBar)
    //   2. Start Menu Programs subdirectories whose shortcuts target chrome_proxy.exe
    //      (the directory Chrome creates per-locale, e.g. "Chrome Apps" / "Chrome 应用")
    static std::vector<LnkInfo> GetTaskbarPinnedApps() {
        std::vector<LnkInfo> results;

        wchar_t buf[MAX_PATH] = {};
        if (FAILED(SHGetFolderPathW(nullptr, CSIDL_APPDATA, nullptr, 0, buf)))
            return results;

        // ── 1. Taskbar pinned folder ─────────────────────────────────────
        std::wstring taskbarDir = std::wstring(buf)
            + L"\\Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar";
        ScanLnkDir(taskbarDir, results, nullptr);

        // ── 2. Chrome PWAs from Start Menu Programs subdirs ──────────────
        wchar_t progBuf[MAX_PATH] = {};
        if (SUCCEEDED(SHGetFolderPathW(nullptr, CSIDL_PROGRAMS, nullptr, 0, progBuf))) {
            // Enumerate one level of subdirectories (e.g. "Chrome Apps", "Chrome 应用")
            WIN32_FIND_DATAW fd;
            HANDLE hFind = FindFirstFileW((std::wstring(progBuf) + L"\\*").c_str(), &fd);
            if (hFind != INVALID_HANDLE_VALUE) {
                do {
                    if (!(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)) continue;
                    if (wcscmp(fd.cFileName, L".") == 0)  continue;
                    if (wcscmp(fd.cFileName, L"..") == 0) continue;
                    std::wstring subDir = std::wstring(progBuf) + L"\\" + fd.cFileName;
                    // Only collect shortcuts that target chrome_proxy.exe with --app-id
                    ScanLnkDir(subDir, results, L"chrome_proxy.exe");
                } while (FindNextFileW(hFind, &fd));
                FindClose(hFind);
            }
        }

        return results;
    }

    // Resolve a .lnk shortcut: returns target path, arguments, and display
    // name (lnk filename without extension).  PWA shortcuts carry their
    // --profile-directory / --app-id args here.
    static LnkInfo ResolveLnkFull(const std::wstring& lnkPath) {
        LnkInfo info;

        IShellLinkW* pSL = nullptr;
        if (FAILED(CoCreateInstance(CLSID_ShellLink, nullptr, CLSCTX_INPROC_SERVER,
                                    IID_PPV_ARGS(&pSL))))
            return info;

        IPersistFile* pPF = nullptr;
        if (FAILED(pSL->QueryInterface(IID_PPV_ARGS(&pPF)))) {
            pSL->Release();
            return info;
        }

        if (SUCCEEDED(pPF->Load(lnkPath.c_str(), STGM_READ))) {
            WIN32_FIND_DATAW fd;
            wchar_t target[MAX_PATH] = {};
            if (SUCCEEDED(pSL->GetPath(target, MAX_PATH, &fd, SLGP_UNCPRIORITY))
                && target[0] != L'\0') {
                info.path = target;

                // Capture launch arguments (critical for PWA shortcuts)
                wchar_t argsBuf[4096] = {};
                if (SUCCEEDED(pSL->GetArguments(argsBuf, 4096)) && argsBuf[0] != L'\0')
                    info.args = argsBuf;

                // Derive display name from the .lnk filename
                size_t slash = lnkPath.find_last_of(L"\\/");
                std::wstring fn = (slash != std::wstring::npos)
                                  ? lnkPath.substr(slash + 1) : lnkPath;
                size_t dot = fn.rfind(L'.');
                info.name = (dot != std::wstring::npos) ? fn.substr(0, dot) : fn;
            }
        }

        pPF->Release();
        pSL->Release();
        return info;
    }

    // Convenience wrapper – returns only the target path (no args).
    static std::wstring ResolveLnk(const std::wstring& lnkPath) {
        return ResolveLnkFull(lnkPath).path;
    }

private:
    // Scan a directory for .lnk files and resolve each one.
    // If exeFilter is non-null, only include shortcuts whose target filename
    // matches (case-insensitive) and that contain "--app-id" in their args.
    static void ScanLnkDir(const std::wstring& dir,
                            std::vector<LnkInfo>& out,
                            const wchar_t* exeFilter) {
        WIN32_FIND_DATAW fd;
        HANDLE hFind = FindFirstFileW((dir + L"\\*.lnk").c_str(), &fd);
        if (hFind == INVALID_HANDLE_VALUE) return;

        do {
            if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) continue;
            LnkInfo info = ResolveLnkFull(dir + L"\\" + fd.cFileName);
            if (info.path.empty()) continue;

            if (exeFilter) {
                // Only accept shortcuts targeting the specified exe
                const wchar_t* fn = wcsrchr(info.path.c_str(), L'\\');
                fn = fn ? fn + 1 : info.path.c_str();
                if (_wcsicmp(fn, exeFilter) != 0) continue;
                // And only if they carry --app-id (identifies a real PWA)
                if (info.args.find(L"--app-id") == std::wstring::npos) continue;
            }

            out.push_back(std::move(info));
        } while (FindNextFileW(hFind, &fd));

        FindClose(hFind);
    }

    static void ScanDir(const std::wstring& dir, bool recursive,
                        std::vector<std::wstring>& out) {
        WIN32_FIND_DATAW fd;
        HANDLE hFind = FindFirstFileW((dir + L"\\*").c_str(), &fd);
        if (hFind == INVALID_HANDLE_VALUE) return;

        do {
            if (fd.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN)  continue;
            if (fd.dwFileAttributes & FILE_ATTRIBUTE_SYSTEM)  continue;
            if (wcscmp(fd.cFileName, L".") == 0)              continue;
            if (wcscmp(fd.cFileName, L"..") == 0)             continue;

            std::wstring full = dir + L"\\" + fd.cFileName;

            if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
                if (recursive) ScanDir(full, true, out);
            } else {
                if (IsExecutable(fd.cFileName)) out.push_back(full);
            }
        } while (FindNextFileW(hFind, &fd));

        FindClose(hFind);
    }

    static bool IsExecutable(const wchar_t* name) {
        const wchar_t* dot = wcsrchr(name, L'.');
        if (!dot) return false;
        return _wcsicmp(dot, L".exe") == 0
            || _wcsicmp(dot, L".bat") == 0
            || _wcsicmp(dot, L".cmd") == 0;
    }
};

} // namespace QuickLaunchTool
