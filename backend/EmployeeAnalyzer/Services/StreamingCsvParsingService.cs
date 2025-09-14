using EmployeeAnalyzer.Models;
using System.Globalization;

namespace EmployeeAnalyzer.Services;

public class StreamingCsvParsingService
{
    private readonly string[] _dateFormats = new[]
    {
        "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd",
        "dd-MM-yyyy", "MM-dd-yyyy", "dd.MM.yyyy", "yyyy.MM.dd",
        "yyyyMMdd", "MMM dd, yyyy", "dd MMM yyyy", 
        "MMMM dd, yyyy", "dd MMMM yyyy"
    };

    public async Task<List<EmployeeRecord>> ParseCsvAsync(Stream csvStream)
    {
        var records = new List<EmployeeRecord>();
        using var reader = new StreamReader(csvStream);
        
        // Skip BOM if present
        if (reader.Peek() == 0xFEFF)
            reader.Read();

        string? line;
        int lineNumber = 0;
        bool skipHeader = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (skipHeader && IsHeaderRow(line))
            {
                skipHeader = false;
                continue;
            }
            skipHeader = false;

            try
            {
                var record = ParseCsvLine(line, lineNumber);
                records.Add(record);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing line {lineNumber}: {ex.Message}");
            }
        }

        return records;
    }

    private bool IsHeaderRow(string line)
    {
        try
        {
            var fields = SplitCsvLine(line);
            if (fields.Count < 2) return false;
            return !int.TryParse(fields[0].Trim(), out _) || !int.TryParse(fields[1].Trim(), out _);
        }
        catch
        {
            return false;
        }
    }

    private EmployeeRecord ParseCsvLine(string line, int lineNumber)
    {
        var fields = SplitCsvLine(line);

        if (fields.Count != 4)
            throw new ArgumentException($"Expected 4 fields but found {fields.Count}");

        if (!int.TryParse(fields[0].Trim(), out int empId))
            throw new ArgumentException($"Invalid Employee ID: '{fields[0]}'");

        if (!int.TryParse(fields[1].Trim(), out int projectId))
            throw new ArgumentException($"Invalid Project ID: '{fields[1]}'");

        var dateFromStr = fields[2].Trim();
        if (!TryParseDate(dateFromStr, out DateTime dateFrom))
            throw new ArgumentException($"Invalid DateFrom format: '{dateFromStr}'");

        DateTime? dateTo = null;
        var dateToStr = fields[3].Trim();
        
        if (!string.IsNullOrEmpty(dateToStr) && 
            !string.Equals(dateToStr, "NULL", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseDate(dateToStr, out DateTime parsedDateTo))
                throw new ArgumentException($"Invalid DateTo format: '{dateToStr}'");
            dateTo = parsedDateTo;
        }

        if (dateTo.HasValue && dateTo.Value < dateFrom)
            throw new ArgumentException($"DateTo cannot be earlier than DateFrom");

        return new EmployeeRecord
        {
            EmployeeId = empId,
            ProjectId = projectId,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
    }

    private List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }
        
        fields.Add(currentField.ToString());
        return fields;
    }

    private bool TryParseDate(string dateStr, out DateTime result)
    {
        result = default;
        
        if (string.IsNullOrWhiteSpace(dateStr))
            return false;

        foreach (var format in _dateFormats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        return DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}