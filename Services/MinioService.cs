using Minio;
using Minio.DataModel.Args;

namespace FileConverterApi.Services
{
   

    public class MinioService
    {
        private readonly IMinioClient _client;
        private readonly string _bucket;

        public MinioService(IConfiguration config)
        {
            var cfg = config.GetSection("Minio");
            _bucket = cfg["Bucket"]!;
            _client = new MinioClient()
                .WithEndpoint(cfg["Endpoint"])
                .WithCredentials(cfg["AccessKey"], cfg["SecretKey"])
                .WithSSL(false)
                .Build();
        }

        public async Task EnsureBucketAsync()
        {
            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucket));
            if (!exists)
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucket));
        }

        public async Task<string> UploadAsync(string localPath, string objectKey)
        {
            await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(objectKey)
                .WithFileName(localPath)
                .WithContentType("application/octet-stream"));
            return $"s3://{_bucket}/{objectKey}";
        }

        public string Bucket => _bucket;
    }
}
