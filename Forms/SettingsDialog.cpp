#include "SettingsDialog.h"
#include <commctrl.h>
#include <uxtheme.h>
#include "../Resources/Resource.h"
#include "../Services/ConfigManager.h"
#include "../Utils/LocalizationManager.h"
#include "../Utils/ThemeManager.h"

#pragma comment(lib, "uxtheme.lib")

// Forward declaration (MainWindow.cpp provides this)
namespace QuickLaunchTool { class MainWindow; }
extern void RefreshMainWindow();

namespace QuickLaunchTool {

// ── Public entry point ────────────────────────────────────────────────────────

bool SettingsDialog::Show(HWND hParent) {
    SettingsDialog dlg;
    // Copy current config as working state
    dlg.m_working = ConfigManager::GetInstance()->Config();

    INT_PTR result = DialogBoxParamW(
        GetModuleHandleW(nullptr),
        MAKEINTRESOURCEW(IDD_SETTINGS),
        hParent,
        s_DlgProc,
        reinterpret_cast<LPARAM>(&dlg));

    return result == IDOK;
}

// ── Dialog proc ───────────────────────────────────────────────────────────────

INT_PTR CALLBACK SettingsDialog::s_DlgProc(HWND hDlg, UINT msg,
                                            WPARAM wParam, LPARAM lParam) {
    SettingsDialog* self = nullptr;
    if (msg == WM_INITDIALOG) {
        self = reinterpret_cast<SettingsDialog*>(lParam);
        self->m_hDlg = hDlg;
        SetWindowLongPtrW(hDlg, DWLP_USER, reinterpret_cast<LONG_PTR>(self));
        self->Init();
        return TRUE;
    }
    self = reinterpret_cast<SettingsDialog*>(GetWindowLongPtrW(hDlg, DWLP_USER));
    if (!self) return FALSE;
    return static_cast<INT_PTR>(self->DlgProc(msg, wParam, lParam));
}

INT_PTR SettingsDialog::DlgProc(UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {

    case WM_COMMAND:
        switch (LOWORD(wParam)) {
        case IDOK:
            Apply();
            EndDialog(m_hDlg, IDOK);
            RefreshMainWindow();
            return TRUE;
        case IDCANCEL:
            EndDialog(m_hDlg, IDCANCEL);
            return TRUE;
        }
        break;

    case WM_HSCROLL:
        if (reinterpret_cast<HWND>(lParam) == GetDlgItem(m_hDlg, IDC_SLIDER_OPACITY))
            UpdateOpacityLabel();
        break;

    // ── Dark-mode coloring (always dark) ─────────────────────────────
    case WM_CTLCOLORDLG:
    case WM_CTLCOLORSTATIC: {
        HDC hdc = reinterpret_cast<HDC>(wParam);
        SetTextColor(hdc, ThemeManager::TextColor(ThemeMode::Dark));
        SetBkColor(hdc,   ThemeManager::BgColor(ThemeMode::Dark));
        static HBRUSH s_dlgBrush = CreateSolidBrush(ThemeManager::BgColor(ThemeMode::Dark));
        return reinterpret_cast<INT_PTR>(s_dlgBrush);
    }

    // Dropdown list background (the open list part of each ComboBox)
    case WM_CTLCOLORLISTBOX: {
        HDC hdc = reinterpret_cast<HDC>(wParam);
        SetTextColor(hdc, ThemeManager::TextColor(ThemeMode::Dark));
        SetBkColor(hdc,   RGB(0x2C, 0x2C, 0x2C));
        static HBRUSH s_listBrush = CreateSolidBrush(RGB(0x2C, 0x2C, 0x2C));
        return reinterpret_cast<INT_PTR>(s_listBrush);
    }

    } // switch
    return FALSE;
}

// ── Init (WM_INITDIALOG) ──────────────────────────────────────────────────────

void SettingsDialog::Init() {
    auto* loc = LocalizationManager::GetInstance();

    // ── Window title & labels ────────────────────────────────────────────
    SetWindowTextW(m_hDlg, loc->Get(LocalizationManager::Key::Settings).c_str());
    SetDlgItemTextW(m_hDlg, IDC_LABEL_LANGUAGE, loc->Get(LocalizationManager::Key::Language).c_str());
    SetDlgItemTextW(m_hDlg, IDC_LABEL_SORT,     loc->Get(LocalizationManager::Key::SortBy).c_str());
    SetDlgItemTextW(m_hDlg, IDC_LABEL_ICONSIZE, loc->Get(LocalizationManager::Key::IconSize).c_str());
    SetDlgItemTextW(m_hDlg, IDC_LABEL_OPACITY,  loc->Get(LocalizationManager::Key::Opacity).c_str());
    SetDlgItemTextW(m_hDlg, IDC_STATIC_OPACITY_VAL, L"");

    // Apply dark title bar if needed
    ThemeManager::ApplyDarkTitleBar(m_hDlg, m_working.theme == ThemeMode::Dark);

    // ── Language combo ───────────────────────────────────────────────────
    HWND hLang = GetDlgItem(m_hDlg, IDC_COMBO_LANGUAGE);
    struct { const wchar_t* code; const wchar_t* label; } langs[] = {
        {L"zh-CN", L"\u4E2D\u6587 (\u7B80\u4F53)"},
        {L"en-US", L"English"},
        {L"ja-JP", L"\u65E5\u672C\u8A9E"},
        {L"ko-KR", L"\uD55C\uAD6D\uC5B4"},
        {L"fr-FR", L"Fran\u00E7ais"},
        {L"de-DE", L"Deutsch"},
        {L"es-ES", L"Espa\u00F1ol"},
    };
    int selLang = 0;
    for (int i = 0; i < 7; ++i) {
        SendMessageW(hLang, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(langs[i].label));
        SendMessageW(hLang, CB_SETITEMDATA, i, reinterpret_cast<LPARAM>(langs[i].code));
        if (m_working.language == langs[i].code) selLang = i;
    }
    SendMessageW(hLang, CB_SETCURSEL, selLang, 0);

    // ── Sort mode combo ──────────────────────────────────────────────────
    HWND hSort = GetDlgItem(m_hDlg, IDC_COMBO_SORT);
    SendMessageW(hSort, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(loc->Get(LocalizationManager::Key::SortName).c_str()));
    SendMessageW(hSort, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(loc->Get(LocalizationManager::Key::SortModified).c_str()));
    SendMessageW(hSort, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(loc->Get(LocalizationManager::Key::SortUseCount).c_str()));
    SendMessageW(hSort, CB_SETCURSEL, static_cast<int>(m_working.sortMode), 0);

    // ── Icon size combo ──────────────────────────────────────────────────
    HWND hSize = GetDlgItem(m_hDlg, IDC_COMBO_ICONSIZE);
    SendMessageW(hSize, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(loc->Get(LocalizationManager::Key::SizeLarge).c_str()));
    SendMessageW(hSize, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(loc->Get(LocalizationManager::Key::SizeMedium).c_str()));
    SendMessageW(hSize, CB_ADDSTRING, 0, reinterpret_cast<LPARAM>(loc->Get(LocalizationManager::Key::SizeSmall).c_str()));
    SendMessageW(hSize, CB_SETCURSEL, static_cast<int>(m_working.iconSize), 0);

    // ── Apply dark Explorer theme to all combo boxes ─────────────────────
    SetWindowTheme(hLang,  L"DarkMode_Explorer", nullptr);
    SetWindowTheme(hSort,  L"DarkMode_Explorer", nullptr);
    SetWindowTheme(hSize,  L"DarkMode_Explorer", nullptr);

    // ── Opacity slider ───────────────────────────────────────────────────
    HWND hSlider = GetDlgItem(m_hDlg, IDC_SLIDER_OPACITY);
    SendMessageW(hSlider, TBM_SETRANGE, FALSE, MAKELPARAM(10, 100));
    SendMessageW(hSlider, TBM_SETTICFREQ, 10, 0);
    int opVal = static_cast<int>(m_working.opacity * 100.0);
    SendMessageW(hSlider, TBM_SETPOS, TRUE, opVal);
    UpdateOpacityLabel();

    // ── Always on top checkbox ───────────────────────────────────────────
    HWND hTop = GetDlgItem(m_hDlg, IDC_CHECK_TOPMOST);
    SetWindowTextW(hTop, loc->Get(LocalizationManager::Key::TopMost).c_str());
    SendMessageW(hTop, BM_SETCHECK, m_working.topMost ? BST_CHECKED : BST_UNCHECKED, 0);

    // ── OK / Cancel ──────────────────────────────────────────────────────
    SetDlgItemTextW(m_hDlg, IDOK,     loc->Get(LocalizationManager::Key::OK).c_str());
    SetDlgItemTextW(m_hDlg, IDCANCEL, loc->Get(LocalizationManager::Key::Cancel).c_str());
}

// ── Apply (IDOK) ──────────────────────────────────────────────────────────────

void SettingsDialog::Apply() {
    // Language
    HWND hLang = GetDlgItem(m_hDlg, IDC_COMBO_LANGUAGE);
    int  iLang = static_cast<int>(SendMessageW(hLang, CB_GETCURSEL, 0, 0));
    if (iLang != CB_ERR) {
        // Retrieve the code stored as item data (pointer to static string literal)
        const wchar_t* code = reinterpret_cast<const wchar_t*>(
            SendMessageW(hLang, CB_GETITEMDATA, iLang, 0));
        if (code) m_working.language = code;
    }

    m_working.sortMode = static_cast<SortMode>(
        SendMessageW(GetDlgItem(m_hDlg, IDC_COMBO_SORT), CB_GETCURSEL, 0, 0));
    m_working.iconSize = static_cast<IconSize>(
        SendMessageW(GetDlgItem(m_hDlg, IDC_COMBO_ICONSIZE), CB_GETCURSEL, 0, 0));

    LRESULT opPos = SendMessageW(GetDlgItem(m_hDlg, IDC_SLIDER_OPACITY), TBM_GETPOS, 0, 0);
    m_working.opacity = static_cast<double>(opPos) / 100.0;

    m_working.topMost = (SendMessageW(GetDlgItem(m_hDlg, IDC_CHECK_TOPMOST),
                                      BM_GETCHECK, 0, 0) == BST_CHECKED);

    // Write back to config (preserve window geometry)
    AppConfig& cfg     = ConfigManager::GetInstance()->Config();
    cfg.language       = m_working.language;
    cfg.sortMode       = m_working.sortMode;
    cfg.theme          = ThemeMode::Dark;   // always dark
    cfg.iconSize       = m_working.iconSize;
    cfg.opacity        = m_working.opacity;
    cfg.topMost        = m_working.topMost;
    ConfigManager::GetInstance()->Save();

    // Update localization language
    LocalizationManager::GetInstance()->SetLanguage(cfg.language);
}

void SettingsDialog::UpdateOpacityLabel() {
    LRESULT pos = SendMessageW(GetDlgItem(m_hDlg, IDC_SLIDER_OPACITY), TBM_GETPOS, 0, 0);
    wchar_t buf[16];
    swprintf_s(buf, L"%d%%", static_cast<int>(pos));
    SetDlgItemTextW(m_hDlg, IDC_STATIC_OPACITY_VAL, buf);
}

} // namespace QuickLaunchTool
