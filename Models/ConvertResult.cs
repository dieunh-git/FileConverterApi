namespace FileConverterApi.Models
{
    public class ConvertResult
    {
        public bool Success { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ParquetKey { get; set; } = string.Empty; 
        public string TableName { get; set; } = string.Empty; 
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public long FileSizeBytes { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
    }
}
