// Thêm alias ở đầu file
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using SysData = System.Data;  // ← alias để phân biệt

public class ParquetWriterService
{
    public async Task<string> WriteAsync(SysData.DataTable table)
    {
        var path = Path.Combine(Path.GetTempPath(),
                     $"{table.TableName}-{DateTime.UtcNow:yyyyMMddHHmmss}.parquet");
        var fields = BuildFields(table);
        var schema = new ParquetSchema(fields.ToArray());

        using var fs = File.Create(path);
        using var writer = await ParquetWriter.CreateAsync(schema, fs);
        using var group = writer.CreateRowGroup();

        foreach (SysData.DataColumn col in table.Columns)
        {
            var vals = table.Rows.Cast<SysData.DataRow>()
                .Select(r => r[col] is DBNull ? null : r[col].ToString())
                .ToArray();

            // Parquet.Data.DataColumn ← dùng thẳng vì không có alias
            await group.WriteColumnAsync(
                new DataColumn(schema.FindDataField(Sanitize(col.ColumnName)), vals));
        }

        return path;
    }

    private List<DataField> BuildFields(SysData.DataTable table)
    {
        var fields = new List<DataField>();
        foreach (SysData.DataColumn col in table.Columns)
            fields.Add(new DataField<string>(Sanitize(col.ColumnName)));
        return fields;
    }

    private string Sanitize(string name)
        => System.Text.RegularExpressions.Regex
            .Replace(name.Trim().ToLower().Replace(" ", "_"), @"[^\w]", "");
}