#pragma once

#include <string>
#include <map>
#include <windows.h>

namespace QuickLaunchTool {

class LocalizationManager {
public:
    enum class Key {
        AppName,
        Search,
        AddFile,
        AddFolder,
        ImportTaskbar,
        DeleteSelected,
        Settings,
        // Settings dialog labels
        Language,
        SortBy,
        Theme,
        IconSize,
        TopMost,
        Opacity,
        OK,
        Cancel,
        // Sort options
        SortName,
        SortModified,
        SortUseCount,
        // Theme options
        ThemeLight,
        ThemeDark,
        // Icon size options
        SizeLarge,
        SizeMedium,
        SizeSmall,
        // Context menu
        Launch,
        RunAsAdmin,
        OpenLocation,
        Remove,
        // Message boxes
        ConfirmDeleteTitle,
        ConfirmDeleteMsg,
        NoAppsSelectedMsg,
        // File dialogs
        SelectExeTitle,
        SelectFolderTitle,
        // Tooltips
        TipAddFile,
        TipAddFolder,
        TipImport,
        TipDelete,
        TipSettings,
    };

private:
    std::wstring              m_lang;
    std::map<Key, std::wstring> m_strings;
    static LocalizationManager* s_instance;

    LocalizationManager() { SetLanguage(L"zh-CN"); }

public:
    static LocalizationManager* GetInstance() {
        if (!s_instance) s_instance = new LocalizationManager();
        return s_instance;
    }
    static void DestroyInstance() { delete s_instance; s_instance = nullptr; }

    void SetLanguage(const std::wstring& lang) {
        m_lang = lang;
        Load();
    }
    const std::wstring& GetLanguage() const { return m_lang; }

