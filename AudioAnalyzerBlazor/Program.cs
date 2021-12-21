using AudioAnalyzerBlazor.Data;
using AudioAnalyzer.FeatureExtraction;
using AudioAnalyzer.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<AudioAnalyzerService>();
builder.Services.AddScoped<FeatureExtractionPipeline>();
builder.Services.AddScoped<IPersistenceService, JsonPersistenceService>();
builder.Services.AddScoped<FeatureExtractorFormState>();
builder.Services.AddScoped<UploadedFilesState>();
builder.Services.AddScoped<CircuitHandler, CircuitHandlerService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
