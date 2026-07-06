var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PagueVeloz API V1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
}));

app.MapGet("/", () => "PagueVeloz API is running! Acesse /swagger");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
