using System.Globalization;
using System.Text;

namespace ExamSchedule.Api.Data;

public static class AcademicCatalog
{
    public static readonly IReadOnlyList<string> KhoaList = new[]
    {
        "Khoa Giáo dục thể chất - Quốc phòng và An ninh",
        "Khoa Khoa học Xã hội và Nhân văn",
        "Khoa Kĩ thuật và Môi trường",
        "Khoa Kinh tế và Du lịch",
        "Khoa Ngoại ngữ",
        "Khoa Quản lý và Đô thị",
        "Khoa Sư phạm",
        "Khoa Toán - Công nghệ thông tin",
        "Viện Hà Nội học và Đào tạo quốc tế"
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByKhoa =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Khoa Giáo dục thể chất - Quốc phòng và An ninh"] = new[]
            {
                "Giáo dục thể chất (ĐH)"
            },
            ["Khoa Khoa học Xã hội và Nhân văn"] = new[]
            {
                "Chính trị học (ĐH)",
                "Công tác Xã hội (ĐH)",
                "Luật (ĐH)",
                "Quản lý Giáo dục (ĐH)",
                "Tâm lý học (ĐH)",
                "Văn học (ĐH)"
            },
            ["Khoa Kĩ thuật và Môi trường"] = new[]
            {
                "Công nghệ - kĩ thuật môi trường (ĐH)"
            },
            ["Khoa Kinh tế và Du lịch"] = new[]
            {
                "Quản lý kinh tế (ĐH)",
                "Quản trị dịch vụ du lịch và lữ hành (ĐH)",
                "Quản trị khách sạn (ĐH)",
                "Quản trị Kinh doanh (ĐH)",
                "Tài chính - Ngân hàng (ĐH)"
            },
            ["Khoa Ngoại ngữ"] = new[]
            {
                "Ngôn ngữ Anh (ĐH)",
                "Ngôn ngữ Trung Quốc (ĐH)",
                "Sư phạm Tiếng Anh (ĐH)"
            },
            ["Khoa Quản lý và Đô thị"] = new[]
            {
                "Logistics và quản lý chuỗi cung ứng (ĐH)",
                "Quản lý công (ĐH)"
            },
            ["Khoa Sư phạm"] = new[]
            {
                "Giáo dục Công dân (ĐH)",
                "Giáo dục đặc biệt (ĐH)",
                "Giáo dục Mầm non - Giáo dục hòa nhập (ĐH)",
                "Giáo dục Mầm non (ĐH)",
                "Giáo dục Tiểu học - Giáo dục hòa nhập (ĐH)",
                "Giáo dục Tiểu học (ĐH)",
                "Giám dục Tiểu học tiên tiến (ĐH)",
                "SP Lịch sử (ĐH)",
                "SP Ngữ văn (ĐH)",
                "SP Toán học (ĐH)",
                "SP Vật lý (ĐH)"
            },
            ["Khoa Toán - Công nghệ thông tin"] = new[]
            {
                "Công nghệ Thông tin (ĐH)",
                "Sư phạm Tin học (ĐH)",
                "Toán ứng dụng (ĐH)"
            },
            ["Viện Hà Nội học và Đào tạo quốc tế"] = new[]
            {
                "Văn hóa học (ĐH)",
                "Việt Nam học (ĐH)"
            }
        };

