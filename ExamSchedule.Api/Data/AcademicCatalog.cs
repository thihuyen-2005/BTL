using System.Globalization;
using System.Text;

namespace ExamSchedule.Api.Data;

public static class AcademicCatalog
{
    public static readonly IReadOnlyList<string> KhoaList = new[]
    {
        "Khoa Giáo dục thể chất - Quốc phòng và An ninh",
        "Khoa Khoa học Xã hội và Nhân văn",
        "Khoa Kỹ thuật và Môi trường",
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
                "Công tác xã hội (ĐH)",
                "Luật (ĐH)",
                "Quản lý Giáo dục (ĐH)",
                "Tâm lý học (ĐH)",
                "Văn học (ĐH)"
            },
            ["Khoa Kỹ thuật và Môi trường"] = new[]
            {
                "Công nghệ - Kỹ thuật môi trường (ĐH)"
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
                "Logistics và quản lý chuỗi ứng (ĐH)",
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
                "Giáo dục Tiểu học tiên tiến (ĐH)",
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

    public static string? NormalizeKhoa(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        foreach (var khoa in KhoaList)
        {
            if (string.Equals(NormalizeText(khoa), NormalizeText(trimmed), StringComparison.OrdinalIgnoreCase))
                return khoa;
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
        var lookupKhoa = NormalizeKhoa(khoa);

        if (!string.IsNullOrWhiteSpace(lookupKhoa) && ByKhoa.TryGetValue(lookupKhoa, out var majors))
        {
            foreach (var major in majors)
            {
                if (string.Equals(NormalizeText(major), NormalizeText(normalizedValue), StringComparison.OrdinalIgnoreCase))
                    return major;
                if (major.Contains(normalizedValue, StringComparison.OrdinalIgnoreCase) || normalizedValue.Contains(major, StringComparison.OrdinalIgnoreCase))
                    return major;
            }
        }

        foreach (var majorOptions in ByKhoa.Values)
        {
            foreach (var major in majorOptions)
            {
                if (string.Equals(NormalizeText(major), NormalizeText(normalizedValue), StringComparison.OrdinalIgnoreCase))
                    return major;
                if (major.Contains(normalizedValue, StringComparison.OrdinalIgnoreCase) || normalizedValue.Contains(major, StringComparison.OrdinalIgnoreCase))
                    return major;
            }
        }

        return normalizedValue;
    }

    private static string NormalizeText(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC)
            .Replace("&", "and")
            .Replace("  ", " ")
            .Trim();
    }
}
