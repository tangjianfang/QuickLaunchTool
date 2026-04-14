#ifndef NOMINMAX
#define NOMINMAX
#endif

#include "MainWindow.h"
#include "SettingsDialog.h"
#include <shlobj.h>
#include <commdlg.h>
#include <windowsx.h>
#include <algorithm>
#include "../Resources/Resource.h"
#include "../Services/ConfigManager.h"
#include "../Services/IconExtractor.h"
#include "../Services/FileScanner.h"
#include "../Services/ProcessLauncher.h"
#include "../Utils/LocalizationManager.h"
#include "../Utils/ThemeManager.h"

#pragma comment(lib, "comctl32.lib")
#pragma comment(linker, "\"/manifestdependency:type='win32' \
name='Microsoft.Windows.Common-Controls' version='6.0.0.0' \
processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")

// Provide RefreshMainWindow so SettingsDialog.cpp's stub is overridden.
void RefreshMainWindow() {
    auto* mw = QuickLaunchTool::MainWindow::GetInstance();
    if (mw) mw->RefreshAfterSettings();
}

namespace QuickLaunchTool {

MainWindow* MainWindow::s_instance = nullptr;

// ── MDL2 glyph code points ────────────────────────────────────────────────────
// Segoe MDL2 Assets (Win10+)
static const wchar_t GLYPH_ADDFILE   = L'\uE8A5'; // DocumentAdd
static const wchar_t GLYPH_ADDFOLDER = L'\uED43'; // FolderAdd
static const wchar_t GLYPH_IMPORT    = L'\uE898'; // Download/Import
static const wchar_t GLYPH_DELETE    = L'\uE74D'; // Delete
static const wchar_t GLYPH_SETTINGS  = L'\uE713'; // Settings

// ── Construction / Destruction ────────────────────────────────────────────────

MainWindow::MainWindow() {
    s_instance = this;
    m_btns[0] = { ID_TOOLBAR_ADDFILE,   GLYPH_ADDFILE,   0, BTN_W };
    m_btns[1] = { ID_TOOLBAR_ADDFOLDER, GLYPH_ADDFOLDER, 0, BTN_W };
    m_btns[2] = { ID_TOOLBAR_IMPORT,    GLYPH_IMPORT,    0, BTN_W };
    m_btns[3] = { ID_TOOLBAR_DELETE,    GLYPH_DELETE,    0, BTN_W };
    m_btns[4] = { ID_TOOLBAR_SETTINGS,  GLYPH_SETTINGS,  0, BTN_W };
}

MainWindow::~MainWindow() {
    for (auto& a : m_apps)
        if (a.hIcon) { DestroyIcon(a.hIcon); a.hIcon = nullptr; }
    if (m_hMDL2Font) DeleteObject(m_hMDL2Font);
    s_instance = nullptr;
}

// ── Create ────────────────────────────────────────────────────────────────────

bool MainWindow::Create(HINSTANCE hInst) {
    m_hInst = hInst;
    if (!RegisterClasses()) return false;
    if (!AppGrid::Register(hInst)) return false;

    // MDL2 font for toolbar icon buttons
    m_hMDL2Font = CreateFontW(20, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
                               DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
                               CLEARTYPE_QUALITY, DEFAULT_PITCH, L"Segoe MDL2 Assets");

    auto& cfg = ConfigManager::GetInstance()->Config();
    LocalizationManager::GetInstance()->SetLanguage(cfg.language);

    int x = cfg.windowX, y = cfg.windowY;
    int w = cfg.windowW, h = cfg.windowH;

    if (x < 0 || y < 0) {
        x = (GetSystemMetrics(SM_CXSCREEN) - w) / 2;
        y = (GetSystemMetrics(SM_CYSCREEN) - h) / 2;
    }

    m_hWnd = CreateWindowExW(
        WS_EX_ACCEPTFILES, L"QLT_MainWindow",
        LocalizationManager::GetInstance()->Get(LocalizationManager::Key::AppName).c_str(),
        WS_OVERLAPPEDWINDOW,
        x, y, w, h,
        nullptr, nullptr, hInst, this);
    if (!m_hWnd) return false;

    ThemeManager::ApplyDarkTitleBar(m_hWnd, cfg.theme == ThemeMode::Dark);
    ApplyTopMost();
    ApplyOpacity();

    ShowWindow(m_hWnd, SW_SHOW);
    UpdateWindow(m_hWnd);
    return true;
}

// ── Run ───────────────────────────────────────────────────────────────────────

void MainWindow::Run() {
    MSG msg;
    while (GetMessageW(&msg, nullptr, 0, 0)) {
        if (!IsDialogMessageW(m_hWnd, &msg)) {
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
    }
}

// ── RefreshAfterSettings ──────────────────────────────────────────────────────

void MainWindow::RefreshAfterSettings() {
    auto& cfg = ConfigManager::GetInstance()->Config();
    LocalizationManager::GetInstance()->SetLanguage(cfg.language);

    SetWindowTextW(m_hWnd,
        LocalizationManager::GetInstance()->Get(LocalizationManager::Key::AppName).c_str());

    ApplyTheme();
    ApplyTopMost();
    ApplyOpacity();

    bool iconSizeChanged = true; // always reload to be safe
    if (iconSizeChanged) {
        LoadApps(); // reloads icons at new size
    } else {
        SortApps();
        FilterApps();
    }

    SetupTooltips();
    InvalidateRect(m_hToolbar, nullptr, TRUE);
}

// ── Window class registration ─────────────────────────────────────────────────

bool MainWindow::RegisterClasses() {
    WNDCLASSEXW wc = {};
    wc.cbSize        = sizeof(wc);
    wc.style         = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc   = s_WndProc;
    wc.hInstance     = m_hInst;
    wc.hIcon         = LoadIconW(m_hInst, MAKEINTRESOURCEW(IDI_APP_ICON));
    wc.hIconSm       = wc.hIcon;
    wc.hCursor       = LoadCursorW(nullptr, IDC_ARROW);
    wc.hbrBackground = nullptr;
    wc.lpszClassName = L"QLT_MainWindow";
    if (!RegisterClassExW(&wc)) return false;

    WNDCLASSEXW tb = {};
    tb.cbSize        = sizeof(tb);
    tb.style         = CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS;
    tb.lpfnWndProc   = s_ToolbarProc;
    tb.hInstance     = m_hInst;
    tb.hCursor       = LoadCursorW(nullptr, IDC_ARROW);
    tb.hbrBackground = nullptr;
    tb.lpszClassName = L"QLT_Toolbar";
    if (!RegisterClassExW(&tb)) return false;

    return true;
}

// ── Child window creation ─────────────────────────────────────────────────────

void MainWindow::CreateToolbar() {
    auto& cfg = ConfigManager::GetInstance()->Config();
    m_hToolbar = CreateWindowExW(
        0, L"QLT_Toolbar", nullptr,
        WS_CHILD | WS_VISIBLE,
        0, 0, 0, TOOLBAR_H,
        m_hWnd, reinterpret_cast<HMENU>(1000), m_hInst, this);

    CreateSearchEdit();
}

void MainWindow::CreateSearchEdit() {
    m_hSearch = CreateWindowExW(
        WS_EX_CLIENTEDGE, L"EDIT", nullptr,
        WS_CHILD | WS_VISIBLE | ES_AUTOHSCROLL,
        0, 0, 150, 26,
        m_hToolbar, reinterpret_cast<HMENU>(ID_SEARCH_EDIT), m_hInst, nullptr);

    SendMessageW(m_hSearch, WM_SETFONT,
                 reinterpret_cast<WPARAM>(GetStockObject(DEFAULT_GUI_FONT)), TRUE);

    // Placeholder text shown when empty
    auto* loc = LocalizationManager::GetInstance();
    SendMessageW(m_hSearch, EM_SETCUEBANNER, FALSE,
                 reinterpret_cast<LPARAM>(loc->Get(LocalizationManager::Key::Search).c_str()));
}

void MainWindow::CreateTooltip() {
    m_hTooltip = CreateWindowExW(0, TOOLTIPS_CLASSW, nullptr,
        WS_POPUP | TTS_NOPREFIX | TTS_ALWAYSTIP,
        CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
        m_hToolbar, nullptr, m_hInst, nullptr);
    SetupTooltips();
}

void MainWindow::SetupTooltips() {
    if (!m_hTooltip) return;
    // Remove all existing tools
    TOOLINFOW ti = {};
    ti.cbSize = sizeof(ti);
    while (SendMessageW(m_hTooltip, TTM_ENUMTOOLS, 0, reinterpret_cast<LPARAM>(&ti)))
        SendMessageW(m_hTooltip, TTM_DELTOOL, 0, reinterpret_cast<LPARAM>(&ti));

    auto* loc = LocalizationManager::GetInstance();
    LocalizationManager::Key tips[] = {
        LocalizationManager::Key::TipAddFile,
        LocalizationManager::Key::TipAddFolder,
        LocalizationManager::Key::TipImport,
        LocalizationManager::Key::TipDelete,
        LocalizationManager::Key::TipSettings,
    };

    for (int i = 0; i < 5; ++i) {
        TOOLINFOW tip = {};
        tip.cbSize   = sizeof(tip);
        tip.uFlags   = TTF_SUBCLASS;
        tip.hwnd     = m_hToolbar;
        tip.uId      = static_cast<UINT_PTR>(m_btns[i].cmdId);
        tip.rect     = { m_btns[i].x, (TOOLBAR_H - BTN_H) / 2,
                         m_btns[i].x + m_btns[i].w, (TOOLBAR_H - BTN_H) / 2 + BTN_H };
        tip.lpszText = const_cast<wchar_t*>(loc->Get(tips[i]).c_str());
        SendMessageW(m_hTooltip, TTM_ADDTOOLW, 0, reinterpret_cast<LPARAM>(&tip));
    }
}

// ── Layout ────────────────────────────────────────────────────────────────────

void MainWindow::LayoutChildren() {
    RECT rc; GetClientRect(m_hWnd, &rc);
    int w = rc.right, h = rc.bottom;

    SetWindowPos(m_hToolbar, nullptr, 0, 0, w, TOOLBAR_H, SWP_NOZORDER | SWP_NOACTIVATE);
    m_grid.Resize(0, TOOLBAR_H, w, h - TOOLBAR_H);

    // Layout toolbar internals
    // Left: search box (fills space left of buttons)
    // Right: 5 icon buttons (Settings last, then others grouped left)
    int rightEdge = w - 4;
    // Settings button at far right
    m_btns[4].x = rightEdge - BTN_W;
    // Gap before settings
    rightEdge = m_btns[4].x - 8;
    // Delete, Import, AddFolder, AddFile from right to left
    for (int i = 3; i >= 0; --i) {
        m_btns[i].x = rightEdge - BTN_W;
        rightEdge   = m_btns[i].x - 2;
    }

    // Search box: fixed width (200px max), vertically centred
    int searchLeft  = 8;
    int searchWidth = std::min(200, m_btns[0].x - 16);
    int searchY     = (TOOLBAR_H - 24) / 2;
    SetWindowPos(m_hSearch, nullptr, searchLeft, searchY,
                 searchWidth, 24, SWP_NOZORDER | SWP_NOACTIVATE);

    SetupTooltips();
}

// ── App management ────────────────────────────────────────────────────────────

void MainWindow::LoadApps() {
    // Destroy existing icons
    for (auto& a : m_apps)
        if (a.hIcon) { DestroyIcon(a.hIcon); a.hIcon = nullptr; }
    m_apps.clear();

    auto& cfg  = ConfigManager::GetInstance()->Config();
    int   px   = cfg.GetIconDimensions();

    for (size_t i = 0; i < cfg.cachedAppPaths.size(); ++i) {
        const auto& path      = cfg.cachedAppPaths[i];
        const auto& args      = (i < cfg.cachedAppArgs.size())  ? cfg.cachedAppArgs[i]  : L"";
        const auto& storedName= (i < cfg.cachedAppNames.size()) ? cfg.cachedAppNames[i] : L"";

        AppInfo app;
        app.fullPath  = path;
        app.args      = args;
        app.name      = storedName.empty() ? ProcessLauncher::BaseName(path) : storedName;
        app.useCount  = ConfigManager::GetInstance()->GetUseCount(path, args);
        app.hIcon     = IconExtractor::Extract(path, px);

        WIN32_FILE_ATTRIBUTE_DATA fa;
        if (GetFileAttributesExW(path.c_str(), GetFileExInfoStandard, &fa))
            app.lastModified = fa.ftLastWriteTime;

        m_apps.push_back(std::move(app));
    }

    SortApps();
    FilterApps();
}

void MainWindow::SortApps() {
    auto& cfg = ConfigManager::GetInstance()->Config();
    std::stable_sort(m_apps.begin(), m_apps.end(),
        [&](const AppInfo& a, const AppInfo& b) {
            switch (cfg.sortMode) {
                case SortMode::Name:
                    return _wcsicmp(a.name.c_str(), b.name.c_str()) < 0;
                case SortMode::Modified:
                    return CompareFileTime(&a.lastModified, &b.lastModified) > 0;
                case SortMode::UseCount:
                    return a.useCount > b.useCount;
                default:
                    return false;
            }
        });
}

void MainWindow::FilterApps() {
    m_filtered.clear();
    m_filtered.reserve(m_apps.size());

    for (int i = 0; i < static_cast<int>(m_apps.size()); ++i) {
        if (m_searchText.empty()) {
            m_filtered.push_back(i);
        } else {
            // Case-insensitive substring match
            std::wstring lower = m_apps[i].name;
            CharLowerW(lower.data());
            std::wstring searchLower = m_searchText;
            CharLowerW(searchLower.data());
            if (lower.find(searchLower) != std::wstring::npos)
                m_filtered.push_back(i);
        }
    }

    auto& cfg = ConfigManager::GetInstance()->Config();
    m_grid.Update(&m_apps, &m_filtered, cfg.theme, cfg.GetIconDimensions());
}

// ── App operations ────────────────────────────────────────────────────────────

void MainWindow::SetSelection(int filtIdx, bool ctrlHeld) {
    if (filtIdx < 0 || filtIdx >= static_cast<int>(m_filtered.size())) return;
    int appIdx = m_filtered[filtIdx];

    if (!ctrlHeld) {
        for (auto& a : m_apps) a.isSelected = false;
        m_apps[appIdx].isSelected = true;
    } else {
        m_apps[appIdx].isSelected = !m_apps[appIdx].isSelected;
    }

    auto& cfg = ConfigManager::GetInstance()->Config();
    m_grid.Update(&m_apps, &m_filtered, cfg.theme, cfg.GetIconDimensions());
}

void MainWindow::LaunchApp(int filtIdx) {
    if (filtIdx < 0 || filtIdx >= static_cast<int>(m_filtered.size())) return;
    auto& app = m_apps[m_filtered[filtIdx]];
    ProcessLauncher::Launch(app.fullPath, app.args);
    ++app.useCount;
    ConfigManager::GetInstance()->IncrementUseCount(app.fullPath, app.args);
}

void MainWindow::LaunchAppAsAdmin(int filtIdx) {
    if (filtIdx < 0 || filtIdx >= static_cast<int>(m_filtered.size())) return;
    auto& app = m_apps[m_filtered[filtIdx]];
    ProcessLauncher::LaunchAsAdmin(app.fullPath, app.args);
    ++app.useCount;
    ConfigManager::GetInstance()->IncrementUseCount(app.fullPath, app.args);
}

void MainWindow::OpenAppLocation(int filtIdx) {
    if (filtIdx < 0 || filtIdx >= static_cast<int>(m_filtered.size())) return;
    ProcessLauncher::OpenLocation(m_apps[m_filtered[filtIdx]].fullPath);
}

void MainWindow::RemoveApp(int filtIdx) {
    if (filtIdx < 0 || filtIdx >= static_cast<int>(m_filtered.size())) return;
    const auto& app = m_apps[m_filtered[filtIdx]];
    ConfigManager::GetInstance()->RemoveApp(app.fullPath, app.args);
    LoadApps();
}

void MainWindow::AddFile() {
    wchar_t path[MAX_PATH] = {};
    OPENFILENAMEW ofn = {};
    ofn.lStructSize = sizeof(ofn);
    ofn.hwndOwner   = m_hWnd;
    ofn.lpstrFilter = L"Executable Files (*.exe)\0*.exe\0All Files (*.*)\0*.*\0";
    ofn.lpstrFile   = path;
    ofn.nMaxFile    = MAX_PATH;
    ofn.lpstrTitle  = LocalizationManager::GetInstance()
                        ->Get(LocalizationManager::Key::SelectExeTitle).c_str();
    ofn.Flags       = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST;
    if (GetOpenFileNameW(&ofn)) {
        ConfigManager::GetInstance()->AddPath(path);
        LoadApps();
    }
}

void MainWindow::AddFolder() {
    wchar_t displayName[MAX_PATH] = {};
    BROWSEINFOW bi = {};
    bi.hwndOwner  = m_hWnd;
    bi.pszDisplayName = displayName;
    bi.lpszTitle  = LocalizationManager::GetInstance()
                      ->Get(LocalizationManager::Key::SelectFolderTitle).c_str();
    bi.ulFlags    = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE;

    LPITEMIDLIST pidl = SHBrowseForFolderW(&bi);
    if (!pidl) return;

    wchar_t folder[MAX_PATH] = {};
    if (SHGetPathFromIDListW(pidl, folder)) {
        auto files = FileScanner::ScanFolder(folder, true);
        for (const auto& f : files)
            ConfigManager::GetInstance()->AddPath(f);
        LoadApps();
    }
    CoTaskMemFree(pidl);
}

void MainWindow::ImportTaskbar() {
    auto apps = FileScanner::GetTaskbarPinnedApps();
    for (const auto& a : apps)
        ConfigManager::GetInstance()->AddApp(a.path, a.args, a.name);
    LoadApps();
}

void MainWindow::DeleteSelected() {
    bool any = false;
    for (auto& a : m_apps) if (a.isSelected) { any = true; break; }

    if (!any) {
        MessageBoxW(m_hWnd,
            LocalizationManager::GetInstance()->Get(LocalizationManager::Key::NoAppsSelectedMsg).c_str(),
            LocalizationManager::GetInstance()->Get(LocalizationManager::Key::AppName).c_str(),
            MB_OK | MB_ICONINFORMATION);
        return;
    }

    int r = MessageBoxW(m_hWnd,
        LocalizationManager::GetInstance()->Get(LocalizationManager::Key::ConfirmDeleteMsg).c_str(),
        LocalizationManager::GetInstance()->Get(LocalizationManager::Key::ConfirmDeleteTitle).c_str(),
        MB_YESNO | MB_ICONQUESTION);
    if (r != IDYES) return;

    std::vector<std::pair<std::wstring, std::wstring>> toRemove;
    for (auto& a : m_apps) if (a.isSelected) toRemove.push_back({a.fullPath, a.args});
    for (auto& p : toRemove) ConfigManager::GetInstance()->RemoveApp(p.first, p.second);
    LoadApps();
}

void MainWindow::HandleDropFiles(HDROP hDrop) {
    UINT count = DragQueryFileW(hDrop, 0xFFFFFFFF, nullptr, 0);
    bool added = false;

    for (UINT i = 0; i < count; ++i) {
        wchar_t path[MAX_PATH] = {};
        if (!DragQueryFileW(hDrop, i, path, MAX_PATH)) continue;

        DWORD attr = GetFileAttributesW(path);
        if (attr == INVALID_FILE_ATTRIBUTES) continue;

        if (attr & FILE_ATTRIBUTE_DIRECTORY) {
            // Scan folder for executables
            auto files = FileScanner::ScanFolder(path, true);
            for (const auto& f : files) {
                ConfigManager::GetInstance()->AddPath(f);
                added = true;
            }
        } else {
            // Check extension
            const wchar_t* dot = wcsrchr(path, L'.');
            if (dot) {
                if (_wcsicmp(dot, L".lnk") == 0) {
                    auto info = FileScanner::ResolveLnkFull(path);
                    if (!info.path.empty()) {
                        ConfigManager::GetInstance()->AddApp(info.path, info.args, info.name);
                        added = true;
                    }
                } else if (_wcsicmp(dot, L".exe") == 0 ||
                           _wcsicmp(dot, L".bat") == 0 ||
                           _wcsicmp(dot, L".cmd") == 0) {
                    ConfigManager::GetInstance()->AddPath(path);
                    added = true;
                }
            }
        }
    }

    DragFinish(hDrop);
    if (added) LoadApps();
}

void MainWindow::ShowContextMenu(int filtIdx, int screenX, int screenY) {
    if (filtIdx < 0 || filtIdx >= static_cast<int>(m_filtered.size())) return;
    m_contextTarget = filtIdx;

    auto* loc = LocalizationManager::GetInstance();
    HMENU hMenu = CreatePopupMenu();
    AppendMenuW(hMenu, MF_STRING, ID_CTX_LAUNCH,       loc->Get(LocalizationManager::Key::Launch).c_str());
    AppendMenuW(hMenu, MF_STRING, ID_CTX_RUNAS_ADMIN,  loc->Get(LocalizationManager::Key::RunAsAdmin).c_str());
    AppendMenuW(hMenu, MF_SEPARATOR, 0, nullptr);
    AppendMenuW(hMenu, MF_STRING, ID_CTX_OPEN_LOCATION,loc->Get(LocalizationManager::Key::OpenLocation).c_str());
    AppendMenuW(hMenu, MF_SEPARATOR, 0, nullptr);
    AppendMenuW(hMenu, MF_STRING, ID_CTX_REMOVE,       loc->Get(LocalizationManager::Key::Remove).c_str());

    TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_RIGHTBUTTON, screenX, screenY, 0, m_hWnd, nullptr);
    DestroyMenu(hMenu);
}

// ── Config helpers ────────────────────────────────────────────────────────────

void MainWindow::SaveWindowRect() {
    RECT rc; GetWindowRect(m_hWnd, &rc);
    auto& cfg    = ConfigManager::GetInstance()->Config();
    cfg.windowX  = rc.left;
    cfg.windowY  = rc.top;
    cfg.windowW  = rc.right  - rc.left;
    cfg.windowH  = rc.bottom - rc.top;
    ConfigManager::GetInstance()->Save();
}

void MainWindow::ApplyTheme() {
    auto& cfg = ConfigManager::GetInstance()->Config();
    ThemeManager::ApplyDarkTitleBar(m_hWnd, cfg.theme == ThemeMode::Dark);

    COLORREF bg = ThemeManager::BgColor(cfg.theme);
    HBRUSH hBg  = CreateSolidBrush(bg);
    SetClassLongPtrW(m_hWnd, GCLP_HBRBACKGROUND, reinterpret_cast<LONG_PTR>(hBg));

    m_grid.Update(&m_apps, &m_filtered, cfg.theme, cfg.GetIconDimensions());
    InvalidateRect(m_hToolbar, nullptr, TRUE);
    InvalidateRect(m_hWnd,     nullptr, TRUE);
}

void MainWindow::ApplyTopMost() {
    auto& cfg = ConfigManager::GetInstance()->Config();
    HWND order = cfg.topMost ? HWND_TOPMOST : HWND_NOTOPMOST;
    SetWindowPos(m_hWnd, order, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
}

void MainWindow::ApplyOpacity() {
    auto& cfg = ConfigManager::GetInstance()->Config();
    ThemeManager::SetOpacity(m_hWnd, cfg.opacity);
}

// ── Toolbar drawing ───────────────────────────────────────────────────────────

void MainWindow::PaintToolbar(HDC hdc) {
    RECT rc; GetClientRect(m_hToolbar, &rc);
    auto& cfg = ConfigManager::GetInstance()->Config();

    // Background
    HBRUSH hBg = CreateSolidBrush(ThemeManager::ToolbarColor(cfg.theme));
    FillRect(hdc, &rc, hBg);
    DeleteObject(hBg);

    // Bottom separator line
    HPEN hPen = CreatePen(PS_SOLID, 1, ThemeManager::BorderColor(cfg.theme));
    HGDIOBJ old = SelectObject(hdc, hPen);
    MoveToEx(hdc, 0, rc.bottom - 1, nullptr);
    LineTo(hdc, rc.right, rc.bottom - 1);
    SelectObject(hdc, old);
    DeleteObject(hPen);

    // Icon buttons
    if (!m_hMDL2Font) return;
    HFONT hOldFont = static_cast<HFONT>(SelectObject(hdc, m_hMDL2Font));
    SetBkMode(hdc, TRANSPARENT);

    POINT cursor; GetCursorPos(&cursor);
    ScreenToClient(m_hToolbar, &cursor);
    int hoverCmd = ToolbarHitTest(cursor.x, cursor.y);

    for (const auto& btn : m_btns) {
        int bx = btn.x;
        int by = (TOOLBAR_H - BTN_H) / 2;

        // Hover background
        if (btn.cmdId == hoverCmd) {
            HBRUSH hHov = CreateSolidBrush(ThemeManager::IconBtnHoverColor(cfg.theme));
            RECT   hr   = { bx, by, bx + BTN_W, by + BTN_H };
            FillRect(hdc, &hr, hHov);
            DeleteObject(hHov);
        }

        // Glyph
        COLORREF glyphColor = ThemeManager::TextColor(cfg.theme);
        SetTextColor(hdc, glyphColor);
        RECT glyphRc = { bx, by, bx + BTN_W, by + BTN_H };
        DrawTextW(hdc, &btn.glyph, 1, &glyphRc, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
    }
    SelectObject(hdc, hOldFont);
}

int MainWindow::ToolbarHitTest(int x, int y) const {
    int by = (TOOLBAR_H - BTN_H) / 2;
    if (y < by || y > by + BTN_H) return -1;
    for (const auto& btn : m_btns)
        if (x >= btn.x && x < btn.x + btn.w) return btn.cmdId;
    return -1;
}

void MainWindow::HandleToolbarClick(int cmdId) {
    switch (cmdId) {
        case ID_TOOLBAR_ADDFILE:   AddFile();        break;
        case ID_TOOLBAR_ADDFOLDER: AddFolder();      break;
        case ID_TOOLBAR_IMPORT:    ImportTaskbar();  break;
        case ID_TOOLBAR_DELETE:    DeleteSelected(); break;
        case ID_TOOLBAR_SETTINGS:
            SettingsDialog::Show(m_hWnd);
            break;
    }
}

// ── Main window proc ──────────────────────────────────────────────────────────

LRESULT CALLBACK MainWindow::s_WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    MainWindow* self = nullptr;
    if (msg == WM_CREATE) {
        auto* cs = reinterpret_cast<CREATESTRUCTW*>(lParam);
        self = reinterpret_cast<MainWindow*>(cs->lpCreateParams);
        SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(self));
        self->m_hWnd = hWnd;
        self->CreateToolbar();

        RECT rc; GetClientRect(hWnd, &rc);
        self->m_grid.Create(hWnd, cs->hInstance, 0, MainWindow::TOOLBAR_H,
                            rc.right, rc.bottom - MainWindow::TOOLBAR_H);
        self->CreateTooltip();
        self->LoadApps();
        self->LayoutChildren();
        DragAcceptFiles(hWnd, TRUE);
        return 0;
    }
    self = reinterpret_cast<MainWindow*>(GetWindowLongPtrW(hWnd, GWLP_USERDATA));
    if (!self) return DefWindowProcW(hWnd, msg, wParam, lParam);
    return self->WndProc(msg, wParam, lParam);
}

LRESULT MainWindow::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {

    case WM_ERASEBKGND: {
        auto& cfg = ConfigManager::GetInstance()->Config();
        RECT rc; GetClientRect(m_hWnd, &rc);
        HDC hdc = reinterpret_cast<HDC>(wParam);
        HBRUSH hBg = CreateSolidBrush(ThemeManager::BgColor(cfg.theme));
        FillRect(hdc, &rc, hBg);
        DeleteObject(hBg);
        return 1;
    }

    case WM_SIZE:
        LayoutChildren();
        return 0;

    case WM_COMMAND: {
        int id = LOWORD(wParam);
        // Search edit
        if (id == ID_SEARCH_EDIT && HIWORD(wParam) == EN_CHANGE) {
            wchar_t buf[256] = {};
            GetWindowTextW(m_hSearch, buf, 256);
            m_searchText = buf;
            m_grid.ScrollToTop();
            FilterApps();
            return 0;
        }
        // Context menu
        switch (id) {
            case ID_CTX_LAUNCH:        LaunchApp(m_contextTarget);        break;
            case ID_CTX_RUNAS_ADMIN:   LaunchAppAsAdmin(m_contextTarget); break;
            case ID_CTX_OPEN_LOCATION: OpenAppLocation(m_contextTarget);  break;
            case ID_CTX_REMOVE:        RemoveApp(m_contextTarget);        break;
        }
        return 0;
    }

    // AppGrid notifications
    case WMG_LAUNCH:
        LaunchApp(static_cast<int>(wParam));
        return 0;

    case WMG_SELECT:
        SetSelection(static_cast<int>(wParam), lParam != 0);
        return 0;

    case WMG_CONTEXTMENU: {
        int screenX = static_cast<int>(LOWORD(lParam));
        int screenY = static_cast<int>(HIWORD(lParam));
        ShowContextMenu(static_cast<int>(wParam), screenX, screenY);
        return 0;
    }

    case WM_DROPFILES:
        HandleDropFiles(reinterpret_cast<HDROP>(wParam));
        return 0;

    case WM_MOVE:
    case WM_EXITSIZEMOVE:
        SaveWindowRect();
        return 0;

    case WM_DESTROY:
        SaveWindowRect();
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(m_hWnd, msg, wParam, lParam);
}

// ── Toolbar window proc ───────────────────────────────────────────────────────

LRESULT CALLBACK MainWindow::s_ToolbarProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    MainWindow* self = nullptr;
    if (msg == WM_CREATE) {
        auto* cs = reinterpret_cast<CREATESTRUCTW*>(lParam);
        self = reinterpret_cast<MainWindow*>(cs->lpCreateParams);
        SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(self));
        return 0;
    }
    self = reinterpret_cast<MainWindow*>(GetWindowLongPtrW(hWnd, GWLP_USERDATA));
    if (!self) return DefWindowProcW(hWnd, msg, wParam, lParam);
    return self->ToolbarProc(msg, wParam, lParam);
}

