using Microsoft.EntityFrameworkCore;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;
using SupplyChain.Infrastructure.Persistence;

namespace SupplyChain.Infrastructure.Persistence;

/// <summary>
/// Creates the SQLite database on first run and inserts a realistic demo market
/// so the app is useful the moment it starts.
/// </summary>
public static class DbInitializer
{
    public const string DemoPassword = "Demo@123";

    public static async Task InitializeAsync(
        SupplyChainDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var admin = User(hasher, "Ada", "Obi", "admin@supplychain.dev", "admin", UserRole.Admin, "SupplyChain", "Lagos", now.AddDays(-400));
        var amina = User(hasher, "Amina", "Bello", "amina@farm.dev", "amina", UserRole.Farmer, "Bello Family Farms", "Kaduna", now.AddDays(-320));
        var musa = User(hasher, "Musa", "Ibrahim", "musa@farm.dev", "musa", UserRole.Farmer, "Sahel Harvest Co.", "Kano", now.AddDays(-300));
        var tunde = User(hasher, "Tunde", "Okafor", "tunde@distro.dev", "tunde", UserRole.Distributor, "Okafor Distribution", "Lagos", now.AddDays(-260));
        var chioma = User(hasher, "Chioma", "Eze", "chioma@freshmart.dev", "chioma", UserRole.Retailer, "FreshMart Lagos", "Lagos", now.AddDays(-220));
        var emeka = User(hasher, "Emeka", "Nwosu", "emeka@market.dev", "emeka", UserRole.Retailer, "Nwosu Corner Store", "Enugu", now.AddDays(-200));
        var dayo = User(hasher, "Dayo", "Adeyemi", "dayo@customer.dev", "dayo", UserRole.Customer, "", "Ibadan", now.AddDays(-150));
        var zainab = User(hasher, "Zainab", "Yusuf", "zainab@customer.dev", "zainab", UserRole.Customer, "", "Abuja", now.AddDays(-90));

        admin.AvatarUrl = "/uploads/avatars/demo/28e78d32-200e-4459-951a-44226453e575.jpeg";
        amina.AvatarUrl = "/uploads/avatars/demo/0827cce3-1032-4c04-945a-2471a4064243.jpeg";
        chioma.AvatarUrl = "/uploads/avatars/demo/74e99aa5-268a-49ee-9660-b6877a472140.jpg";
        dayo.AvatarUrl = "/uploads/avatars/demo/99ee0774-23b3-4eca-a14a-3b82b4a9943d.jpg";
        zainab.AvatarUrl = "/uploads/avatars/demo/b93efedd-ffbf-46b2-a9a5-16f12772cc4f.jpg";

        amina.Bio = "Third-generation yam and rice growers in Kaduna, working with 40 smallholder families.";
        musa.Bio = "Grains, tubers and vegetables from the Sahel belt, graded and packed at source.";
        tunde.Bio = "Cold-chain and long-haul distribution across South-West Nigeria.";
        chioma.Bio = "Neighbourhood grocer in Lekki focused on same-day fresh produce.";
        emeka.Bio = "Family-run corner store serving Enugu since 2009.";

        var users = new[] { admin, amina, musa, tunde, chioma, emeka, dayo, zainab };
        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);

        Product Make(string name, string reference, ProductCategory category, string description,
            string unit, decimal price, decimal cost, int quantity, string origin, string image,
            AppUser owner, ProductStage stage, bool featured, int daysAgo) => new()
        {
            Name = name,
            Reference = reference,
            Category = category,
            Description = description,
            Unit = unit,
            Price = price,
            CostPrice = cost,
            Quantity = quantity,
            InitialQuantity = quantity,
            Origin = origin,
            ImageUrl = image,
            OwnerId = owner.Id,
            Stage = stage,
            IsActive = true,
            IsFeatured = featured,
            CreatedAt = now.AddDays(-daysAgo),
            UpdatedAt = now.AddDays(-daysAgo)
        };

        var yam = Make("Fresh White Yam", "SC-YAM-1001", ProductCategory.Tubers,
            "Hand-selected white yam tubers, cured for 48 hours and graded for uniform size. Perfect for pounded yam and roasting.",
            "kg", 1_800m, 0m, 320, "Benue, Nigeria", "/media/produce/yam.svg", amina, ProductStage.Farm, true, 21);

