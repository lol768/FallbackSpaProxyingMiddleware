Fallback SPA proxying middleware
--------------------------------

**This proxy is only intended for use in development, to support HMR and the other features that frontend SPA frameworks
support with their built-in web servers. It must not be used in production.**

The default SpaServices `Microsoft.AspNetCore.SpaProxy` is broken and
the development-mode SPA proxy tramples all over any defined MVC-style endpoints, directing all requests to the SPA
development server, even those you might like to be served by an API controller.

Microsoft claim that this is intentional and that it should be up to the SPA to determine which endpoints
to proxy back to the backend. This behaviour is illogical, poorly-documented and differs completely between
development and production (where we use `UseSpaStaticFiles` / `endpoints.MapFallbackToFile`).

Microsoft will not discuss or properly this logic and have locked numerous issues on the matter:

- https://github.com/dotnet/AspNetCore.Docs/issues/18405
- https://github.com/dotnet/aspnetcore/issues/45130#issuecomment-1326595534

This middleware plays nicely with other MVC endpoints and allows them to serve requests first where a route has been
defined. It can be added as follows:

In `Program.cs`:

```csharp
// ....
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseSpa(s =>
{
    if (app.Environment.IsDevelopment())
    {
        s.ApplicationBuilder.UseFallbackSpaProxying("http://localhost:9999");
    }
    // .....
});
```

The implementation is a bit janky, relying on reflection because the classes we need are marked `internal`, but it
works.