    const std::wstring& Get(Key k) const {
        auto it = m_strings.find(k);
        static const std::wstring empty;
        return it != m_strings.end() ? it->second : empty;
    }

private:
    void Load() {
        m_strings.clear();
        if (m_lang == L"zh-CN") {
            m_strings[Key::AppName]           = L"\u5FEB\u901F\u542F\u52A8\u5DE5\u5177";
            m_strings[Key::Search]            = L"\u641C\u7D22";
            m_strings[Key::AddFile]           = L"\u6DFB\u52A0\u6587\u4EF6";
            m_strings[Key::AddFolder]         = L"\u6DFB\u52A0\u6587\u4EF6\u5939";
            m_strings[Key::ImportTaskbar]     = L"\u5BFC\u5165\u4EFB\u52A1\u680F";
            m_strings[Key::DeleteSelected]    = L"\u5220\u9664\u9009\u4E2D";
            m_strings[Key::Settings]          = L"\u8BBE\u7F6E";
            m_strings[Key::Language]          = L"\u8BED\u8A00";
            m_strings[Key::SortBy]            = L"\u6392\u5217\u65B9\u5F0F";
            m_strings[Key::Theme]             = L"\u4E3B\u9898";
            m_strings[Key::IconSize]          = L"\u56FE\u6807\u5927\u5C0F";
            m_strings[Key::TopMost]           = L"\u7A97\u53E3\u7F6E\u9876";
            m_strings[Key::Opacity]           = L"\u4E0D\u900F\u660E\u5EA6";
            m_strings[Key::OK]                = L"\u786E\u5B9A";
            m_strings[Key::Cancel]            = L"\u53D6\u6D88";
            m_strings[Key::SortName]          = L"\u540D\u79F0";
            m_strings[Key::SortModified]      = L"\u4FEE\u6539\u65F6\u95F4";
            m_strings[Key::SortUseCount]      = L"\u4F7F\u7528\u6B21\u6570";
            m_strings[Key::ThemeLight]        = L"\u6D45\u8272";
            m_strings[Key::ThemeDark]         = L"\u6DF1\u8272";
            m_strings[Key::SizeLarge]         = L"\u5927 (48px)";
            m_strings[Key::SizeMedium]        = L"\u4E2D (32px)";
            m_strings[Key::SizeSmall]         = L"\u5C0F (24px)";
            m_strings[Key::Launch]            = L"\u542F\u52A8";
            m_strings[Key::RunAsAdmin]        = L"\u4EE5\u7BA1\u7406\u5458\u8EAB\u4EFD\u8FD0\u884C";
            m_strings[Key::OpenLocation]      = L"\u6253\u5F00\u6587\u4EF6\u6240\u5728\u4F4D\u7F6E";
            m_strings[Key::Remove]            = L"\u79FB\u9664";
            m_strings[Key::ConfirmDeleteTitle]= L"\u786E\u8BA4\u5220\u9664";
            m_strings[Key::ConfirmDeleteMsg]  = L"\u786E\u8BA4\u8981\u79FB\u9664\u9009\u4E2D\u7684\u5E94\u7528\u5417\uFF1F";
            m_strings[Key::NoAppsSelectedMsg] = L"\u8BF7\u5148\u9009\u62E9\u8981\u79FB\u9664\u7684\u5E94\u7528\u3002";
            m_strings[Key::SelectExeTitle]    = L"\u9009\u62E9\u53EF\u6267\u884C\u6587\u4EF6";
            m_strings[Key::SelectFolderTitle] = L"\u9009\u62E9\u6587\u4EF6\u5939";
            m_strings[Key::TipAddFile]        = L"\u6DFB\u52A0\u6587\u4EF6";
            m_strings[Key::TipAddFolder]      = L"\u6DFB\u52A0\u6587\u4EF6\u5939";
            m_strings[Key::TipImport]         = L"\u5BFC\u5165\u4EFB\u52A1\u680F\u5E94\u7528";
            m_strings[Key::TipDelete]         = L"\u5220\u9664\u9009\u4E2D\u9879";
            m_strings[Key::TipSettings]       = L"\u8BBE\u7F6E";
        } else if (m_lang == L"en-US") {
            m_strings[Key::AppName]           = L"Quick Launch Tool";
            m_strings[Key::Search]            = L"Search";
            m_strings[Key::AddFile]           = L"Add File";
            m_strings[Key::AddFolder]         = L"Add Folder";
            m_strings[Key::ImportTaskbar]     = L"Import Taskbar";
            m_strings[Key::DeleteSelected]    = L"Delete Selected";
            m_strings[Key::Settings]          = L"Settings";
            m_strings[Key::Language]          = L"Language";
            m_strings[Key::SortBy]            = L"Sort By";
            m_strings[Key::Theme]             = L"Theme";
            m_strings[Key::IconSize]          = L"Icon Size";
            m_strings[Key::TopMost]           = L"Always on Top";
            m_strings[Key::Opacity]           = L"Opacity";
            m_strings[Key::OK]                = L"OK";
            m_strings[Key::Cancel]            = L"Cancel";
            m_strings[Key::SortName]          = L"Name";
            m_strings[Key::SortModified]      = L"Modified Date";
            m_strings[Key::SortUseCount]      = L"Use Count";
            m_strings[Key::ThemeLight]        = L"Light";
            m_strings[Key::ThemeDark]         = L"Dark";
            m_strings[Key::SizeLarge]         = L"Large (48px)";
            m_strings[Key::SizeMedium]        = L"Medium (32px)";
            m_strings[Key::SizeSmall]         = L"Small (24px)";
            m_strings[Key::Launch]            = L"Launch";
            m_strings[Key::RunAsAdmin]        = L"Run as Administrator";
            m_strings[Key::OpenLocation]      = L"Open File Location";
            m_strings[Key::Remove]            = L"Remove";
            m_strings[Key::ConfirmDeleteTitle]= L"Confirm Delete";
            m_strings[Key::ConfirmDeleteMsg]  = L"Remove the selected application(s)?";
            m_strings[Key::NoAppsSelectedMsg] = L"Please select an application first.";
            m_strings[Key::SelectExeTitle]    = L"Select Executable File";
            m_strings[Key::SelectFolderTitle] = L"Select Folder";
            m_strings[Key::TipAddFile]        = L"Add File";
            m_strings[Key::TipAddFolder]      = L"Add Folder";
            m_strings[Key::TipImport]         = L"Import from Taskbar";
            m_strings[Key::TipDelete]         = L"Delete Selected";
            m_strings[Key::TipSettings]       = L"Settings";
        } else if (m_lang == L"ja-JP") {
            m_strings[Key::AppName]           = L"\u30AF\u30A4\u30C3\u30AF\u8D77\u52D5\u30C4\u30FC\u30EB";
            m_strings[Key::Search]            = L"\u691C\u7D22";
            m_strings[Key::AddFile]           = L"\u30D5\u30A1\u30A4\u30EB\u8FFD\u52A0";
            m_strings[Key::AddFolder]         = L"\u30D5\u30A9\u30EB\u30C0\u8FFD\u52A0";
            m_strings[Key::ImportTaskbar]     = L"\u30BF\u30B9\u30AF\u30D0\u30FC\u304B\u3089\u30A4\u30F3\u30DD\u30FC\u30C8";
            m_strings[Key::DeleteSelected]    = L"\u9078\u629E\u524A\u9664";
            m_strings[Key::Settings]          = L"\u8A2D\u5B9A";
            m_strings[Key::Language]          = L"\u8A00\u8A9E";
            m_strings[Key::SortBy]            = L"\u4E26\u3073\u9806";
            m_strings[Key::Theme]             = L"\u30C6\u30FC\u30DE";
            m_strings[Key::IconSize]          = L"\u30A2\u30A4\u30B3\u30F3\u30B5\u30A4\u30BA";
            m_strings[Key::TopMost]           = L"\u5E38\u306B\u524D\u9762\u306B\u8868\u793A";
            m_strings[Key::Opacity]           = L"\u4E0D\u900F\u660E\u5EA6";
            m_strings[Key::OK]                = L"OK";
            m_strings[Key::Cancel]            = L"\u30AD\u30E3\u30F3\u30BB\u30EB";
            m_strings[Key::SortName]          = L"\u540D\u524D";
            m_strings[Key::SortModified]      = L"\u66F4\u65B0\u65E5\u6642";
            m_strings[Key::SortUseCount]      = L"\u4F7F\u7528\u56DE\u6570";
            m_strings[Key::ThemeLight]        = L"\u30E9\u30A4\u30C8";
            m_strings[Key::ThemeDark]         = L"\u30C0\u30FC\u30AF";
            m_strings[Key::SizeLarge]         = L"\u5927 (48px)";
            m_strings[Key::SizeMedium]        = L"\u4E2D (32px)";
            m_strings[Key::SizeSmall]         = L"\u5C0F (24px)";
            m_strings[Key::Launch]            = L"\u8D77\u52D5";
            m_strings[Key::RunAsAdmin]        = L"\u7BA1\u7406\u8005\u3068\u3057\u3066\u5B9F\u884C";
            m_strings[Key::OpenLocation]      = L"\u30D5\u30A1\u30A4\u30EB\u306E\u5834\u6240\u3092\u958B\u304F";
            m_strings[Key::Remove]            = L"\u524A\u9664";
            m_strings[Key::ConfirmDeleteTitle]= L"\u524A\u9664\u78BA\u8A8D";
            m_strings[Key::ConfirmDeleteMsg]  = L"\u9078\u629E\u3057\u305F\u30A2\u30D7\u30EA\u3092\u524A\u9664\u3057\u307E\u3059\u304B\uFF1F";
            m_strings[Key::NoAppsSelectedMsg] = L"\u524A\u9664\u3059\u308B\u30A2\u30D7\u30EA\u3092\u9078\u629E\u3057\u3066\u304F\u3060\u3055\u3044\u3002";
            m_strings[Key::SelectExeTitle]    = L"\u5B9F\u884C\u30D5\u30A1\u30A4\u30EB\u3092\u9078\u629E";
            m_strings[Key::SelectFolderTitle] = L"\u30D5\u30A9\u30EB\u30C0\u3092\u9078\u629E";
            m_strings[Key::TipAddFile]        = m_strings[Key::AddFile];
            m_strings[Key::TipAddFolder]      = m_strings[Key::AddFolder];
            m_strings[Key::TipImport]         = m_strings[Key::ImportTaskbar];
            m_strings[Key::TipDelete]         = m_strings[Key::DeleteSelected];
            m_strings[Key::TipSettings]       = m_strings[Key::Settings];
        } else if (m_lang == L"ko-KR") {
            m_strings[Key::AppName]           = L"\uBE60\uB978 \uC2E4\uD589 \uB3C4\uAD6C";
            m_strings[Key::Search]            = L"\uAC80\uC0C9";
            m_strings[Key::AddFile]           = L"\uD30C\uC77C \uCD94\uAC00";
            m_strings[Key::AddFolder]         = L"\uD3F4\uB354 \uCD94\uAC00";
            m_strings[Key::ImportTaskbar]     = L"\uC791\uC5C5 \uD45C\uC2DC\uC904 \uAC00\uC838\uC624\uAE30";
            m_strings[Key::DeleteSelected]    = L"\uC120\uD0DD \uC0AD\uC81C";
            m_strings[Key::Settings]          = L"\uC124\uC815";
            m_strings[Key::Language]          = L"\uC5B8\uC5B4";
            m_strings[Key::SortBy]            = L"\uC815\uB82C \uBC29\uC2DD";
            m_strings[Key::Theme]             = L"\uD14C\uB9C8";
            m_strings[Key::IconSize]          = L"\uC544\uC774\uCF58 \uD06C\uAE30";
            m_strings[Key::TopMost]           = L"\uD56D\uC0C1 \uC704\uC5D0 \uD45C\uC2DC";
            m_strings[Key::Opacity]           = L"\uBD88\uD22C\uBA85\uB3C4";
            m_strings[Key::OK]                = L"\uD655\uC778";
            m_strings[Key::Cancel]            = L"\uCDE8\uC18C";
            m_strings[Key::SortName]          = L"\uC774\uB984";
            m_strings[Key::SortModified]      = L"\uC218\uC815\uC77C";
            m_strings[Key::SortUseCount]      = L"\uC0AC\uC6A9 \uD69F\uC218";
            m_strings[Key::ThemeLight]        = L"\uBC1D\uC740 \uD14C\uB9C8";
            m_strings[Key::ThemeDark]         = L"\uC5B4\uB450\uC6B4 \uD14C\uB9C8";
            m_strings[Key::SizeLarge]         = L"\uD06C\uAC8C (48px)";
            m_strings[Key::SizeMedium]        = L"\uC911\uAC04 (32px)";
            m_strings[Key::SizeSmall]         = L"\uC791\uAC8C (24px)";
            m_strings[Key::Launch]            = L"\uC2E4\uD589";
            m_strings[Key::RunAsAdmin]        = L"\uAD00\uB9AC\uC790 \uAD8C\uD55C\uC73C\uB85C \uC2E4\uD589";
            m_strings[Key::OpenLocation]      = L"\uD30C\uC77C \uC704\uCE58 \uC5F4\uAE30";
            m_strings[Key::Remove]            = L"\uC81C\uAC70";
            m_strings[Key::ConfirmDeleteTitle]= L"\uC0AD\uC81C \uD655\uC778";
            m_strings[Key::ConfirmDeleteMsg]  = L"\uC120\uD0DD\uD55C \uC571\uC744 \uC81C\uAC70\uD558\uACA0\uC2B5\uB2C8\uAE4C?";
            m_strings[Key::NoAppsSelectedMsg] = L"\uC81C\uAC70\uD560 \uC571\uC744 \uBA3C\uC800 \uC120\uD0DD\uD558\uC138\uC694.";
            m_strings[Key::SelectExeTitle]    = L"\uC2E4\uD589 \uD30C\uC77C \uC120\uD0DD";
            m_strings[Key::SelectFolderTitle] = L"\uD3F4\uB354 \uC120\uD0DD";
            m_strings[Key::TipAddFile]        = m_strings[Key::AddFile];
            m_strings[Key::TipAddFolder]      = m_strings[Key::AddFolder];
            m_strings[Key::TipImport]         = m_strings[Key::ImportTaskbar];
            m_strings[Key::TipDelete]         = m_strings[Key::DeleteSelected];
            m_strings[Key::TipSettings]       = m_strings[Key::Settings];
        } else if (m_lang == L"fr-FR") {
            m_strings[Key::AppName]           = L"Outil de lancement rapide";
            m_strings[Key::Search]            = L"Rechercher";
            m_strings[Key::AddFile]           = L"Ajouter fichier";
            m_strings[Key::AddFolder]         = L"Ajouter dossier";
            m_strings[Key::ImportTaskbar]     = L"Importer barre des t\u00E2ches";
            m_strings[Key::DeleteSelected]    = L"Supprimer s\u00E9lection";
            m_strings[Key::Settings]          = L"Param\u00E8tres";
            m_strings[Key::Language]          = L"Langue";
            m_strings[Key::SortBy]            = L"Trier par";
            m_strings[Key::Theme]             = L"Th\u00E8me";
            m_strings[Key::IconSize]          = L"Taille des ic\u00F4nes";
            m_strings[Key::TopMost]           = L"Toujours au premier plan";
            m_strings[Key::Opacity]           = L"Opacit\u00E9";
            m_strings[Key::OK]                = L"OK";
            m_strings[Key::Cancel]            = L"Annuler";
            m_strings[Key::SortName]          = L"Nom";
            m_strings[Key::SortModified]      = L"Date de modification";
            m_strings[Key::SortUseCount]      = L"Nombre d'utilisations";
            m_strings[Key::ThemeLight]        = L"Clair";
            m_strings[Key::ThemeDark]         = L"Sombre";
            m_strings[Key::SizeLarge]         = L"Grand (48px)";
            m_strings[Key::SizeMedium]        = L"Moyen (32px)";
            m_strings[Key::SizeSmall]         = L"Petit (24px)";
            m_strings[Key::Launch]            = L"Lancer";
            m_strings[Key::RunAsAdmin]        = L"Ex\u00E9cuter en tant qu'administrateur";
            m_strings[Key::OpenLocation]      = L"Ouvrir l'emplacement du fichier";
            m_strings[Key::Remove]            = L"Supprimer";
            m_strings[Key::ConfirmDeleteTitle]= L"Confirmer la suppression";
            m_strings[Key::ConfirmDeleteMsg]  = L"Supprimer les applications s\u00E9lectionn\u00E9es\u00A0?";
            m_strings[Key::NoAppsSelectedMsg] = L"Veuillez d'abord s\u00E9lectionner une application.";
            m_strings[Key::SelectExeTitle]    = L"S\u00E9lectionner un fichier ex\u00E9cutable";
            m_strings[Key::SelectFolderTitle] = L"S\u00E9lectionner un dossier";
            m_strings[Key::TipAddFile]        = m_strings[Key::AddFile];
            m_strings[Key::TipAddFolder]      = m_strings[Key::AddFolder];
            m_strings[Key::TipImport]         = m_strings[Key::ImportTaskbar];
            m_strings[Key::TipDelete]         = m_strings[Key::DeleteSelected];
            m_strings[Key::TipSettings]       = m_strings[Key::Settings];
        } else if (m_lang == L"de-DE") {
            m_strings[Key::AppName]           = L"Schnellstart-Tool";
            m_strings[Key::Search]            = L"Suchen";
            m_strings[Key::AddFile]           = L"Datei hinzuf\u00FCgen";
            m_strings[Key::AddFolder]         = L"Ordner hinzuf\u00FCgen";
            m_strings[Key::ImportTaskbar]     = L"Taskleiste importieren";
            m_strings[Key::DeleteSelected]    = L"Ausgew\u00E4hlte l\u00F6schen";
            m_strings[Key::Settings]          = L"Einstellungen";
            m_strings[Key::Language]          = L"Sprache";
            m_strings[Key::SortBy]            = L"Sortieren nach";
            m_strings[Key::Theme]             = L"Design";
            m_strings[Key::IconSize]          = L"Symbolgr\u00F6\u00DFe";
            m_strings[Key::TopMost]           = L"Immer im Vordergrund";
            m_strings[Key::Opacity]           = L"Transparenz";
            m_strings[Key::OK]                = L"OK";
            m_strings[Key::Cancel]            = L"Abbrechen";
            m_strings[Key::SortName]          = L"Name";
            m_strings[Key::SortModified]      = L"\u00C4nderungsdatum";
            m_strings[Key::SortUseCount]      = L"Verwendungen";
            m_strings[Key::ThemeLight]        = L"Hell";
            m_strings[Key::ThemeDark]         = L"Dunkel";
            m_strings[Key::SizeLarge]         = L"Gro\u00DF (48px)";
            m_strings[Key::SizeMedium]        = L"Mittel (32px)";
            m_strings[Key::SizeSmall]         = L"Klein (24px)";
            m_strings[Key::Launch]            = L"Starten";
            m_strings[Key::RunAsAdmin]        = L"Als Administrator ausf\u00FChren";
            m_strings[Key::OpenLocation]      = L"Dateispeicherort \u00F6ffnen";
            m_strings[Key::Remove]            = L"Entfernen";
            m_strings[Key::ConfirmDeleteTitle]= L"L\u00F6schen best\u00E4tigen";
            m_strings[Key::ConfirmDeleteMsg]  = L"Ausgew\u00E4hlte Anwendungen entfernen?";
            m_strings[Key::NoAppsSelectedMsg] = L"Bitte zuerst eine Anwendung ausw\u00E4hlen.";
            m_strings[Key::SelectExeTitle]    = L"Ausf\u00FChrbare Datei ausw\u00E4hlen";
            m_strings[Key::SelectFolderTitle] = L"Ordner ausw\u00E4hlen";
            m_strings[Key::TipAddFile]        = m_strings[Key::AddFile];
            m_strings[Key::TipAddFolder]      = m_strings[Key::AddFolder];
            m_strings[Key::TipImport]         = m_strings[Key::ImportTaskbar];
            m_strings[Key::TipDelete]         = m_strings[Key::DeleteSelected];
            m_strings[Key::TipSettings]       = m_strings[Key::Settings];
        } else if (m_lang == L"es-ES") {
            m_strings[Key::AppName]           = L"Herramienta de inicio r\u00E1pido";
            m_strings[Key::Search]            = L"Buscar";
            m_strings[Key::AddFile]           = L"A\u00F1adir archivo";
            m_strings[Key::AddFolder]         = L"A\u00F1adir carpeta";
            m_strings[Key::ImportTaskbar]     = L"Importar barra de tareas";
            m_strings[Key::DeleteSelected]    = L"Eliminar selecci\u00F3n";
            m_strings[Key::Settings]          = L"Configuraci\u00F3n";
            m_strings[Key::Language]          = L"Idioma";
            m_strings[Key::SortBy]            = L"Ordenar por";
            m_strings[Key::Theme]             = L"Tema";
            m_strings[Key::IconSize]          = L"Tama\u00F1o de icono";
            m_strings[Key::TopMost]           = L"Siempre encima";
            m_strings[Key::Opacity]           = L"Opacidad";
            m_strings[Key::OK]                = L"Aceptar";
            m_strings[Key::Cancel]            = L"Cancelar";
            m_strings[Key::SortName]          = L"Nombre";
            m_strings[Key::SortModified]      = L"Fecha de modificaci\u00F3n";
            m_strings[Key::SortUseCount]      = L"N\u00FAm. de usos";
            m_strings[Key::ThemeLight]        = L"Claro";
            m_strings[Key::ThemeDark]         = L"Oscuro";
            m_strings[Key::SizeLarge]         = L"Grande (48px)";
            m_strings[Key::SizeMedium]        = L"Mediano (32px)";
            m_strings[Key::SizeSmall]         = L"Peque\u00F1o (24px)";
            m_strings[Key::Launch]            = L"Iniciar";
            m_strings[Key::RunAsAdmin]        = L"Ejecutar como administrador";
            m_strings[Key::OpenLocation]      = L"Abrir ubicaci\u00F3n del archivo";
            m_strings[Key::Remove]            = L"Eliminar";
            m_strings[Key::ConfirmDeleteTitle]= L"Confirmar eliminaci\u00F3n";
            m_strings[Key::ConfirmDeleteMsg]  = L"\u00BFEliminar las aplicaciones seleccionadas?";
            m_strings[Key::NoAppsSelectedMsg] = L"Seleccione primero una aplicaci\u00F3n.";
            m_strings[Key::SelectExeTitle]    = L"Seleccionar archivo ejecutable";
            m_strings[Key::SelectFolderTitle] = L"Seleccionar carpeta";
            m_strings[Key::TipAddFile]        = m_strings[Key::AddFile];
            m_strings[Key::TipAddFolder]      = m_strings[Key::AddFolder];
            m_strings[Key::TipImport]         = m_strings[Key::ImportTaskbar];
            m_strings[Key::TipDelete]         = m_strings[Key::DeleteSelected];
            m_strings[Key::TipSettings]       = m_strings[Key::Settings];
        } else {
            m_lang = L"zh-CN";
            Load();
        }
    }
};

} // namespace QuickLaunchTool
