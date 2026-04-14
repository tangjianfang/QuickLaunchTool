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

class FileScanner {
public:
    // Recursively scan a folder for .exe / .bat / .cmd files.
    static std::vector<std::wstring> ScanFolder(const std::wstring& folder, bool recursive) {
        std::vector<std::wstring> results;
        ScanDir(folder, recursive, results);
        return results;
    }

    // Return resolved target paths of all .lnk files in the Windows Taskbar
    // pinned apps folder.
    static std::vector<std::wstring> GetTaskbarPinnedApps() {
        std::vector<std::wstring> results;

        wchar_t buf[MAX_PATH] = {};
        // %APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar
        if (FAILED(SHGetFolderPathW(nullptr, CSIDL_APPDATA, nullptr, 0, buf)))
            return results;

        std::wstring dir = std::wstring(buf)
            + L"\\Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar";

        WIN32_FIND_DATAW fd;
        HANDLE hFind = FindFirstFileW((dir + L"\\*.lnk").c_str(), &fd);
        if (hFind == INVALID_HANDLE_VALUE) return results;

        do {
            if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) continue;
            std::wstring lnkPath = dir + L"\\" + fd.cFileName;
            std::wstring target  = ResolveLnk(lnkPath);
            if (!target.empty()) results.push_back(target);
        } while (FindNextFileW(hFind, &fd));

        FindClose(hFind);
        return results;
    }

    // Resolve a .lnk shortcut to its target path via IShellLink.
    static std::wstring ResolveLnk(const std::wstring& lnkPath) {
        IShellLinkW* pSL = nullptr;
        if (FAILED(CoCreateInstance(CLSID_ShellLink, nullptr, CLSCTX_INPROC_SERVER,
                                    IID_PPV_ARGS(&pSL))))
            return {};

        IPersistFile* pPF = nullptr;
        if (FAILED(pSL->QueryInterface(IID_PPV_ARGS(&pPF)))) {
            pSL->Release();
            return {};
        }

        std::wstring result;
        if (SUCCEEDED(pPF->Load(lnkPath.c_str(), STGM_READ))) {
            WIN32_FIND_DATAW fd;
            wchar_t target[MAX_PATH] = {};
            if (SUCCEEDED(pSL->GetPath(target, MAX_PATH, &fd, SLGP_UNCPRIORITY))
                && target[0] != L'\0') {
                result = target;
            }
        }

        pPF->Release();
        pSL->Release();
        return result;
    }

private:
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
