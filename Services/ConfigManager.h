#pragma once

#include <string>
#include <algorithm>
#include <windows.h>
#include <shlobj.h>
#include "../Models/AppConfig.h"
#include "../Utils/JsonHelper.h"

namespace QuickLaunchTool {

class ConfigManager {
public:
    static ConfigManager* GetInstance() {
        if (!s_instance) s_instance = new ConfigManager();
        return s_instance;
    }
    static void DestroyInstance() { delete s_instance; s_instance = nullptr; }

    AppConfig& Config() { return m_config; }

    // ── Path helpers ──────────────────────────────────────────────────────

    void AddPath(const std::wstring& path) {
        for (const auto& p : m_config.cachedAppPaths)
            if (_wcsicmp(p.c_str(), path.c_str()) == 0) return; // already present
        m_config.cachedAppPaths.push_back(path);
        Save();
    }

    void RemovePath(const std::wstring& path) {
        auto& v = m_config.cachedAppPaths;
        auto it = std::remove_if(v.begin(), v.end(),
            [&](const std::wstring& p){ return _wcsicmp(p.c_str(), path.c_str()) == 0; });
        if (it == v.end()) return;
        v.erase(it, v.end());
        m_config.useCountMap.erase(LowerKey(path));
        Save();
    }

    void IncrementUseCount(const std::wstring& path) {
        auto key = LowerKey(path);
        ++m_config.useCountMap[key];
        Save();
    }

    int GetUseCount(const std::wstring& path) const {
        auto key = LowerKey(path);
        auto it = m_config.useCountMap.find(key);
        return it != m_config.useCountMap.end() ? it->second : 0;
    }

    // ── Persistence ───────────────────────────────────────────────────────

    void Load() {
        HANDLE hFile = CreateFileW(m_configPath.c_str(), GENERIC_READ, FILE_SHARE_READ,
                                   nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (hFile == INVALID_HANDLE_VALUE) return;

        DWORD sz = GetFileSize(hFile, nullptr);
        if (sz == 0 || sz > 2 * 1024 * 1024) { CloseHandle(hFile); return; }

        std::string raw(sz, '\0');
        DWORD read = 0;
        if (!ReadFile(hFile, raw.data(), sz, &read, nullptr)) { CloseHandle(hFile); return; }
        CloseHandle(hFile);

        // Convert UTF-8 → wide
        int wlen = MultiByteToWideChar(CP_UTF8, 0, raw.c_str(), -1, nullptr, 0);
        std::wstring json(wlen, L'\0');
        MultiByteToWideChar(CP_UTF8, 0, raw.c_str(), -1, json.data(), wlen);

        m_config.sortMode  = static_cast<SortMode> (JsonHelper::ExtractInt(json, L"sortMode"));
        m_config.theme     = static_cast<ThemeMode>(JsonHelper::ExtractInt(json, L"theme"));
        m_config.iconSize  = static_cast<IconSize> (JsonHelper::ExtractInt(json, L"iconSize"));
        m_config.language  = JsonHelper::ExtractString(json, L"language");
        if (m_config.language.empty()) m_config.language = L"zh-CN";
        m_config.topMost   = JsonHelper::ExtractBool(json, L"topMost");
        m_config.opacity   = JsonHelper::ExtractDouble(json, L"opacity");
        if (m_config.opacity < 0.1 || m_config.opacity > 1.0) m_config.opacity = 1.0;
        m_config.windowX   = JsonHelper::ExtractInt(json, L"windowX");
        m_config.windowY   = JsonHelper::ExtractInt(json, L"windowY");
        m_config.windowW   = JsonHelper::ExtractInt(json, L"windowW");
        m_config.windowH   = JsonHelper::ExtractInt(json, L"windowH");
        if (m_config.windowW < 400) m_config.windowW = 900;
        if (m_config.windowH < 300) m_config.windowH = 600;

        m_config.cachedAppPaths = JsonHelper::ExtractStringArray(json, L"cachedAppPaths");
        m_config.useCountMap    = JsonHelper::ExtractStringIntMap(json, L"useCountMap");
    }

    void Save() {
        auto& c = m_config;
        std::vector<std::pair<std::wstring, std::wstring>> fields;

        fields.push_back({L"sortMode",       std::to_wstring(static_cast<int>(c.sortMode))});
        fields.push_back({L"theme",          std::to_wstring(static_cast<int>(c.theme))});
        fields.push_back({L"iconSize",       std::to_wstring(static_cast<int>(c.iconSize))});
        fields.push_back({L"language",       L"\"" + c.language + L"\""});
        fields.push_back({L"topMost",        c.topMost ? L"true" : L"false"});
        wchar_t op[16]; swprintf_s(op, L"%.2f", c.opacity);
        fields.push_back({L"opacity",        op});
        fields.push_back({L"windowX",        std::to_wstring(c.windowX)});
        fields.push_back({L"windowY",        std::to_wstring(c.windowY)});
        fields.push_back({L"windowW",        std::to_wstring(c.windowW)});
        fields.push_back({L"windowH",        std::to_wstring(c.windowH)});
        fields.push_back({L"cachedAppPaths", JsonHelper::StringArrayToJson(c.cachedAppPaths)});
        fields.push_back({L"useCountMap",    JsonHelper::StringIntMapToJson(c.useCountMap)});

        std::wstring json = JsonHelper::BuildObject(fields);

        int utf8len = WideCharToMultiByte(CP_UTF8, 0, json.c_str(), -1, nullptr, 0, nullptr, nullptr);
        std::string utf8(utf8len, '\0');
        WideCharToMultiByte(CP_UTF8, 0, json.c_str(), -1, utf8.data(), utf8len, nullptr, nullptr);

        HANDLE hFile = CreateFileW(m_configPath.c_str(), GENERIC_WRITE, 0,
                                   nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (hFile == INVALID_HANDLE_VALUE) return;
        DWORD written = 0;
        WriteFile(hFile, utf8.c_str(), static_cast<DWORD>(utf8.size() - 1), &written, nullptr);
        CloseHandle(hFile);
    }

private:
    AppConfig   m_config;
    std::wstring m_configPath;
    static ConfigManager* s_instance;

    ConfigManager() {
        wchar_t appData[MAX_PATH] = {};
        if (SUCCEEDED(SHGetFolderPathW(nullptr, CSIDL_APPDATA, nullptr, 0, appData))) {
            std::wstring dir = std::wstring(appData) + L"\\QuickLaunchToolCpp";
            CreateDirectoryW(dir.c_str(), nullptr);
            m_configPath = dir + L"\\config.json";
        } else {
            m_configPath = L"config.json";
        }
        Load();
    }

    static std::wstring LowerKey(const std::wstring& s) {
        std::wstring k = s;
        CharLowerW(k.data());
        return k;
    }
};

} // namespace QuickLaunchTool
