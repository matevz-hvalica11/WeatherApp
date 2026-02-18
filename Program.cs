var builder = WebApplication.CreateBuilder(args);

// Do not manually rebuild Configuration
// ASP.NET already does the right thing by default

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "history",
    pattern: "History/{action=Index}/{id?}",
    defaults: new { controller = "History", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Weather}/{action=Index}/{id?}");

app.Run();
