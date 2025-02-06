using Azure;
using Azure.AI.DocumentIntelligence;
using InvoiceRecognizer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<InvoiceRecognizerService>();

string endpoint = builder.Configuration.GetSection("DocumentIntelligentAPI:Endpoint").Value;
string apiKey = builder.Configuration.GetSection("DocumentIntelligentAPI:ApiKey").Value;

builder.Services.AddSingleton<DocumentIntelligenceClient>(new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey)));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
