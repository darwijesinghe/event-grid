using App.API.Data;
using App.API.Models;
using App.API.Services;
using App.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Composition root for App.API:
// registers HTTP, Swagger, Event Grid, blob storage, and Azure SQL.

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers that expose the orders and invoices APIs
builder.Services.AddControllers();
// Enable OpenAPI metadata used by Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind Event Grid, SQL, and storage sections from configuration onto AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration);

// Singletons: Event Grid publisher and blob uploader are stateless SDK wrappers
builder.Services.AddSingleton<IOrderEventPublisher, OrderEventPublisher>();
builder.Services.AddSingleton<IInvoiceBlobService, InvoiceBlobService>();

// Scoped SQL repository shares the request-scoped AppDbContext
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var dbOptions = sp.GetRequiredService<IOptions<AppSettings>>().Value;
    options.UseSqlServer(dbOptions.AppDbContextOptions.ConnectionString);
});
builder.Services.AddScoped<IOrderRepository, SqlOrderRepository>();

var app = builder.Build();

// Swagger UI is only exposed in Development to avoid publishing the API surface in other environments
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
