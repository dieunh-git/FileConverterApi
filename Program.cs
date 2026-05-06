using FileConverterApi.Interfaces;
using FileConverterApi.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.Services.AddSingleton<CsvReaderService>();
builder.Services.AddSingleton<ExcelReaderService>();
builder.Services.AddSingleton<ParquetWriterService>();
builder.Services.AddSingleton<MinioService>();
builder.Services.AddSingleton<IcebergService>();

// Upload file lớn tối đa 100MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opt =>
{
    opt.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Init Iceberg namespace
var iceberg = app.Services.GetRequiredService<IcebergService>();
await iceberg.InitAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
try
{
    app.MapControllers();
}
catch (ReflectionTypeLoadException ex)
{
    foreach (var loaderEx in ex.LoaderExceptions)
        Console.WriteLine($"❌ LoaderException: {loaderEx?.Message}");
    throw;
}
app.Run();