using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Infrastructure.Persistence;
using SupplyChain.Infrastructure.Security;
using SupplyChain.Infrastructure.Services;
using SupplyChain.Infrastructure.Storage;

namespace SupplyChain.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSupplyChainInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string contentRootPath)
    {
        services.AddDbContext<SupplyChainDbContext>(options => options.UseSqlite(connectionString));

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton(new StorageOptions { RootPath = contentRootPath });
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
