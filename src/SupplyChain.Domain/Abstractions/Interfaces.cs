using SupplyChain.Domain.Common;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;

namespace SupplyChain.Domain.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IUserService
{
    Task<ServiceResult<AppUser>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ServiceResult<AppUser>> UpdateProfileAsync(Guid userId, ProfileUpdate update, CancellationToken ct = default);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);
    Task<ServiceResult> UpdateAvatarAsync(Guid userId, string avatarUrl, CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> GetAllAsync(string? search = null, UserRole? role = null, CancellationToken ct = default);
    Task<ServiceResult> SetRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);
    Task<ServiceResult> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
}

public interface IProductService
{
    Task<PagedResult<Product>> SearchAsync(ProductQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetFeaturedAsync(int take = 6, CancellationToken ct = default);
    Task<Product?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<ServiceResult<Product>> CreateAsync(Guid ownerId, ProductCreate create, CancellationToken ct = default);
    Task<ServiceResult<Product>> UpdateAsync(Guid ownerId, Guid productId, ProductCreate update, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(Guid ownerId, Guid productId, CancellationToken ct = default);
    Task<ServiceResult<Product>> AcquireAsync(AcquireRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SupplyChainTransfer>> GetTransfersAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<SupplyChainTransfer>> GetTransfersBySellerAsync(Guid sellerId, CancellationToken ct = default);
    Task<bool> CanAcquireAsync(Product product, UserRole buyerRole);
    Task<IReadOnlyDictionary<ProductCategory, int>> GetCategoryCountsAsync(CancellationToken ct = default);
}

public interface ICartService
{
    Task<IReadOnlyList<CartItem>> GetAsync(Guid userId, CancellationToken ct = default);
    Task<ServiceResult<CartItem>> AddAsync(Guid userId, Guid productId, int quantity = 1, CancellationToken ct = default);
    Task<ServiceResult> UpdateQuantityAsync(Guid userId, Guid cartItemId, int quantity, CancellationToken ct = default);
    Task<ServiceResult> RemoveAsync(Guid userId, Guid cartItemId, CancellationToken ct = default);
    Task<ServiceResult> ClearAsync(Guid userId, CancellationToken ct = default);
    Task<decimal> GetTotalAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetCountAsync(Guid userId, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<ServiceResult<Order>> CheckoutAsync(Guid userId, CheckoutDetails details, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetForCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetForSellerAsync(Guid sellerId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order?> GetAsync(Guid orderId, CancellationToken ct = default);
    Task<ServiceResult> UpdateStatusAsync(Guid orderId, Guid actorId, UserRole actorRole, OrderStatus status, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSnapshot> GetAsync(AppUser user, CancellationToken ct = default);
}

public interface IFileStorage
{
    Task<ServiceResult<string>> SaveImageAsync(Stream stream, string fileName, string folder, CancellationToken ct = default);
    bool Delete(string relativeUrl);
}
