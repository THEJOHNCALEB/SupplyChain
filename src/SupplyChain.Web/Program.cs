using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Infrastructure;
using SupplyChain.Infrastructure.Persistence;
using SupplyChain.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddResponseCompression();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=supplychain.db";

builder.Services.AddSupplyChainInfrastructure(connectionString, builder.Environment.ContentRootPath);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "supplychain.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.ReturnUrlParameter = "returnUrl";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Farmer", "Distributor", "Retailer", "Admin"));
    options.AddPolicy("FarmerOnly", policy => policy.RequireRole("Farmer", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/status/{0}");
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await using (var scope = app.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<SupplyChainDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbInitializer.InitializeAsync(db, hasher);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Could not prepare the database. Check the ConnectionStrings:Default setting.");
    }
}

app.Run();