        var maize = Make("Golden Maize", "SC-MAI-1002", ProductCategory.Grains,
            "Sun-dried yellow maize with low moisture content. Cleaned, de-stoned and ready for milling or feed.",
            "kg", 650m, 0m, 800, "Kaduna, Nigeria", "/media/produce/maize.svg", musa, ProductStage.Farm, true, 18);

        var rice = Make("Ofada Rice Paddy", "SC-RIC-1003", ProductCategory.Grains,
            "Aromatic local Ofada paddy from Kebbi. Milled on order and packed in breathable jute bags.",
            "kg", 920m, 0m, 1_200, "Kebbi, Nigeria", "/media/produce/rice.svg", amina, ProductStage.Farm, false, 15);

        var beans = Make("Brown Honey Beans", "SC-BEA-1004", ProductCategory.Grains,
            "Sweet, soft-cooking honey beans. Triple-sorted to remove grit, stones and broken grains.",
            "kg", 1_700m, 0m, 400, "Niger, Nigeria", "/media/produce/beans.svg", musa, ProductStage.Farm, true, 12);

        var potatoes = Make("Irish Potatoes", "SC-POT-1005", ProductCategory.Tubers,
            "Firm Irish potatoes from the Jos plateau, ideal for chips, roasting and boiling.",
            "kg", 1_300m, 0m, 260, "Plateau, Nigeria", "/media/produce/potatoes.svg", amina, ProductStage.Farm, false, 9);

        var tomatoes = Make("Roma Tomatoes", "SC-TOM-2001", ProductCategory.Vegetables,
            "Vine-ripened Roma tomatoes, packed in ventilated crates the same morning they are harvested.",
            "kg", 1_100m, 850m, 150, "Kano, Nigeria", "/media/produce/tomatoes.svg", tunde, ProductStage.Distribution, true, 8);

        var pepper = Make("Habanero Pepper", "SC-PEP-2002", ProductCategory.Spices,
            "Hot, bright habanero peppers — the backbone of a proper stew. Graded and crated.",
            "kg", 2_400m, 1_700m, 90, "Kaduna, Nigeria", "/media/produce/pepper.svg", tunde, ProductStage.Distribution, false, 7);

        var pineapple = Make("Sweet Pineapple", "SC-PIN-2003", ProductCategory.Fruits,
            "Golden, low-acid pineapples with 15° Brix sweetness, harvested at peak ripeness.",
            "piece", 1_000m, 700m, 180, "Edo, Nigeria", "/media/produce/pineapple.svg", tunde, ProductStage.Distribution, true, 6);

        var eggs = Make("Farm Fresh Eggs", "SC-EGG-3001", ProductCategory.Livestock,
            "Free-range eggs collected daily, candled and packed in 30-egg crates.",
            "crate", 4_200m, 3_600m, 60, "Ogun, Nigeria", "/media/produce/eggs.svg", chioma, ProductStage.Retail, true, 5);

        var milk = Make("Whole Milk", "SC-MIL-3002", ProductCategory.Dairy,
            "Fresh, pasteurised whole milk in returnable glass bottles. Delivered cold.",
            "litre", 2_800m, 2_100m, 120, "Kaduna, Nigeria", "/media/produce/milk.svg", chioma, ProductStage.Retail, false, 5);

        var plantain = Make("Sweet Plantain", "SC-PLA-3003", ProductCategory.Fruits,
            "Ripe, sweet plantain bunches — perfect for dodo, boiling or chips.",
            "bunch", 1_500m, 1_100m, 200, "Edo, Nigeria", "/media/produce/plantain.svg", emeka, ProductStage.Retail, true, 4);

        var honey = Make("Raw Wildflower Honey", "SC-HON-3004", ProductCategory.Other,
            "Unfiltered, single-origin honey from Kogi beekeepers. Thick, floral and pure.",
            "jar", 5_500m, 4_700m, 40, "Kogi, Nigeria", "/media/produce/honey.svg", emeka, ProductStage.Retail, false, 3);

