using ASNParser;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IFileProcessor, FileProcessor>();

var host = builder.Build();
host.Run();
