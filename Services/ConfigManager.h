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

    // ── App helpers ───────────────────────────────────────────────────────

    // Add an app entry.  For PWAs pass the exe path, the argument string
    // (e.g. "--profile-directory=Default --app-id=xxx"), and the display
    // name (e.g. "Gmail").  Regular apps leave args/name empty.
    void AddApp(const std::wstring& path,
                const std::wstring& args = L"",
                const std::wstring& name = L"") {
        std::wstring key = AppKey(path, args);
        for (size_t i = 0; i < m_config.cachedAppPaths.size(); ++i) {
            std::wstring eArgs = (i < m_config.cachedAppArgs.size())
                                 ? m_config.cachedAppArgs[i] : L"";
            if (AppKey(m_config.cachedAppPaths[i], eArgs) == key) return;
        }
        m_config.cachedAppPaths.push_back(path);
        m_config.cachedAppArgs.push_back(args);
        m_config.cachedAppNames.push_back(name);
        Save();
    }

    // Convenience alias for callers that only have a plain exe path.
    void AddPath(const std::wstring& path) { AddApp(path); }

    void RemoveApp(const std::wstring& path, const std::wstring& args = L"") {
        std::wstring key = AppKey(path, args);
        for (size_t i = 0; i < m_config.cachedAppPaths.size(); ++i) {
            std::wstring eArgs = (i < m_config.cachedAppArgs.size())
                                 ? m_config.cachedAppArgs[i] : L"";
            if (AppKey(m_config.cachedAppPaths[i], eArgs) != key) continue;

            m_config.cachedAppPaths.erase(m_config.cachedAppPaths.begin() + i);
            if (i < m_config.cachedAppArgs.size())
                m_config.cachedAppArgs.erase(m_config.cachedAppArgs.begin() + i);
            if (i < m_config.cachedAppNames.size())
                m_config.cachedAppNames.erase(m_config.cachedAppNames.begin() + i);
            m_config.useCountMap.erase(key);
            Save();
            return;
        }
    }

    // Legacy alias – removes by path only (args assumed empty).
    void RemovePath(const std::wstring& path) { RemoveApp(path, L""); }

    void IncrementUseCount(const std::wstring& path, const std::wstring& args = L"") {
        auto key = AppKey(path, args);
        ++m_config.useCountMap[key];
        Save();
    }

    int GetUseCount(const std::wstring& path, const std::wstring& args = L"") const {
        auto key = AppKey(path, args);
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
        m_config.cachedAppArgs  = JsonHelper::ExtractStringArray(json, L"cachedAppArgs");
        m_config.cachedAppNames = JsonHelper::ExtractStringArray(json, L"cachedAppNames");
        m_config.useCountMap    = JsonHelper::ExtractStringIntMap(json, L"useCountMap");

        // Ensure parallel arrays are the same length (pad with empty strings)
        size_t n = m_config.cachedAppPaths.size();
        m_config.cachedAppArgs.resize(n);
        m_config.cachedAppNames.resize(n);
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
        fields.push_back({L"cachedAppArgs",  JsonHelper::StringArrayToJson(c.cachedAppArgs)});
        fields.push_back({L"cachedAppNames", JsonHelper::StringArrayToJson(c.cachedAppNames)});
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

    // Build a unique lowercase key for useCountMap.
    // For PWAs with args, the key is "path\targs" so two PWAs sharing the
    // same exe but different --app-id values get distinct counts.
    static std::wstring AppKey(const std::wstring& path, const std::wstring& args) {
        std::wstring k = path;
        if (!args.empty()) { k += L'\t'; k += args; }
        CharLowerW(k.data());
        return k;
    }

    static std::wstring LowerKey(const std::wstring& s) {
        std::wstring k = s;
        CharLowerW(k.data());
        return k;
    }
};

} // namespace QuickLaunchTool
