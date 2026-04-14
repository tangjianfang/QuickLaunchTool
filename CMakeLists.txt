cmake_minimum_required(VERSION 3.20)
project(QuickLaunchToolCpp LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ── Compile definitions ───────────────────────────────────────────────────────
add_compile_definitions(
    UNICODE
    _UNICODE
    NOMINMAX
    WIN32_LEAN_AND_MEAN
    _WIN32_WINNT=0x0A00   # Windows 10+
    WINVER=0x0A00
)

add_compile_options(
    /utf-8      # Source and execution charset = UTF-8
    /W3
    /MP         # Parallel compilation
)

# ── Sources ───────────────────────────────────────────────────────────────────
set(SOURCES
    Main.cpp
    Statics.cpp
    Forms/MainWindow.cpp
    Forms/SettingsDialog.cpp
    Controls/AppGrid.cpp
    Resources/Resource.rc
)

add_executable(QuickLaunchToolCpp WIN32 ${SOURCES})

# ── Includes ──────────────────────────────────────────────────────────────────
target_include_directories(QuickLaunchToolCpp PRIVATE ${CMAKE_SOURCE_DIR})

# ── System libraries ──────────────────────────────────────────────────────────
target_link_libraries(QuickLaunchToolCpp PRIVATE
    comctl32
    shell32
    ole32
    shlwapi
    dwmapi
    uxtheme
    comdlg32
)

# ── Output directory ──────────────────────────────────────────────────────────
set_target_properties(QuickLaunchToolCpp PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY_DEBUG   "${CMAKE_SOURCE_DIR}/bin/Debug"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_SOURCE_DIR}/bin/Release"
)

# ── Manifest: provided in Resource.rc; disable linker auto-manifest ──────────
# Prevents CVT1100 "duplicate resource" error (MANIFEST id:1 conflict)
target_link_options(QuickLaunchToolCpp PRIVATE /MANIFEST:NO)
