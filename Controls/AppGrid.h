#pragma once

#include <windows.h>
#include <vector>
#include "../Models/AppInfo.h"
#include "../Models/AppConfig.h"

namespace QuickLaunchTool {

// Scrollable icon-grid control.
//
// Ownership: AppGrid stores const-pointers to the parent's app and index
// vectors. The parent must call Update() whenever either vector changes.
//
// Notifications sent to parent via PostMessage:
//   WMG_LAUNCH      wParam = filtered index
//   WMG_SELECT      wParam = filtered index, lParam = 1 if Ctrl held
//   WMG_CONTEXTMENU wParam = filtered index, lParam = MAKELPARAM(screenX, screenY)
//
// (WMG_* defined in Resources/Resource.h)

class AppGrid {
public:
    static bool Register(HINSTANCE hInst);

    AppGrid();
    ~AppGrid();

    bool Create(HWND hParent, HINSTANCE hInst, int x, int y, int w, int h);
    HWND Hwnd() const { return m_hWnd; }

    // Call after app list, indices, theme, or icon size changes.
    void Update(const std::vector<AppInfo>* apps,
                const std::vector<int>*     indices,
                ThemeMode theme,
                int       iconPx);

    void ScrollToTop();
    void Resize(int x, int y, int w, int h);

private:
    HWND  m_hWnd     = nullptr;
    HWND  m_hTooltip = nullptr;
    int   m_iconPx  = 32;
    int   m_btnW    = 64;
    int   m_btnH    = 66;
    int   m_cols    = 1;
    int   m_scrollY = 0;
    int   m_hovered = -1;
    bool  m_tracking = false;   // TrackMouseEvent active?
    ThemeMode m_theme = ThemeMode::Light;

    const std::vector<AppInfo>* m_apps    = nullptr;
    const std::vector<int>*     m_indices = nullptr;

    // ── Drawing ───────────────────────────────────────────────────────────
    void Paint(HDC hdc, const RECT& client);
    void DrawButton(HDC hdc, int filtIdx, int x, int y);

    // ── Layout ────────────────────────────────────────────────────────────
    void RecalcLayout(int clientW);
    void UpdateScrollBar(int clientH);

    // ── Hit testing ───────────────────────────────────────────────────────
    int  HitTest(int x, int y) const;

    // ── Window proc ───────────────────────────────────────────────────────
    LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);
    static LRESULT CALLBACK s_WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

    static const wchar_t* s_className;
};

} // namespace QuickLaunchTool
