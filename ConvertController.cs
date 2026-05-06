using FileConverterApi.Interfaces;
using FileConverterApi.Models;
using FileConverterApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace FileConverterApi
{
    [ApiController]
    [Route("api/convertfile")]
    public class ConvertController : ControllerBase
    {
        private readonly CsvReaderService _csv;
        private readonly ExcelReaderService _excel;
        private readonly ParquetWriterService _parquet;
        private readonly MinioService _minio;
        private readonly IcebergService _iceberg;
        private readonly IConfiguration _config;
        public ConvertController(
            CsvReaderService csv, ExcelReaderService excel,
            ParquetWriterService parquet, MinioService minio,
            IcebergService iceberg, IConfiguration config)
        {
            _csv = csv; _excel = excel;
            _parquet = parquet; _minio = minio;
            _iceberg = iceberg; _config = config;
        }
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {

            if (file is null || file.Length == 0)
            {
                return BadRequest("Chưa chọn file");
            }
            var ext = Path.GetExtension(file.Name).ToLower();
            if (ext is not ".csv" and not ".xlsx" and not ".xls")
            {
                return BadRequest("Không hỗ trợ định dạng khác ngoài file csv, xlsx, xls");
            }
            try
            {
                DataTable table = new DataTable();
                using (var stream = file.OpenReadStream()) {
                    table = ext == ".csv"
                        ? _csv.Read(stream, file.FileName)
                        : _excel.Read(stream, file.FileName);
                }
                var result = await ProcessAsync(table,file.FileName);
                return Ok(result);

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        [HttpPost("local")]
        public async Task<IActionResult> FromLocal([FromBody] LocalFileRequest req)
        {
            if (!System.IO.File.Exists(req.FilePath))
                return NotFound($"Không tìm thấy file: {req.FilePath}");

            var ext = Path.GetExtension(req.FilePath).ToLower();
            if (ext is not ".csv" and not ".xlsx" and not ".xls")
                return BadRequest("Chỉ hỗ trợ .csv, .xlsx, .xls");
            try
            {
                var table = ext == ".csv"
                    ? _csv.ReadFromPath(req.FilePath)
                    : _excel.ReadFromPath(req.FilePath);

                var result = await ProcessAsync(table, Path.GetFileName(req.FilePath));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ConvertResult
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }
        [HttpPost(("watch"))]
        public async Task<IActionResult> ConvertWatch()
        {
            var folder = _config["LocalWatch:Path"];
            if(!Directory.Exists(folder))
            {
                return NotFound("Thư mục không tồn tại");
            }
            var files = Directory.GetFiles(folder,"*.*")
                .Where(f  => new[] {".csv",".xlsx",".xls"}
                    .Contains(Path.GetExtension(f).ToLower()))
                .ToList();
                ;
            if (!files.Any())
            {
                return Ok(new
                {
                    message = "Không có file nào trong thư mục",
                    results = new List<ConvertResult>()
                });
            }
            var results = new List<ConvertResult>();
            foreach (var filePath in files)
            {
                try
                {
                    var ext = Path.GetExtension(filePath).ToLower();
                    var table = ext == ".csv"
                        ? _csv.ReadFromPath(filePath)
                        : _excel.ReadFromPath(filePath);
                    var result = await ProcessAsync(table, Path.GetFileName(filePath));
                    results.Add(result);
                }
                catch (Exception ex) {
                    results.Add(new ConvertResult
                    {
                        Success = false,
                        FileName = Path.GetFileName(filePath),
                        Message = ex.Message
                    });
                }
            }
            return Ok(new 
            {
                total = results.Count,
                success = results.Count(r => r.Success),
                failed = results.Count(r => !r.Success),
                results
            });
        }
        private async Task<ConvertResult> ProcessAsync(DataTable table, string originalFileName)
        {
            var tableName = Path.GetFileNameWithoutExtension(originalFileName)
                        .ToLower().Replace(" ", "_");

            // 2. Ghi Parquet
            var localPath = await _parquet.WriteAsync(table);

            // 3. Upload MinIO
            await _minio.EnsureBucketAsync();
            var objectKey = $"{tableName}/{DateTime.UtcNow:yyyyMMdd-HHmmss}.parquet";
            var s3Path = await _minio.UploadAsync(localPath, objectKey);

            // 4. Đăng ký Iceberg
            var s3Location = $"s3://{_minio.Bucket}/{tableName}/";
            await _iceberg.RegisterTableAsync(tableName, s3Location, table);

            // Dọn file tạm
            System.IO.File.Delete(localPath);

            return new ConvertResult
            {
                Success = true,
                FileName = originalFileName,
                ParquetKey = objectKey,
                TableName = tableName,
                TotalRows = table.Rows.Count,
                TotalColumns = table.Columns.Count,
                FileSizeBytes = new FileInfo(localPath).Exists ? new FileInfo(localPath).Length : 0,
                Message = "Chuyển đổi thành công!",
                Columns = table.Columns.Cast<DataColumn>()
                                    .Select(c => c.ColumnName).ToList()
            };
        }
    }
}
