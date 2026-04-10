using Mando.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMandoApiServices(builder.Configuration);

var app = builder.Build();

await app.UseMandoApiPipelineAsync();

app.Run();

public partial class Program
{
}