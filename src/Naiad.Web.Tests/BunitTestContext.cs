public class BunitTestContext : BunitContext
{
    public BunitTestContext() =>
        // Index injects FileDownloadService for the SVG/PNG downloads; a real instance over the loose
        // JSInterop runtime is enough for components to resolve and render under bUnit.
        Services.AddScoped<FileDownloadService>();
}
