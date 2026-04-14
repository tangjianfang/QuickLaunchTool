#ifndef NOMINMAX
#define NOMINMAX
#endif

#include "AppGrid.h"
#include <algorithm>
#include <shellapi.h>
#include <windowsx.h>
#include "../Utils/ThemeManager.h"
#include "../Resources/Resource.h"

namespace QuickLaunchTool {

const wchar_t* AppGrid::s_className = L"QLT_AppGrid";

// ── Registration ──────────────────────────────────────────────────────────────

bool AppGrid::Register(HINSTANCE hInst) {
    WNDCLASSEXW wc = {};
    wc.cbSize        = sizeof(wc);
    wc.style         = CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS;
    wc.lpfnWndProc   = s_WndProc;
    wc.hInstance     = hInst;
    wc.hCursor       = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = nullptr; // we paint everything
    wc.lpszClassName = s_className;
    return RegisterClassExW(&wc) != 0;
}

// ── Construction / Destruction ────────────────────────────────────────────────

AppGrid::AppGrid() = default;
AppGrid::~AppGrid() = default;

bool AppGrid::Create(HWND hParent, HINSTANCE hInst, int x, int y, int w, int h) {
    m_hWnd = CreateWindowExW(WS_EX_ACCEPTFILES, s_className, nullptr,
                             WS_CHILD | WS_VISIBLE | WS_VSCROLL | WS_CLIPCHILDREN,
                             x, y, w, h, hParent,
                             reinterpret_cast<HMENU>(2000), hInst, this);
    if (!m_hWnd) return false;
    DragAcceptFiles(m_hWnd, TRUE);

    // Tracking tooltip for icon hover names
    m_hTooltip = CreateWindowExW(WS_EX_TOPMOST, TOOLTIPS_CLASSW, nullptr,
        WS_POPUP | TTS_NOPREFIX | TTS_ALWAYSTIP,
        CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
        m_hWnd, nullptr, hInst, nullptr);
    if (m_hTooltip) {
        TOOLINFOW ti = {};
        ti.cbSize   = sizeof(ti);
        ti.uFlags   = TTF_TRACK | TTF_ABSOLUTE;
        ti.hwnd     = m_hWnd;
        ti.uId      = 1;
        ti.lpszText = const_cast<wchar_t*>(L"");
        SendMessageW(m_hTooltip, TTM_ADDTOOLW, 0, reinterpret_cast<LPARAM>(&ti));
    }
    return true;
}

// ── Public interface ──────────────────────────────────────────────────────────

void AppGrid::Update(const std::vector<AppInfo>* apps,
                     const std::vector<int>*     indices,
                     ThemeMode theme, int iconPx) {
    m_apps    = apps;
    m_indices = indices;
    m_theme   = theme;
    m_iconPx  = iconPx;
    m_btnW    = iconPx + 32;
    m_btnH    = iconPx + 34;
    m_hovered = -1;

    RECT rc; GetClientRect(m_hWnd, &rc);
    RecalcLayout(rc.right);
    UpdateScrollBar(rc.bottom);
    InvalidateRect(m_hWnd, nullptr, TRUE);
}

void AppGrid::ScrollToTop() {
    m_scrollY = 0;
    SCROLLINFO si = {};
    si.cbSize = sizeof(si); si.fMask = SIF_POS; si.nPos = 0;
    SetScrollInfo(m_hWnd, SB_VERT, &si, TRUE);
    InvalidateRect(m_hWnd, nullptr, TRUE);
}

void AppGrid::Resize(int x, int y, int w, int h) {
    SetWindowPos(m_hWnd, nullptr, x, y, w, h, SWP_NOZORDER | SWP_NOACTIVATE);
}

// ── Layout ────────────────────────────────────────────────────────────────────

void AppGrid::RecalcLayout(int clientW) {
    m_cols = std::max(1, clientW / m_btnW);
}

void AppGrid::UpdateScrollBar(int clientH) {
    int count     = m_indices ? static_cast<int>(m_indices->size()) : 0;
    int rows      = (count + m_cols - 1) / m_cols;
    int totalH    = rows * m_btnH;

    // Clamp scroll offset
    int maxScroll = std::max(0, totalH - clientH);
    m_scrollY     = std::min(m_scrollY, maxScroll);

    SCROLLINFO si = {};
    si.cbSize = sizeof(si);
    si.fMask  = SIF_RANGE | SIF_PAGE | SIF_POS;
    si.nMin   = 0;
    si.nMax   = totalH;
    si.nPage  = static_cast<UINT>(clientH);
    si.nPos   = m_scrollY;
    SetScrollInfo(m_hWnd, SB_VERT, &si, TRUE);
}

// ── Hit testing ───────────────────────────────────────────────────────────────

int AppGrid::HitTest(int x, int y) const {
    if (!m_indices || m_indices->empty()) return -1;
    int ay   = y + m_scrollY;
    int row  = ay / m_btnH;
    int col  = x  / m_btnW;
    if (col < 0 || col >= m_cols) return -1;
    int idx  = row * m_cols + col;
    if (idx < 0 || idx >= static_cast<int>(m_indices->size())) return -1;

    // Check that cursor is inside the button rect (there's no padding between cells)
    int bx = col * m_btnW;
    int by = row * m_btnH - m_scrollY;
    if (x < bx || x >= bx + m_btnW) return -1;
    if (y < by || y >= by + m_btnH)  return -1;
    return idx;
}

// ── Drawing ───────────────────────────────────────────────────────────────────

void AppGrid::Paint(HDC hdc, const RECT& client) {
    // ── Background ──────────────────────────────────────────────────────
    HBRUSH hBgBrush = CreateSolidBrush(ThemeManager::BgColor(m_theme));
    FillRect(hdc, &client, hBgBrush);
    DeleteObject(hBgBrush);

    if (!m_apps || !m_indices) return;

    int count = static_cast<int>(m_indices->size());
    if (count == 0) return;

    // ── Determine visible range ──────────────────────────────────────────
    int firstRow = std::max(0, m_scrollY / m_btnH - 1);
    int lastRow  = (m_scrollY + client.bottom) / m_btnH + 1;
    int firstIdx = firstRow * m_cols;
    int lastIdx  = std::min(count, (lastRow + 1) * m_cols);

    // ── Font for label text ──────────────────────────────────────────────
    HFONT hFont = static_cast<HFONT>(GetStockObject(DEFAULT_GUI_FONT));
    HFONT hOld  = static_cast<HFONT>(SelectObject(hdc, hFont));
    SetBkMode(hdc, TRANSPARENT);

    for (int i = firstIdx; i < lastIdx; ++i) {
        int row = i / m_cols;
        int col = i % m_cols;
        int x   = col * m_btnW;
        int y   = row * m_btnH - m_scrollY;
        DrawButton(hdc, i, x, y);
    }

    SelectObject(hdc, hOld);
}

void AppGrid::DrawButton(HDC hdc, int filtIdx, int x, int y) {
    const AppInfo& app = (*m_apps)[(*m_indices)[filtIdx]];
    bool hovered  = (filtIdx == m_hovered);
    bool selected = app.isSelected;

    // ── Button background ────────────────────────────────────────────────
    RECT btnRc = { x + 2, y + 2, x + m_btnW - 2, y + m_btnH - 2 };

    COLORREF bgColor;
    if (selected) bgColor = ThemeManager::SelectedColor(m_theme);
    else if (hovered) bgColor = ThemeManager::HoverColor(m_theme);
    else bgColor = ThemeManager::BgColor(m_theme);

    HBRUSH hBg = CreateSolidBrush(bgColor);
    HPEN   hPen;
    if (selected || hovered) {
        COLORREF borderClr = selected
            ? ThemeManager::SelectedBorderColor(m_theme)
            : ThemeManager::BorderColor(m_theme);
        hPen = CreatePen(PS_SOLID, 1, borderClr);
    } else {
        hPen = static_cast<HPEN>(GetStockObject(NULL_PEN));
    }

    HGDIOBJ oldBrush = SelectObject(hdc, hBg);
    HGDIOBJ oldPen   = SelectObject(hdc, hPen);
    RoundRect(hdc, btnRc.left, btnRc.top, btnRc.right, btnRc.bottom, 6, 6);
    SelectObject(hdc, oldBrush);
    SelectObject(hdc, oldPen);
    DeleteObject(hBg);
    if (selected || hovered) DeleteObject(hPen);

    // ── Icon ─────────────────────────────────────────────────────────────
    if (app.hIcon) {
        int iconX = x + (m_btnW - m_iconPx) / 2;
        int iconY = y + 6;
        DrawIconEx(hdc, iconX, iconY, app.hIcon, m_iconPx, m_iconPx, 0, nullptr, DI_NORMAL);
    }

    // ── Label ────────────────────────────────────────────────────────────
    SetTextColor(hdc, ThemeManager::TextColor(m_theme));
    RECT textRc = { x + 2, y + 6 + m_iconPx + 4, x + m_btnW - 2, y + m_btnH - 4 };
    DrawTextW(hdc, app.name.c_str(), -1, &textRc,
              DT_CENTER | DT_END_ELLIPSIS | DT_WORDBREAK | DT_NOPREFIX);
}

// ── Window procedure ──────────────────────────────────────────────────────────

LRESULT CALLBACK AppGrid::s_WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    AppGrid* self = nullptr;
    if (msg == WM_CREATE) {
        auto* cs = reinterpret_cast<CREATESTRUCTW*>(lParam);
        self = reinterpret_cast<AppGrid*>(cs->lpCreateParams);
        self->m_hWnd = hWnd;
        SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(self));
    } else {
        self = reinterpret_cast<AppGrid*>(GetWindowLongPtrW(hWnd, GWLP_USERDATA));
    }
    if (!self) return DefWindowProcW(hWnd, msg, wParam, lParam);
    return self->WndProc(msg, wParam, lParam);
}