LRESULT MainWindow::ToolbarProc(UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {

    case WM_ERASEBKGND:
        return 1;

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(m_hToolbar, &ps);
        RECT rc; GetClientRect(m_hToolbar, &rc);

        HDC     memDC  = CreateCompatibleDC(hdc);
        HBITMAP memBmp = CreateCompatibleBitmap(hdc, rc.right, rc.bottom);
        HGDIOBJ oldBmp = SelectObject(memDC, memBmp);

        PaintToolbar(memDC);

        BitBlt(hdc, 0, 0, rc.right, rc.bottom, memDC, 0, 0, SRCCOPY);
        SelectObject(memDC, oldBmp);
        DeleteObject(memBmp);
        DeleteDC(memDC);

        EndPaint(m_hToolbar, &ps);
        return 0;
    }

    case WM_MOUSEMOVE:
        InvalidateRect(m_hToolbar, nullptr, FALSE);
        return 0;

    case WM_MOUSELEAVE:
        InvalidateRect(m_hToolbar, nullptr, FALSE);
        return 0;

    case WM_LBUTTONUP: {
        int x = GET_X_LPARAM(lParam), y = GET_Y_LPARAM(lParam);
        int cmd = ToolbarHitTest(x, y);
        if (cmd > 0) HandleToolbarClick(cmd);
        return 0;
    }

    case WM_CTLCOLOREDIT: {
        // Style the search edit box for dark mode
        auto& cfg = ConfigManager::GetInstance()->Config();
        if (cfg.theme == ThemeMode::Dark) {
            HDC hdc = reinterpret_cast<HDC>(wParam);
            SetTextColor(hdc, ThemeManager::TextColor(ThemeMode::Dark));
            SetBkColor(hdc,   RGB(0x32, 0x32, 0x32));
            static HBRUSH s_editBrush = nullptr;
            if (!s_editBrush)
                s_editBrush = CreateSolidBrush(RGB(0x32, 0x32, 0x32));
            return reinterpret_cast<LRESULT>(s_editBrush);
        }
        break;
    }

    case WM_COMMAND:
        // Forward to main window (search EN_CHANGE etc.)
        return SendMessageW(m_hWnd, msg, wParam, lParam);

    } // switch
    return DefWindowProcW(m_hToolbar, msg, wParam, lParam);
}

} // namespace QuickLaunchTool
