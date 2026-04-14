#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>
#include <commctrl.h>
#include "Forms/MainWindow.h"
#include "Services/ConfigManager.h"
#include "Utils/LocalizationManager.h"

// Request high-performance GPU on laptops with dual GPUs
extern "C" {
    __declspec(dllexport) DWORD NvOptimusEnablement                = 1;
    __declspec(dllexport) int   AmdPowerXpressRequestHighPerformance = 1;
}

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE, LPWSTR, int) {
    // DPI awareness (also declared in manifest; belt-and-suspenders)
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    // Init COM (required for IShellLink, SHGetImageList, etc.)
    HRESULT hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
    if (FAILED(hr)) return 1;

    // Init Common Controls v6
    INITCOMMONCONTROLSEX icex = { sizeof(icex),
        ICC_STANDARD_CLASSES | ICC_UPDOWN_CLASS | ICC_BAR_CLASSES };
    InitCommonControlsEx(&icex);

    {
        QuickLaunchTool::MainWindow mainWindow;
        if (mainWindow.Create(hInstance))
            mainWindow.Run();
    }

    QuickLaunchTool::ConfigManager::DestroyInstance();
    QuickLaunchTool::LocalizationManager::DestroyInstance();

    CoUninitialize();
    return 0;
}
