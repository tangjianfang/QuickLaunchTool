#pragma once

#include <string>
#include <windows.h>
#include <shellapi.h>
#include <commoncontrols.h>
#include <commctrl.h>

#pragma comment(lib, "comctl32.lib")

namespace QuickLaunchTool {

// Extracts application icons at the requested pixel size.
// Uses the system image list so icons are always rendered at native quality.
class IconExtractor {
public:
    // Extract icon from an EXE/LNK/any shell item.
    // Returns nullptr on failure; caller must DestroyIcon.
    static HICON Extract(const std::wstring& path, int sizePx) {
        SHFILEINFOW sfi = {};
        UINT flags = SHGFI_SYSICONINDEX;

        bool exists = (GetFileAttributesW(path.c_str()) != INVALID_FILE_ATTRIBUTES);
        if (!exists) {
            flags |= SHGFI_USEFILEATTRIBUTES;
        }

        if (!SHGetFileInfoW(path.c_str(), FILE_ATTRIBUTE_NORMAL, &sfi, sizeof(sfi), flags)) {
            return GetDefaultIcon(sizePx);
        }

        // Pick the best system image list size
        int shil = BestShilSize(sizePx);

        IImageList* pIL = nullptr;
        if (FAILED(SHGetImageList(shil, IID_PPV_ARGS(&pIL))) || !pIL) {
            return GetDefaultIcon(sizePx);
        }

        HICON hIcon = nullptr;
        pIL->GetIcon(sfi.iIcon, ILD_TRANSPARENT, &hIcon);
        pIL->Release();

        // If the image list size doesn't exactly match what we want, rescale
        if (hIcon && shil != SHIL_LARGE && sizePx != ShilToPixels(shil)) {
            hIcon = ResizeIcon(hIcon, sizePx);
        }

        return hIcon ? hIcon : GetDefaultIcon(sizePx);
    }

    // Generic file/folder icon at the given size.
    // Returns nullptr on failure; caller must DestroyIcon.
    static HICON GetDefaultIcon(int sizePx) {
        SHFILEINFOW sfi = {};
        UINT flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
        flags |= (sizePx >= 32) ? SHGFI_LARGEICON : SHGFI_SMALLICON;
        SHGetFileInfoW(L"*.exe", FILE_ATTRIBUTE_NORMAL, &sfi, sizeof(sfi), flags);
        return sfi.hIcon; // may be nullptr
    }

private:
    static int BestShilSize(int sizePx) {
        if (sizePx >= 48) return SHIL_EXTRALARGE; // 48x48
        if (sizePx >= 32) return SHIL_LARGE;      // 32x32
        return SHIL_SMALL;                         // 16x16
    }

    static int ShilToPixels(int shil) {
        switch (shil) {
            case SHIL_EXTRALARGE: return 48;
            case SHIL_LARGE:      return 32;
            default:              return 16;
        }
    }

    // Scale an HICON to the exact pixel size by drawing into a new bitmap.
    static HICON ResizeIcon(HICON hSrc, int size) {
        HDC hScreen = GetDC(nullptr);
        HDC hDC     = CreateCompatibleDC(hScreen);

        BITMAPINFOHEADER bih = {};
        bih.biSize        = sizeof(bih);
        bih.biWidth       = size;
        bih.biHeight      = -size; // top-down
        bih.biPlanes      = 1;
        bih.biBitCount    = 32;
        bih.biCompression = BI_RGB;

        void* pBits = nullptr;
        HBITMAP hBmp = CreateDIBSection(hDC, reinterpret_cast<BITMAPINFO*>(&bih),
                                        DIB_RGB_COLORS, &pBits, nullptr, 0);
        HGDIOBJ old = SelectObject(hDC, hBmp);

        DrawIconEx(hDC, 0, 0, hSrc, size, size, 0, nullptr, DI_NORMAL);

        SelectObject(hDC, old);
        DeleteDC(hDC);
        ReleaseDC(nullptr, hScreen);
        DestroyIcon(hSrc);

        // Build mask bitmap (all black = fully opaque)
        HBITMAP hMask = CreateBitmap(size, size, 1, 1, nullptr);

        ICONINFO ii = {};
        ii.fIcon    = TRUE;
        ii.hbmColor = hBmp;
        ii.hbmMask  = hMask;
        HICON hNew = CreateIconIndirect(&ii);

        DeleteObject(hBmp);
        DeleteObject(hMask);
        return hNew;
    }
};

} // namespace QuickLaunchTool
