var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<FileDownloadService>();
builder.Services.AddScoped<ThemePreferenceService>();

await builder.Build().RunAsync();
