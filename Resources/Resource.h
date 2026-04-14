#pragma once

#ifndef IDC_STATIC
#define IDC_STATIC (-1)
#endif

// App icon
#define IDI_APP_ICON            1

// Settings dialog
#define IDD_SETTINGS            100

// Settings dialog control IDs
#define IDC_COMBO_LANGUAGE      1010
#define IDC_COMBO_SORT          1011
#define IDC_COMBO_THEME         1012
#define IDC_COMBO_ICONSIZE      1013
#define IDC_SLIDER_OPACITY      1014
#define IDC_STATIC_OPACITY_VAL  1015
#define IDC_CHECK_TOPMOST       1016
// Settings dialog row labels
#define IDC_LABEL_LANGUAGE      1020
#define IDC_LABEL_SORT          1021
#define IDC_LABEL_ICONSIZE      1022
#define IDC_LABEL_OPACITY       1023

// Toolbar / main window command IDs
#define ID_TOOLBAR_ADDFILE      2001
#define ID_TOOLBAR_ADDFOLDER    2002
#define ID_TOOLBAR_IMPORT       2003
#define ID_TOOLBAR_DELETE       2004
#define ID_TOOLBAR_SETTINGS     2005
#define ID_SEARCH_EDIT          2006

// Context menu IDs
#define ID_CTX_LAUNCH           3001
#define ID_CTX_RUNAS_ADMIN      3002
#define ID_CTX_OPEN_LOCATION    3003
#define ID_CTX_REMOVE           3004

// Custom window messages (AppGrid → parent)
#define WMG_LAUNCH              (WM_APP + 1)   // wParam = filteredIndex
#define WMG_SELECT              (WM_APP + 2)   // wParam = filteredIndex, lParam = ctrl-held
#define WMG_CONTEXTMENU         (WM_APP + 3)   // wParam = filteredIndex, lParam = POINT (screen)
