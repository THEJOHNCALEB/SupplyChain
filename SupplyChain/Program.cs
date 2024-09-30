using SupplyChain.Data;
using SupplyChain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using MongoDB.Driver;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

//var userName = Environment.UserName;
int portNumber = 3001;

builder.WebHost.ConfigureKestrel(options => {
    options.Listen(IPAddress.Loopback, portNumber);
});
builder.WebHost.UseUrls();

// Add services to the container.
builder.Services.AddControllersWithViews(options => {
    options.Filters.Add(new AuthorizeFilter());
});
builder.Services.AddAuthentication("SupplyChain")
    .AddCookie("SupplyChain", options => {
        options.LoginPath = "/";
        options.Cookie.Name = "SupplyChainNow";
        options.Cookie.IsEssential = true;
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.LogoutPath = "/LoginPage/Login";
    });


builder.Services.AddTransient<ApplicationDbContext>();
builder.Services.AddTransient<ItemsDbContext>();
builder.Services.AddTransient<OrderDbContext>();
builder.Services.AddTransient<CartDbContext>();

//Add Database Config
builder.Services.AddSingleton(_ => {
    var settings = MongoClientSettings.FromConnectionString(
      "mongodb_url"
      );
    var client = new MongoClient(settings);
    return client.GetDatabase("SupplyChain");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production
    // scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWebSockets();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
