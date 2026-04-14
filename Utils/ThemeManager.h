#pragma once

#include <windows.h>
#include <dwmapi.h>
#include "../Models/AppConfig.h"

#pragma comment(lib, "dwmapi.lib")

namespace QuickLaunchTool {

class ThemeManager {
public:
    // ── Colors ────────────────────────────────────────────────────────────

    static COLORREF BgColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x20, 0x20, 0x20) : RGB(0xF3, 0xF3, 0xF3);
    }
    static COLORREF ToolbarColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x2C, 0x2C, 0x2C) : RGB(0xFF, 0xFF, 0xFF);
    }
    static COLORREF TextColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0xFF, 0xFF, 0xFF) : RGB(0x00, 0x00, 0x00);
    }
    static COLORREF BorderColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x50, 0x50, 0x50) : RGB(0xD0, 0xD0, 0xD0);
    }
    static COLORREF HoverColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x38, 0x38, 0x38) : RGB(0xE5, 0xE5, 0xE5);
    }
    static COLORREF SelectedColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x00, 0x64, 0xB4) : RGB(0xCC, 0xE8, 0xFF);
    }
    static COLORREF SelectedBorderColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x00, 0x99, 0xFF) : RGB(0x00, 0x78, 0xD4);
    }
    static COLORREF IconBtnHoverColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x45, 0x45, 0x45) : RGB(0xD8, 0xD8, 0xD8);
    }
    static COLORREF IconBtnPressColor(ThemeMode t) {
        return t == ThemeMode::Dark ? RGB(0x30, 0x30, 0x30) : RGB(0xC8, 0xC8, 0xC8);
    }

    // ── Brushes (caller must DeleteObject) ────────────────────────────────

    static HBRUSH BgBrush(ThemeMode t)      { return CreateSolidBrush(BgColor(t));      }
    static HBRUSH ToolbarBrush(ThemeMode t) { return CreateSolidBrush(ToolbarColor(t)); }

    // ── DWM dark title bar (Win10 1809+) ─────────────────────────────────

    static void ApplyDarkTitleBar(HWND hWnd, bool dark) {
        BOOL val = dark ? TRUE : FALSE;
        // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Win10 2004+)
        if (FAILED(DwmSetWindowAttribute(hWnd, 20, &val, sizeof(val)))) {
            // Fallback: attribute 19 (Win10 1809–2004)
            DwmSetWindowAttribute(hWnd, 19, &val, sizeof(val));
        }
    }

    // ── Window opacity ────────────────────────────────────────────────────

    static void SetOpacity(HWND hWnd, double opacity) {
        LONG ex = GetWindowLongW(hWnd, GWL_EXSTYLE);
        if (opacity >= 1.0) {
            SetWindowLongW(hWnd, GWL_EXSTYLE, ex & ~WS_EX_LAYERED);
        } else {
            SetWindowLongW(hWnd, GWL_EXSTYLE, ex | WS_EX_LAYERED);
            BYTE alpha = static_cast<BYTE>(opacity * 255.0);
            SetLayeredWindowAttributes(hWnd, 0, alpha, LWA_ALPHA);
        }
    }

    // ── Apply full theme to a window ─────────────────────────────────────

    static void Apply(HWND hWnd, ThemeMode t) {
        ApplyDarkTitleBar(hWnd, t == ThemeMode::Dark);
        HBRUSH hBg = BgBrush(t);
        SetClassLongPtrW(hWnd, GCLP_HBRBACKGROUND, reinterpret_cast<LONG_PTR>(hBg));
        InvalidateRect(hWnd, nullptr, TRUE);
    }
};

} // namespace QuickLaunchTool