    private static readonly IReadOnlyDictionary<string, string> KhoaAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sư Phạm"] = "Khoa Sư phạm",
            ["Sư phạm"] = "Khoa Sư phạm",
            ["Khoa Sư Phạm"] = "Khoa Sư phạm",
            ["Ngoại Ngữ"] = "Khoa Ngoại ngữ",
            ["Ngoại ngữ"] = "Khoa Ngoại ngữ",
            ["Kinh tế - Du lịch"] = "Khoa Kinh tế và Du lịch",
            ["Kinh tế Du lịch"] = "Khoa Kinh tế và Du lịch",
            ["Kinh tế và Du lịch"] = "Khoa Kinh tế và Du lịch",
            ["Toán-CNTT"] = "Khoa Toán - Công nghệ thông tin",
            ["Toán và Công nghệ thông tin"] = "Khoa Toán - Công nghệ thông tin",
            ["Toán - CNTT"] = "Khoa Toán - Công nghệ thông tin",
            ["KHXH&NV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["KHXHNV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["KHXH NV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["KHXH và NV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Khoa học xã hội và nhân văn"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Khoa học xã hội & nhân văn"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Khoa học Xã hội- Nhân văn"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Khoa học XH&NV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Khoa Xã hội & Nhân văn"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Khoa xã hội và nhân văn"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Kho học xã hội và nhân văn"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Xh&Nv"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["KHXHNV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["KHXH và NV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["KHXH và NG"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Quản lý và đô thị"] = "Khoa Quản lý và Đô thị",
            ["Quản lí và Đô thị"] = "Khoa Quản lý và Đô thị",
            ["Quản lý & Đô thị"] = "Khoa Quản lý và Đô thị",
            ["Khoa KHXH&NV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Khoa KHXH NV"] = "Khoa Khoa học Xã hội và Nhân văn",
            ["Giáo dục thể chất"] = "Khoa Giáo dục thể chất - Quốc phòng và An ninh",
            ["GDTC"] = "Khoa Giáo dục thể chất - Quốc phòng và An ninh",
            ["Giáo dục thể chất- Quốc phòng và an ninh"] = "Khoa Giáo dục thể chất - Quốc phòng và An ninh",
            ["Giáo dục thể chất và quốc phòng an ninh"] = "Khoa Giáo dục thể chất - Quốc phòng và An ninh",
            ["Khoa Kỹ thuật và Môi trường"] = "Khoa Kĩ thuật và Môi trường",
            ["Khoa Kĩ thuật và Môi trường"] = "Khoa Kĩ thuật và Môi trường",
            ["Khoa KT & MT"] = "Khoa Kĩ thuật và Môi trường",
            ["Kinh tế & Du lịch"] = "Khoa Kinh tế và Du lịch",
            ["Kinh tế-Du lịch"] = "Khoa Kinh tế và Du lịch",
            ["Toán - CNTT"] = "Khoa Toán - Công nghệ thông tin",
            ["Toán-CNTT"] = "Khoa Toán - Công nghệ thông tin",
            ["Viện Hà Nội học & Đào tạo Quốc tế"] = "Viện Hà Nội học và Đào tạo quốc tế",
            ["Viện Hà Nội và Đào tạo quốc tế"] = "Viện Hà Nội học và Đào tạo quốc tế"
        };

    private static readonly IReadOnlyDictionary<string, string> NganhAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CTXH"] = "Công tác Xã hội (ĐH)",
            ["Công tác xã hội"] = "Công tác Xã hội (ĐH)",
            ["QLGD"] = "Quản lý Giáo dục (ĐH)",
            ["Quản lý giáo dục"] = "Quản lý Giáo dục (ĐH)",
            ["Quản lí Giáo dục"] = "Quản lý Giáo dục (ĐH)",
            ["GDMN"] = "Giáo dục Mầm non (ĐH)",
            ["Giáo dục mầm non"] = "Giáo dục Mầm non (ĐH)",
            ["Ngành Giáo dục mầm non"] = "Giáo dục Mầm non (ĐH)",
            ["GDTH"] = "Giáo dục Tiểu học (ĐH)",
            ["Giáo dục tiểu học"] = "Giáo dục Tiểu học (ĐH)",
            ["QTDVDL&LH"] = "Quản trị dịch vụ du lịch và lữ hành (ĐH)",
            ["Quản trị dịch vụ và lữ hành"] = "Quản trị dịch vụ du lịch và lữ hành (ĐH)",
            ["Luật học"] = "Luật (ĐH)",
            ["Tâm Lý Học"] = "Tâm lý học (ĐH)",
            ["Tâm lý học"] = "Tâm lý học (ĐH)",
            ["Văn Hoá Học"] = "Văn hóa học (ĐH)",
            ["Văn Học"] = "Văn học (ĐH)",
            ["Văn học"] = "Văn học (ĐH)",
            ["Văn học D2023"] = "Văn học (ĐH)",
            ["Logistics & Quản lý chuỗi cung ứng"] = "Logistics và quản lý chuỗi cung ứng (ĐH)",
            ["Logistics và Quản lý chuỗi cung ứng"] = "Logistics và quản lý chuỗi cung ứng (ĐH)",
            ["Logistics và quản lí chuỗi cung ứng"] = "Logistics và quản lý chuỗi cung ứng (ĐH)",
            ["Logistics và Quản lý chuỗi cung ứng 2018"] = "Logistics và quản lý chuỗi cung ứng (ĐH)",
            ["Giáo Dục Đặc Biệt"] = "Giáo dục đặc biệt (ĐH)",
            ["Giáo dục đặc biệt"] = "Giáo dục đặc biệt (ĐH)",
            ["Sư phạm Giáo Dục Công Dân"] = "Giáo dục Công dân (ĐH)",
            ["Giáo dục công dân"] = "Giáo dục Công dân (ĐH)",
            ["Giám dục Tiểu học tiên tiến"] = "Giám dục Tiểu học tiên tiến (ĐH)",
            ["Giáo dục Tiểu học tiên tiến"] = "Giám dục Tiểu học tiên tiến (ĐH)",
            ["Ngành Giáo dục mầm non"] = "Giáo dục Mầm non (ĐH)",
            ["Giáo dục thể chất"] = "Giáo dục thể chất (ĐH)",
            ["Công nghệ - Kĩ thuật môi trường"] = "Công nghệ - kĩ thuật môi trường (ĐH)",
            ["Công nghệ - kỹ thuật môi trường"] = "Công nghệ - kĩ thuật môi trường (ĐH)",
            ["Công nghệ - kĩ thuật môi trường"] = "Công nghệ - kĩ thuật môi trường (ĐH)",
            ["Sư phạm Vật lý"] = "SP Vật lý (ĐH)",
            ["Văn học D2023"] = "Văn học (ĐH)",
            ["Văn Hoá Học"] = "Văn hóa học (ĐH)"
        };