LRESULT AppGrid::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {

    case WM_ERASEBKGND:
        return 1; // handled in WM_PAINT

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(m_hWnd, &ps);

        RECT rc; GetClientRect(m_hWnd, &rc);

        // Double-buffer
        HDC     memDC  = CreateCompatibleDC(hdc);
        HBITMAP memBmp = CreateCompatibleBitmap(hdc, rc.right, rc.bottom);
        HGDIOBJ oldBmp = SelectObject(memDC, memBmp);

        Paint(memDC, rc);

        BitBlt(hdc, 0, 0, rc.right, rc.bottom, memDC, 0, 0, SRCCOPY);
        SelectObject(memDC, oldBmp);
        DeleteObject(memBmp);
        DeleteDC(memDC);

        EndPaint(m_hWnd, &ps);
        return 0;
    }

    case WM_SIZE: {
        int w = LOWORD(lParam), h = HIWORD(lParam);
        RecalcLayout(w);
        UpdateScrollBar(h);
        InvalidateRect(m_hWnd, nullptr, TRUE);
        return 0;
    }

    case WM_VSCROLL: {
        SCROLLINFO si = {};
        si.cbSize = sizeof(si); si.fMask = SIF_ALL;
        GetScrollInfo(m_hWnd, SB_VERT, &si);
        int old = si.nPos;
        switch (LOWORD(wParam)) {
            case SB_LINEUP:    si.nPos -= m_btnH; break;
            case SB_LINEDOWN:  si.nPos += m_btnH; break;
            case SB_PAGEUP:    si.nPos -= si.nPage; break;
            case SB_PAGEDOWN:  si.nPos += si.nPage; break;
            case SB_THUMBTRACK: si.nPos = si.nTrackPos; break;
        }
        si.nPos = std::max(si.nMin, std::min(si.nPos, si.nMax - (int)si.nPage + 1));
        si.fMask = SIF_POS;
        SetScrollInfo(m_hWnd, SB_VERT, &si, TRUE);
        GetScrollInfo(m_hWnd, SB_VERT, &si);
        if (si.nPos != old) {
            m_scrollY = si.nPos;
            InvalidateRect(m_hWnd, nullptr, TRUE);
        }
        return 0;
    }

    case WM_MOUSEWHEEL: {
        int delta = GET_WHEEL_DELTA_WPARAM(wParam);
        SCROLLINFO si = {};
        si.cbSize = sizeof(si); si.fMask = SIF_ALL;
        GetScrollInfo(m_hWnd, SB_VERT, &si);
        int old = si.nPos;
        si.nPos -= (delta / WHEEL_DELTA) * m_btnH;
        si.nPos  = std::max(si.nMin, std::min(si.nPos, si.nMax - (int)si.nPage + 1));
        si.fMask = SIF_POS;
        SetScrollInfo(m_hWnd, SB_VERT, &si, TRUE);
        GetScrollInfo(m_hWnd, SB_VERT, &si);
        if (si.nPos != old) {
            m_scrollY = si.nPos;
            InvalidateRect(m_hWnd, nullptr, TRUE);
        }
        return 0;
    }

    case WM_MOUSEMOVE: {
        int x = GET_X_LPARAM(lParam), y = GET_Y_LPARAM(lParam);
        int hit = HitTest(x, y);
        if (hit != m_hovered) {
            m_hovered = hit;
            InvalidateRect(m_hWnd, nullptr, TRUE);
        }
        // Update tracking tooltip
        if (m_hTooltip) {
            TOOLINFOW ti = {};
            ti.cbSize   = sizeof(ti);
            ti.uFlags   = TTF_TRACK | TTF_ABSOLUTE;
            ti.hwnd     = m_hWnd;
            ti.uId      = 1;
            if (hit >= 0 && m_apps && m_indices) {
                const std::wstring& name = (*m_apps)[(*m_indices)[hit]].name;
                ti.lpszText = const_cast<wchar_t*>(name.c_str());
                SendMessageW(m_hTooltip, TTM_UPDATETIPTEXTW, 0, reinterpret_cast<LPARAM>(&ti));
                POINT pt = { x, y };
                ClientToScreen(m_hWnd, &pt);
                SendMessageW(m_hTooltip, TTM_TRACKPOSITION, 0, MAKELPARAM(pt.x + 12, pt.y + 20));
                SendMessageW(m_hTooltip, TTM_TRACKACTIVATE, TRUE, reinterpret_cast<LPARAM>(&ti));
            } else {
                ti.lpszText = const_cast<wchar_t*>(L"");
                SendMessageW(m_hTooltip, TTM_TRACKACTIVATE, FALSE, reinterpret_cast<LPARAM>(&ti));
            }
        }
        // Enable WM_MOUSELEAVE tracking
        if (!m_tracking) {
            TRACKMOUSEEVENT tme = { sizeof(tme), TME_LEAVE, m_hWnd, 0 };
            TrackMouseEvent(&tme);
            m_tracking = true;
        }
        return 0;
    }

    case WM_MOUSELEAVE:
        m_hovered  = -1;
        m_tracking = false;
        InvalidateRect(m_hWnd, nullptr, TRUE);
        if (m_hTooltip) {
            TOOLINFOW ti = {};
            ti.cbSize   = sizeof(ti);
            ti.hwnd     = m_hWnd;
            ti.uId      = 1;
            ti.lpszText = const_cast<wchar_t*>(L"");
            SendMessageW(m_hTooltip, TTM_TRACKACTIVATE, FALSE, reinterpret_cast<LPARAM>(&ti));
        }
        return 0;

    case WM_LBUTTONDOWN: {
        int x = GET_X_LPARAM(lParam), y = GET_Y_LPARAM(lParam);
        int hit = HitTest(x, y);
        if (hit >= 0) {
            bool ctrl = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
            PostMessageW(GetParent(m_hWnd), WMG_SELECT, hit, ctrl ? 1 : 0);
        }
        SetFocus(m_hWnd);
        return 0;
    }

    case WM_LBUTTONDBLCLK: {
        int x = GET_X_LPARAM(lParam), y = GET_Y_LPARAM(lParam);
        int hit = HitTest(x, y);
        if (hit >= 0)
            PostMessageW(GetParent(m_hWnd), WMG_LAUNCH, hit, 0);
        return 0;
    }

    case WM_RBUTTONUP: {
        int x = GET_X_LPARAM(lParam), y = GET_Y_LPARAM(lParam);
        int hit = HitTest(x, y);
        if (hit >= 0) {
            POINT pt = { x, y };
            ClientToScreen(m_hWnd, &pt);
            PostMessageW(GetParent(m_hWnd), WMG_CONTEXTMENU,
                         hit, MAKELPARAM(pt.x, pt.y));
        }
        return 0;
    }

    case WM_DROPFILES:
        // Forward to main window for handling
        SendMessageW(GetParent(m_hWnd), WM_DROPFILES, wParam, lParam);
        return 0;

    } // switch
    return DefWindowProcW(m_hWnd, msg, wParam, lParam);
}

} // namespace QuickLaunchTool
