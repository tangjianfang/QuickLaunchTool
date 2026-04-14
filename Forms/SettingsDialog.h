#pragma once

#include <windows.h>
#include "../Models/AppConfig.h"

namespace QuickLaunchTool {

// Modal settings dialog backed by IDD_SETTINGS dialog resource.
// Returns true if the user clicked OK.
class SettingsDialog {
public:
    // Show the dialog. On OK, writes changes to ConfigManager and calls
    // the provided refresh callback.
    static bool Show(HWND hParent);

private:
    HWND      m_hDlg    = nullptr;
    AppConfig m_working;  // working copy of config, applied on OK

    void   Init();
    void   Apply();
    void   UpdateOpacityLabel();
    void   ApplyThemeToDialog();

    INT_PTR DlgProc(UINT msg, WPARAM wParam, LPARAM lParam);
    static INT_PTR CALLBACK s_DlgProc(HWND hDlg, UINT msg, WPARAM wParam, LPARAM lParam);
};

} // namespace QuickLaunchTool
