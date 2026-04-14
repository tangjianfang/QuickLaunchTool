#pragma once

#include <string>
#include <vector>
#include <map>
#include <algorithm>

namespace QuickLaunchTool {

// Minimal hand-rolled JSON parser/builder. Handles flat objects with
// string, int, double, bool, string-array, and string-int-map values.
class JsonHelper {
public:
    // ── Escaping ──────────────────────────────────────────────────────────

    static std::wstring Escape(const std::wstring& s) {
        std::wstring out;
        out.reserve(s.size() + 4);
        for (wchar_t c : s) {
            switch (c) {
                case L'"':  out += L"\\\""; break;
                case L'\\': out += L"\\\\"; break;
                case L'\n': out += L"\\n";  break;
                case L'\r': out += L"\\r";  break;
                case L'\t': out += L"\\t";  break;
                default:    out += c;       break;
            }
        }
        return out;
    }

    static std::wstring Unescape(const std::wstring& s) {
        std::wstring out;
        out.reserve(s.size());
        for (size_t i = 0; i < s.size(); ++i) {
            if (s[i] == L'\\' && i + 1 < s.size()) {
                switch (s[++i]) {
                    case L'"':  out += L'"';  break;
                    case L'\\': out += L'\\'; break;
                    case L'n':  out += L'\n'; break;
                    case L'r':  out += L'\r'; break;
                    case L't':  out += L'\t'; break;
                    default:    out += s[i];  break;
                }
            } else {
                out += s[i];
            }
        }
        return out;
    }

    // ── Extraction ────────────────────────────────────────────────────────

    static std::wstring ExtractString(const std::wstring& json, const std::wstring& key) {
        auto kp = FindKey(json, key);
        if (kp == std::wstring::npos) return L"";
        size_t col = json.find(L':', kp);
        if (col == std::wstring::npos) return L"";
        size_t q1 = json.find(L'"', col + 1);
        if (q1 == std::wstring::npos) return L"";
        size_t q2 = FindClosingQuote(json, q1 + 1);
        if (q2 == std::wstring::npos) return L"";
        return Unescape(json.substr(q1 + 1, q2 - q1 - 1));
    }

    static int ExtractInt(const std::wstring& json, const std::wstring& key) {
        auto kp = FindKey(json, key);
        if (kp == std::wstring::npos) return 0;
        size_t col = json.find(L':', kp);
        if (col == std::wstring::npos) return 0;
        size_t p = col + 1;
        while (p < json.size() && (json[p] == L' ' || json[p] == L'\t')) ++p;
        std::wstring num;
        while (p < json.size() && (iswdigit(json[p]) || json[p] == L'-')) num += json[p++];
        return num.empty() ? 0 : _wtoi(num.c_str());
    }

    static double ExtractDouble(const std::wstring& json, const std::wstring& key) {
        auto kp = FindKey(json, key);
        if (kp == std::wstring::npos) return 0.0;
        size_t col = json.find(L':', kp);
        if (col == std::wstring::npos) return 0.0;
        size_t p = col + 1;
        while (p < json.size() && (json[p] == L' ' || json[p] == L'\t')) ++p;
        std::wstring num;
        while (p < json.size() && (iswdigit(json[p]) || json[p] == L'-' || json[p] == L'.')) num += json[p++];
        return num.empty() ? 0.0 : _wtof(num.c_str());
    }

    static bool ExtractBool(const std::wstring& json, const std::wstring& key) {
        auto kp = FindKey(json, key);
        if (kp == std::wstring::npos) return false;
        size_t col = json.find(L':', kp);
        if (col == std::wstring::npos) return false;
        size_t p = col + 1;
        while (p < json.size() && (json[p] == L' ' || json[p] == L'\t')) ++p;
        return json.compare(p, 4, L"true") == 0;
    }

    static std::vector<std::wstring> ExtractStringArray(const std::wstring& json, const std::wstring& key) {
        std::vector<std::wstring> out;
        auto kp = FindKey(json, key);
        if (kp == std::wstring::npos) return out;
        size_t ab = json.find(L'[', json.find(L':', kp));
        if (ab == std::wstring::npos) return out;
        size_t ae = json.find(L']', ab);
        if (ae == std::wstring::npos) return out;
        size_t p = ab + 1;
        while ((p = json.find(L'"', p)) != std::wstring::npos && p < ae) {
            size_t q = FindClosingQuote(json, p + 1);
            if (q == std::wstring::npos || q > ae) break;
            out.push_back(Unescape(json.substr(p + 1, q - p - 1)));
            p = q + 1;
        }
        return out;
    }

    static std::map<std::wstring, int> ExtractStringIntMap(const std::wstring& json, const std::wstring& key) {
        std::map<std::wstring, int> out;
        auto kp = FindKey(json, key);
        if (kp == std::wstring::npos) return out;
        size_t ob = json.find(L'{', json.find(L':', kp));
        if (ob == std::wstring::npos) return out;
        size_t oe = json.find(L'}', ob);
        if (oe == std::wstring::npos) return out;
        size_t p = ob + 1;
        while ((p = json.find(L'"', p)) != std::wstring::npos && p < oe) {
            size_t q = FindClosingQuote(json, p + 1);
            if (q == std::wstring::npos || q > oe) break;
            std::wstring mk = Unescape(json.substr(p + 1, q - p - 1));
            size_t col = json.find(L':', q + 1);
            if (col == std::wstring::npos || col > oe) break;
            size_t np = col + 1;
            while (np < json.size() && (json[np] == L' ' || json[np] == L'\t')) ++np;
            std::wstring num;
            while (np < json.size() && (iswdigit(json[np]) || json[np] == L'-')) num += json[np++];
            if (!num.empty()) out[mk] = _wtoi(num.c_str());
            p = np;
        }
        return out;
    }

    // ── Building ──────────────────────────────────────────────────────────

    static std::wstring StringArrayToJson(const std::vector<std::wstring>& arr) {
        std::wstring j = L"[\n";
        for (size_t i = 0; i < arr.size(); ++i) {
            j += L"    \"" + Escape(arr[i]) + L"\"";
            if (i + 1 < arr.size()) j += L",";
            j += L"\n";
        }
        j += L"  ]";
        return j;
    }

    static std::wstring StringIntMapToJson(const std::map<std::wstring, int>& m) {
        if (m.empty()) return L"{}";
        std::wstring j = L"{\n";
        size_t i = 0;
        for (const auto& kv : m) {
            j += L"    \"" + Escape(kv.first) + L"\": " + std::to_wstring(kv.second);
            if (++i < m.size()) j += L",";
            j += L"\n";
        }
        j += L"  }";
        return j;
    }

    static std::wstring BuildObject(const std::vector<std::pair<std::wstring, std::wstring>>& fields) {
        std::wstring j = L"{\n";
        for (size_t i = 0; i < fields.size(); ++i) {
            j += L"  \"" + fields[i].first + L"\": " + fields[i].second;
            if (i + 1 < fields.size()) j += L",";
            j += L"\n";
        }
        j += L"}";
        return j;
    }

private:
    static size_t FindKey(const std::wstring& json, const std::wstring& key) {
        std::wstring needle = L"\"" + key + L"\"";
        return json.find(needle);
    }

    // Find the closing quote of a string starting at pos (pos is after the opening quote)
    static size_t FindClosingQuote(const std::wstring& s, size_t pos) {
        while (pos < s.size()) {
            if (s[pos] == L'\\') { pos += 2; continue; }
            if (s[pos] == L'"')  return pos;
            ++pos;
        }
        return std::wstring::npos;
    }
};

} // namespace QuickLaunchTool
