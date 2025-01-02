using System;

namespace CSpider.Utils;

public class Helper
{
    public static DateTime NormalizeDateTime(DateTime date, bool isStartOfDay)
    {
        return isStartOfDay
            ? new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified)
            : new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999, DateTimeKind.Unspecified);
    }
}