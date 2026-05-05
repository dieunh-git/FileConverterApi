using ExcelDataReader;
using System.Data;
using System.Text;

namespace FileConverterApi.Services
{
    public class ExcelReaderServices
    {
        public ExcelReaderServices()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        public DataTable Read(Stream stream, string fileName)
        {
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var config = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true,
                }
            };
            var ds = reader.AsDataSet(config);
            var dt = ds.Tables[0];
            var tableName = Path.GetFileNameWithoutExtension(fileName);
            return dt;
        }
        public DataTable ReadFromPath(string path)
        {
            using var stream = File.OpenRead(path);
            return Read(stream, Path.GetFileName(path));
        }
    }
}
