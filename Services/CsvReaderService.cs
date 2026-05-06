using CsvHelper;
using System.Data;
using System.Globalization;

namespace FileConverterApi.Services
{
    public class CsvReaderService 
    {
        public DataTable Read(Stream stream, string fileName)
        {
            using var reader = new StreamReader(stream); 
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var dt = new DataTable(Path.GetFileNameWithoutExtension(fileName));
            bool isHeader = true;
            while (csv.Read())
            {
                if (isHeader)
                {
                    csv.ReadHeader();
                    foreach(var h in csv.HeaderRecord!)
                    {
                        dt.Columns.Add(h);
                        isHeader = false;
                        continue;
                    }
                }
                var row = dt.NewRow();
                foreach(DataColumn col in dt.Columns)
                {
                    row[col.ColumnName] = csv.GetField(col.ColumnName) ?? "";
                    dt.Rows.Add(row);
                }
            }
            return dt;
        }
        public DataTable ReadFromPath(string filerPath)
        {
            using var stream = File.OpenRead(filerPath);
            return Read(stream, Path.GetFileName(filerPath));

        }
    }
}
