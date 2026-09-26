// Composition root for App.Handlers: hosts Event Grid webhook subscribers.

var builder = WebApplication.CreateBuilder(args);

// Register webhook controllers that receive Event Grid and CloudEvents deliveries
builder.Services.AddControllers();
// Enable OpenAPI metadata used by Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger UI is only exposed in Development to avoid publishing webhook routes in other environments
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
