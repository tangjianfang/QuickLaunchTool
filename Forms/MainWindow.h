#pragma once

#include <windows.h>
#include <commctrl.h>
#include <shellapi.h>
#include <vector>
#include <string>
#include <algorithm>
#include "../Models/AppInfo.h"
#include "../Models/AppConfig.h"
#include "../Controls/AppGrid.h"

namespace QuickLaunchTool {

class MainWindow {
public:
    MainWindow();
    ~MainWindow();

    bool Create(HINSTANCE hInst);
    void Run();

    // Called by SettingsDialog after OK
    void RefreshAfterSettings();

    static MainWindow* GetInstance() { return s_instance; }

private:
    HINSTANCE m_hInst    = nullptr;
    HWND      m_hWnd     = nullptr;    // Main window
    HWND      m_hToolbar = nullptr;    // Custom toolbar strip
    HWND      m_hSearch  = nullptr;    // EDIT control (search)
    HWND      m_hTooltip = nullptr;    // Tooltip control
    AppGrid   m_grid;

    std::vector<AppInfo> m_apps;       // All apps, owns HICONs
    std::vector<int>     m_filtered;   // Indices into m_apps (search-filtered)
    std::wstring         m_searchText;

    int  m_contextTarget = -1;         // filteredIndex for right-click context menu

    static MainWindow* s_instance;

    // ── Initialisation ───────────────────────────────────────────────────
    bool RegisterClasses();
    void CreateToolbar();
    void CreateSearchEdit();
    void CreateTooltip();
    void LayoutChildren();

    // ── App management ───────────────────────────────────────────────────
    void LoadApps();
    void SortApps();
    void FilterApps();

    void AddFile();
    void AddFolder();
    void AddByCommandLine();
    void ImportTaskbar();
    void DeleteSelected();
    void HandleDropFiles(HDROP hDrop);
    void LaunchApp(int filtIdx);
    void LaunchAppAsAdmin(int filtIdx);
    void OpenAppLocation(int filtIdx);
    void RemoveApp(int filtIdx);
    void SetSelection(int filtIdx, bool ctrlHeld);
    void ShowContextMenu(int filtIdx, int screenX, int screenY);

    // ── Toolbar drawing & events ─────────────────────────────────────────
    void PaintToolbar(HDC hdc);
    int  ToolbarHitTest(int x, int y) const;
    void HandleToolbarClick(int cmdId);
    void SetupTooltips();

    // ── Config helpers ───────────────────────────────────────────────────
    void SaveWindowRect();
    void ApplyTheme();
    void ApplyTopMost();
    void ApplyOpacity();

    // ── Window procs ─────────────────────────────────────────────────────
    LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);
    LRESULT ToolbarProc(UINT msg, WPARAM wParam, LPARAM lParam);
    static LRESULT CALLBACK s_WndProc(HWND, UINT, WPARAM, LPARAM);
    static LRESULT CALLBACK s_ToolbarProc(HWND, UINT, WPARAM, LPARAM);

    // Toolbar button descriptor
    struct TBBtn {
        int     cmdId;
        wchar_t glyph;      // Segoe MDL2 Assets code point
        int     x, w;       // filled in during layout
    };
    TBBtn m_btns[6];
    HFONT m_hMDL2Font = nullptr;

    static const int TOOLBAR_H = 44;
    static const int BTN_W     = 36;
    static const int BTN_H     = 32;
};

} // namespace QuickLaunchTool
