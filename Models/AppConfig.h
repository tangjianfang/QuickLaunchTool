#pragma once

#include <string>
#include <vector>
#include <map>

namespace QuickLaunchTool {

enum class SortMode  { Name, Modified, UseCount };
enum class ThemeMode { Light, Dark };
enum class IconSize  { Large, Medium, Small };

struct AppConfig {
    SortMode     sortMode  = SortMode::Name;
    ThemeMode    theme     = ThemeMode::Dark;
    IconSize     iconSize  = IconSize::Medium;
    std::wstring language  = L"zh-CN";
    bool         topMost   = false;
    double       opacity   = 1.0;
    int          windowX   = -1;
    int          windowY   = -1;
    int          windowW   = 900;
    int          windowH   = 600;

    std::vector<std::wstring>   cachedAppPaths;
    std::vector<std::wstring>   cachedAppArgs;   // parallel to cachedAppPaths; empty = no args
    std::vector<std::wstring>   cachedAppNames;  // parallel to cachedAppPaths; empty = derive from exe
    std::map<std::wstring, int> useCountMap;     // AppKey(path,args) lowercase → count

    // Icon pixel dimensions
    int GetIconDimensions() const {
        switch (iconSize) {
            case IconSize::Large:  return 48;
            case IconSize::Small:  return 24;
            default:               return 32;
        }
    }

    // Button cell size (icon + label + padding)
    int GetButtonWidth()  const { return GetIconDimensions() + 32; }
    int GetButtonHeight() const { return GetIconDimensions() + 34; }
};

} // namespace QuickLaunchTool