    public static string? NormalizeKhoa(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        var normalizedText = NormalizeText(trimmed);

        foreach (var canonical in KhoaList)
        {
            if (NormalizeText(canonical) == normalizedText)
                return canonical;
        }

        foreach (var alias in KhoaAliases)
        {
            if (NormalizeText(alias.Key) == normalizedText)
                return alias.Value;
        }

        foreach (var khoa in KhoaList)
        {
            if (khoa.Contains(trimmed, StringComparison.OrdinalIgnoreCase) || trimmed.Contains(khoa, StringComparison.OrdinalIgnoreCase))
                return khoa;
        }

        return trimmed;
    }

    public static string? NormalizeNganhHoc(string? khoa, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalizedValue = value.Trim();
        var normalizedText = NormalizeText(normalizedValue);
        var lookupKhoa = NormalizeKhoa(khoa);

        foreach (var canonical in GetMajorsForKhoa(lookupKhoa))
        {
            if (NormalizeText(canonical) == normalizedText)
                return canonical;
            if (canonical.Contains(normalizedValue, StringComparison.OrdinalIgnoreCase) || normalizedValue.Contains(canonical, StringComparison.OrdinalIgnoreCase))
                return canonical;
        }

        foreach (var alias in NganhAliases)
        {
            if (NormalizeText(alias.Key) == normalizedText)
                return alias.Value;
        }

        foreach (var list in ByKhoa.Values)
        {
            foreach (var canonical in list)
            {
                if (NormalizeText(canonical) == normalizedText)
                    return canonical;
                if (canonical.Contains(normalizedValue, StringComparison.OrdinalIgnoreCase) || normalizedValue.Contains(canonical, StringComparison.OrdinalIgnoreCase))
                    return canonical;
            }
        }

        return normalizedValue;
    }

    private static IEnumerable<string> GetMajorsForKhoa(string? khoa)
    {
        if (string.IsNullOrWhiteSpace(khoa))
            return Enumerable.Empty<string>();

        return ByKhoa.TryGetValue(khoa, out var majors) ? majors : Enumerable.Empty<string>();
    }

    private static string NormalizeText(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
                builder.Append(char.ToLowerInvariant(ch));
            else if (char.IsWhiteSpace(ch) || ch == '&')
                builder.Append(' ');
        }

        var result = builder.ToString();
        while (result.Contains("  "))
            result = result.Replace("  ", " ");

        return result.Trim();
    }
}
