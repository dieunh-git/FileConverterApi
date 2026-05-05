using System.Net.Http.Json;
using System.Data;

namespace FileConverterApi.Interfaces
{

    public class IcebergService
    {
        private readonly HttpClient _http;
        private readonly string _base;
        private readonly string _ns;

        public IcebergService(IConfiguration config)
        {
            _http = new HttpClient();
            _base = config["Iceberg:CatalogUrl"]!;
            _ns = config["Iceberg:Namespace"]!;
        }

        public async Task InitAsync()
        {
            try { await _http.PostAsJsonAsync($"{_base}/namespaces", new { name = new[] { _ns } }); }
            catch { }
        }

        public async Task<bool> RegisterTableAsync(string tableName, string s3Location, DataTable schema)
        {
            try
            {
                var fields = new List<object>();
                int id = 1;
                foreach (DataColumn col in schema.Columns)
                    fields.Add(new { id = id++, name = col.ColumnName.ToLower(), type = "string", required = false });

                await _http.PostAsJsonAsync($"{_base}/namespaces/{_ns}/tables", new
                {
                    name = tableName.ToLower(),
                    location = s3Location,
                    schema = new { type = "struct", fields }
                });
                return true;
            }
            catch { return false; }
        }
    }
}