        var products = new[]
        {
            yam, maize, rice, beans, potatoes, tomatoes, pepper, pineapple, eggs, milk, plantain, honey
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        var transfers = new List<SupplyChainTransfer>();

        void Listed(Product p, AppUser farmer, int daysAgo) => transfers.Add(new SupplyChainTransfer
        {
            ProductId = p.Id,
            FromUserId = null,
            ToUserId = farmer.Id,
            FromStage = ProductStage.Farm,
            ToStage = ProductStage.Farm,
            UnitPrice = p.CostPrice > 0 ? p.CostPrice : p.Price,
            Quantity = p.InitialQuantity,
            Note = "Harvest listed",
            CreatedAt = now.AddDays(-daysAgo)
        });

        void Handoff(Product p, AppUser from, AppUser to, ProductStage fromStage, ProductStage toStage,
            decimal price, int daysAgo, string note) => transfers.Add(new SupplyChainTransfer
        {
            ProductId = p.Id,
            FromUserId = from.Id,
            ToUserId = to.Id,
            FromStage = fromStage,
            ToStage = toStage,
            UnitPrice = price,
            Quantity = p.InitialQuantity,
            Note = note,
            CreatedAt = now.AddDays(-daysAgo)
        });

        Listed(yam, amina, 21);
        Listed(maize, musa, 18);
        Listed(rice, amina, 15);
        Listed(beans, musa, 12);
        Listed(potatoes, amina, 9);

        Listed(tomatoes, musa, 20);
        Handoff(tomatoes, musa, tunde, ProductStage.Farm, ProductStage.Distribution, 850m, 8, "Lot acquired");

        Listed(pepper, musa, 19);
        Handoff(pepper, musa, tunde, ProductStage.Farm, ProductStage.Distribution, 1_700m, 7, "Lot acquired");

        Listed(pineapple, musa, 17);
        Handoff(pineapple, musa, tunde, ProductStage.Farm, ProductStage.Distribution, 700m, 6, "Lot acquired");

        Listed(eggs, amina, 22);
        Handoff(eggs, amina, tunde, ProductStage.Farm, ProductStage.Distribution, 3_000m, 14, "Lot acquired");
        Handoff(eggs, tunde, chioma, ProductStage.Distribution, ProductStage.Retail, 3_600m, 5, "Lot acquired");

        Listed(milk, amina, 20);
        Handoff(milk, amina, tunde, ProductStage.Farm, ProductStage.Distribution, 1_800m, 12, "Lot acquired");
        Handoff(milk, tunde, chioma, ProductStage.Distribution, ProductStage.Retail, 2_100m, 5, "Lot acquired");

        Listed(plantain, musa, 23);
        Handoff(plantain, musa, tunde, ProductStage.Farm, ProductStage.Distribution, 1_100m, 15, "Lot acquired");
        Handoff(plantain, tunde, emeka, ProductStage.Distribution, ProductStage.Retail, 1_100m, 4, "Lot acquired");

        Listed(honey, amina, 30);
        Handoff(honey, amina, tunde, ProductStage.Farm, ProductStage.Distribution, 4_700m, 18, "Lot acquired");
        Handoff(honey, tunde, emeka, ProductStage.Distribution, ProductStage.Retail, 4_700m, 3, "Lot acquired");

        db.Transfers.AddRange(transfers);

        var eggsLine = new OrderItem
        {
            ProductId = eggs.Id,
            SellerId = chioma.Id,
            ProductName = eggs.Name,
            SellerName = chioma.StoreName,
            Unit = eggs.Unit,
            ImageUrl = eggs.ImageUrl,
            UnitPrice = eggs.Price,
            Quantity = 2
        };
        var milkLine = new OrderItem
        {
            ProductId = milk.Id,
            SellerId = chioma.Id,
            ProductName = milk.Name,
            SellerName = chioma.StoreName,
            Unit = milk.Unit,
            ImageUrl = milk.ImageUrl,
            UnitPrice = milk.Price,
            Quantity = 3
        };
        var plantainLine = new OrderItem
        {
            ProductId = plantain.Id,
            SellerId = emeka.Id,
            ProductName = plantain.Name,
            SellerName = emeka.StoreName,
            Unit = plantain.Unit,
            ImageUrl = plantain.ImageUrl,
            UnitPrice = plantain.Price,
            Quantity = 5
        };
        var honeyLine = new OrderItem
        {
            ProductId = honey.Id,
            SellerId = emeka.Id,
            ProductName = honey.Name,
            SellerName = emeka.StoreName,
            Unit = honey.Unit,
            ImageUrl = honey.ImageUrl,
            UnitPrice = honey.Price,
            Quantity = 1
        };
        var plantainLine2 = new OrderItem
        {
            ProductId = plantain.Id,
            SellerId = emeka.Id,
            ProductName = plantain.Name,
            SellerName = emeka.StoreName,
            Unit = plantain.Unit,
            ImageUrl = plantain.ImageUrl,
            UnitPrice = plantain.Price,
            Quantity = 3
        };

        var order1 = new Order
        {
            CustomerId = dayo.Id,
            Reference = "ORD-SEED-0001",
            Status = OrderStatus.Delivered,
            PaymentMethod = "Card",
            ShippingName = dayo.DisplayName,
            ShippingPhone = "0803 555 0142",
            ShippingAddress = "12 Bodija Estate Road",
            ShippingCity = "Ibadan",
            ShippingState = "Oyo",
            CreatedAt = now.AddMonths(-2).AddDays(-3),
            PaidAt = now.AddMonths(-2).AddDays(-3),
            DeliveredAt = now.AddMonths(-2).AddDays(-1)
        };
        order1.Items.Add(eggsLine);
        order1.Items.Add(milkLine);

        var order2 = new Order
        {
            CustomerId = dayo.Id,
            Reference = "ORD-SEED-0002",
            Status = OrderStatus.Shipped,
            PaymentMethod = "Cash on delivery",
            ShippingName = dayo.DisplayName,
            ShippingPhone = "0803 555 0142",
            ShippingAddress = "12 Bodija Estate Road",
            ShippingCity = "Ibadan",
            ShippingState = "Oyo",
            CreatedAt = now.AddDays(-2),
            PaidAt = now.AddDays(-2)
        };
        order2.Items.Add(plantainLine);
        order2.Items.Add(honeyLine);

        var order3 = new Order
        {
            CustomerId = zainab.Id,
            Reference = "ORD-SEED-0003",
            Status = OrderStatus.Paid,
            PaymentMethod = "Card",
            ShippingName = zainab.DisplayName,
            ShippingPhone = "0806 555 0177",
            ShippingAddress = "4 Maitama Close",
            ShippingCity = "Abuja",
            ShippingState = "FCT",
            CreatedAt = now.AddDays(-4),
            PaidAt = now.AddDays(-4)
        };
        order3.Items.Add(plantainLine2);

        var order4 = new Order
        {
            CustomerId = zainab.Id,
            Reference = "ORD-SEED-0004",
            Status = OrderStatus.Delivered,
            PaymentMethod = "Card",
            ShippingName = zainab.DisplayName,
            ShippingPhone = "0806 555 0177",
            ShippingAddress = "4 Maitama Close",
            ShippingCity = "Abuja",
            ShippingState = "FCT",
            CreatedAt = now.AddMonths(-4).AddDays(-2),
            PaidAt = now.AddMonths(-4).AddDays(-2),
            DeliveredAt = now.AddMonths(-4)
        };
        order4.Items.Add(new OrderItem
        {
            ProductId = eggs.Id,
            SellerId = chioma.Id,
            ProductName = eggs.Name,
            SellerName = chioma.StoreName,
            Unit = eggs.Unit,
            ImageUrl = eggs.ImageUrl,
            UnitPrice = eggs.Price,
            Quantity = 1
        });

        var orders = new[] { order1, order2, order3, order4 };
        foreach (var order in orders)
        {
            var subtotal = order.Items.Sum(x => x.LineTotal);
            order.Subtotal = subtotal;
            order.DeliveryFee = subtotal >= 100_000m ? 0 : 2_500m;
            order.Total = subtotal + order.DeliveryFee;
        }

        db.Orders.AddRange(orders);

        db.CartItems.Add(new CartItem
        {
            UserId = dayo.Id,
            ProductId = milk.Id,
            Quantity = 2,
            UnitPrice = milk.Price,
            AddedAt = now.AddHours(-6)
        });

        await db.SaveChangesAsync(ct);
    }

    private static AppUser User(
        IPasswordHasher hasher,
        string firstName,
        string lastName,
        string email,
        string username,
        UserRole role,
        string storeName,
        string state,
        DateTime createdAt) => new()
    {
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        Username = username,
        PhoneNumber = "0803 555 01" + Random.Shared.Next(10, 99),
        PasswordHash = hasher.Hash(DemoPassword),
        Role = role,
        StoreName = storeName,
        State = state,
        City = state,
        IsActive = true,
        IsVerified = true,
        CreatedAt = createdAt
    };
}
