using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

using SistemaHospitalar.Domain.Entities;

using SistemaHospitalar.Domain.Enums;



namespace SistemaHospitalar.Infrastructure.Persistence;



/// <summary>

/// Dados de demonstração do almoxarifado: produtos com saldo, lotes, entrada NF, saída FEFO e requisição.

/// Idempotente — não duplica entradas; complementa saídas se faltarem.

/// </summary>

public static class WarehouseDemoSeed

{

    public const string DemoMarker = "gth-warehouse-demo-v1";

    public const string OutboundDemoMarker = "gth-warehouse-outbound-demo-v1";

    public const string KitDemoName = "Kit Cirúrgico UTI — Demo";
    public const string KitEmergenciaDemoName = "Kit Emergência PS — Demo";
    public const string KitCurativoDemoName = "Kit Curativo Enfermaria — Demo";



    public static async Task EnsureAsync(

        AppDbContext db,

        ILogger logger,

        CancellationToken cancellationToken = default)

    {

        logger.LogInformation("Verificando dados de demonstração do almoxarifado...");



        var products = await EnsureDemoProductsAsync(db, cancellationToken);

        if (products.Count == 0)

        {

            logger.LogWarning("Nenhum produto disponível para seed do almoxarifado.");

            return;

        }



        await EnsureLowStockAlertsAsync(db, products, cancellationToken);



        var hasReceipt = await db.StockReceipts.AnyAsync(

            r => r.Notes != null && r.Notes.Contains(DemoMarker),

            cancellationToken);



        if (!hasReceipt)

        {

            await SeedReceiptAsync(db, products, logger, cancellationToken);

        }



        if (!await db.StockIssues.AnyAsync(

                i => i.Notes != null && i.Notes.Contains(OutboundDemoMarker),

                cancellationToken))

        {

            await SeedOutboundDemoAsync(db, products, logger, cancellationToken);

        }



        await EnsureExpiringLotsAsync(db, products, cancellationToken);



        if (!await db.StockRequisitions.AnyAsync(

                r => r.Notes != null && r.Notes.Contains(DemoMarker),

                cancellationToken))

        {

            await SeedRequisitionsAsync(db, products, cancellationToken);

        }



        await EnsureDemoKitsAsync(db, products, cancellationToken);

        await RepairProductMetadataAsync(db, cancellationToken);
    }

    public static async Task<int> RepairProductMetadataAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        var catalogBySku = DemoCatalog()
            .ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);

        var products = await db.Products
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var product in products)
        {
            var dirty = false;

            if (catalogBySku.TryGetValue(product.Sku, out var demo))
            {
                if (string.IsNullOrWhiteSpace(product.Category) && !string.IsNullOrWhiteSpace(demo.Category))
                {
                    product.Category = demo.Category;
                    dirty = true;
                }

                if (string.IsNullOrWhiteSpace(product.Manufacturer) && !string.IsNullOrWhiteSpace(demo.Manufacturer))
                {
                    product.Manufacturer = demo.Manufacturer;
                    dirty = true;
                }

                if (string.IsNullOrWhiteSpace(product.DefaultLocation) && !string.IsNullOrWhiteSpace(demo.DefaultLocation))
                {
                    product.DefaultLocation = demo.DefaultLocation;
                    dirty = true;
                }

                if (string.IsNullOrWhiteSpace(product.Presentation) && !string.IsNullOrWhiteSpace(demo.Presentation))
                {
                    product.Presentation = demo.Presentation;
                    dirty = true;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(product.Category))
                {
                    product.Category = product.Type switch
                    {
                        ProductType.Medication => "Medicamentos",
                        ProductType.Supply => "Material hospitalar",
                        ProductType.Product => "Equipamentos",
                        _ => "Geral",
                    };
                    dirty = true;
                }

                if (string.IsNullOrWhiteSpace(product.Manufacturer))
                {
                    product.Manufacturer = "Genérico";
                    dirty = true;
                }

                if (string.IsNullOrWhiteSpace(product.DefaultLocation))
                {
                    product.DefaultLocation = product.Type switch
                    {
                        ProductType.Medication => "Farmácia Central",
                        ProductType.Supply => "Almoxarifado Central — A1",
                        ProductType.Product => "Almoxarifado Central — B2",
                        _ => "Almoxarifado Central — A1",
                    };
                    dirty = true;
                }
            }

            if (dirty)
            {
                product.UpdatedAt = DateTime.UtcNow;
                changed++;
            }
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }



    private static async Task SeedRequisitionsAsync(

        AppDbContext db,

        List<Product> products,

        CancellationToken cancellationToken)

    {

        var reqProduct = products.FirstOrDefault(p => p.Sku == "SUP-GAZ") ?? products.First();

        var urgentProduct = products.FirstOrDefault(p => p.Sku == "MAT-SRG10") ?? products.Skip(1).First();

        var seq = await NextRequisitionSequenceAsync(db, cancellationToken);

        db.StockRequisitions.Add(new StockRequisition

        {

            SequenceNumber = seq,

            RequestNumber = $"REQ-DEMO-{DateTime.UtcNow:yyyyMMdd}-001",

            RequestingSector = PurchaseSector.Nursing,

            DestinationLocation = "UTI — Posto 1",

            RequestedBy = "Enfermagem UTI",

            RecipientName = "Maria Santos",

            Priority = StockRequisitionPriority.Normal,

            Status = StockRequisitionStatus.Pending,

            Notes = $"Requisição demo almoxarifado. {DemoMarker}",

            Items =

            [

                new StockRequisitionItem

                {

                    ProductId = reqProduct.Id,

                    Quantity = Math.Min(10, Math.Max(2, reqProduct.QuantityOnHand / 2)),

                    UnitPrice = reqProduct.AveragePurchasePrice,

                },

            ],

        });



        db.StockRequisitions.Add(new StockRequisition

        {

            SequenceNumber = seq + 1,

            RequestNumber = $"REQ-DEMO-{DateTime.UtcNow:yyyyMMdd}-002",

            RequestingSector = PurchaseSector.SurgeryCenter,

            DestinationLocation = "Centro Cirúrgico — Sala 1",

            RequestedBy = "CCIH",

            RecipientName = "Enf. Carla Souza",

            Priority = StockRequisitionPriority.High,

            Status = StockRequisitionStatus.Approved,

            Notes = $"Requisição aprovada demo. {DemoMarker}",

            Items =

            [

                new StockRequisitionItem

                {

                    ProductId = urgentProduct.Id,

                    Quantity = Math.Min(20, Math.Max(5, urgentProduct.QuantityOnHand / 3)),

                    UnitPrice = urgentProduct.AveragePurchasePrice,

                },

            ],

        });



        await db.SaveChangesAsync(cancellationToken);

    }



    private static async Task EnsureLowStockAlertsAsync(

        AppDbContext db,

        List<Product> products,

        CancellationToken cancellationToken)

    {

        var lowStockTargets = new Dictionary<string, decimal>

        {

            ["GER-CRACHA01"] = 5,

            ["MAT-MASK3"] = 6,

            ["CIR-KIT01"] = 8,

            ["MED-OME20"] = 12,

        };



        var changed = false;

        foreach (var product in products)

        {

            if (!lowStockTargets.TryGetValue(product.Sku, out var qty))

            {

                continue;

            }



            if (product.QuantityOnHand <= 0 || product.QuantityOnHand > product.MinimumStock)

            {

                product.QuantityOnHand = qty;

                product.UpdatedAt = DateTime.UtcNow;

                changed = true;

            }

        }



        if (changed)

        {

            await db.SaveChangesAsync(cancellationToken);

        }

    }



    private static async Task EnsureExpiringLotsAsync(

        AppDbContext db,

        List<Product> products,

        CancellationToken cancellationToken)

    {

        const string batchMarker = "LOTE-DEMO-EXP";

        if (await db.ProductLots.AnyAsync(

                l => l.BatchNumber.StartsWith(batchMarker),

                cancellationToken))

        {

            return;

        }



        var expirySoon = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(18);

        var targets = products

            .Where(p => p.Type == ProductType.Medication)

            .Take(3)

            .ToList();



        var i = 0;

        foreach (var product in targets)

        {

            i++;

            var batch = $"{batchMarker}-{i:D2}";

            var qty = 25m;

            product.QuantityOnHand += qty;

            product.UpdatedAt = DateTime.UtcNow;



            db.ProductLots.Add(new ProductLot

            {

                ProductId = product.Id,

                BatchNumber = batch,

                ExpiryDate = expirySoon.AddDays(i * 2),

                Manufacturer = product.Manufacturer ?? "Fabricante Demo",

                QuantityOnHand = qty,

                LocationName = product.DefaultLocation ?? "Farmácia Central",

                UnitCost = product.AveragePurchasePrice,

            });

        }



        if (targets.Count > 0)

        {

            await db.SaveChangesAsync(cancellationToken);

        }

    }



    private static async Task EnsureDemoKitsAsync(
        AppDbContext db,
        List<Product> products,
        CancellationToken cancellationToken)
    {
        var kitDefinitions = new (string Name, string PriceTable, (string Sku, decimal Qty)[] Items)[]
        {
            (
                KitDemoName,
                "Particular",
                [
                    ("SUP-GAZ", 2),
                    ("SUP-LUV-M", 1),
                    ("MAT-SRG10", 2),
                    ("MAT-MASK3", 1),
                    ("CIR-KIT01", 1),
                ]),
            (
                KitEmergenciaDemoName,
                "SUS",
                [
                    ("MAT-CAT18", 2),
                    ("MAT-SRG10", 5),
                    ("SUP-GAZ", 3),
                    ("MAT-MASK3", 2),
                    ("SUP-ALC500", 1),
                ]),
            (
                KitCurativoDemoName,
                "Convênio",
                [
                    ("SUP-GAZ", 4),
                    ("SUP-LUV-M", 2),
                    ("MAT-AG257", 3),
                    ("MAT-EQMAC", 1),
                ]),
        };

        foreach (var (name, priceTable, kitItems) in kitDefinitions)
        {
            await EnsureDemoKitAsync(db, products, name, priceTable, kitItems, cancellationToken);
        }
    }

    private static async Task EnsureDemoKitAsync(
        AppDbContext db,
        List<Product> products,
        string kitName,
        string priceTable,
        (string Sku, decimal Qty)[] kitItems,
        CancellationToken cancellationToken)
    {
        if (await db.ProductKits.AnyAsync(k => k.Name == kitName, cancellationToken))
        {
            return;
        }

        var resolved = kitItems
            .Select(item => (Product: products.FirstOrDefault(p => p.Sku == item.Sku), item.Qty))
            .Where(x => x.Product is not null)
            .ToList();

        if (resolved.Count < 2)
        {
            return;
        }

        var kit = new ProductKit
        {
            Name = kitName,
            PriceTable = priceTable,
            Items = resolved.Select(x => new ProductKitItem
            {
                ProductId = x.Product!.Id,
                Quantity = x.Qty,
                UnitPrice = x.Product.AverageSalePrice,
            }).ToList(),
        };

        db.ProductKits.Add(kit);
        await db.SaveChangesAsync(cancellationToken);
    }



    private static async Task SeedReceiptAsync(

        AppDbContext db,

        List<Product> products,

        ILogger logger,

        CancellationToken cancellationToken)

    {

        logger.LogInformation("Aplicando entrada NF de demonstração do almoxarifado...");



        var receipt = new StockReceipt

        {

            SupplierName = "Distribuidora Hospitalar Demo LTDA",

            SupplierCnpj = "12345678000199",

            InvoiceNumber = "NF-DEMO-202607-001",

            InvoiceSeries = "1",

            InvoiceIssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),

            NfeAccessKey = "35260712345678000199550010000000011234567890",

            ReceivedAt = DateTime.UtcNow.AddDays(-7),

            ReceivedByUserName = "almoxarife@hospital.local",

            FreightAmount = 125.50m,

            DiscountAmount = 50.00m,

            PaymentCondition = "30/60/90",

            Notes = $"Entrada inicial de demonstração. {DemoMarker}",

        };



        decimal total = 0m;

        foreach (var (product, batch, qty, unitPrice, expiry, location) in BuildReceiptLines(products))

        {

            product.QuantityOnHand += qty;

            product.UpdatedAt = DateTime.UtcNow;



            var lot = await db.ProductLots

                .FirstOrDefaultAsync(

                    l => l.ProductId == product.Id && l.BatchNumber == batch && l.IsActive,

                    cancellationToken);



            if (lot is null)

            {

                lot = new ProductLot

                {

                    ProductId = product.Id,

                    BatchNumber = batch,

                    ExpiryDate = expiry,

                    Manufacturer = product.Manufacturer ?? "Fabricante Demo",

                    QuantityOnHand = qty,

                    LocationName = location,

                    UnitCost = unitPrice,

                };

                db.ProductLots.Add(lot);

            }

            else

            {

                lot.QuantityOnHand += qty;

                lot.LocationName ??= location;

                lot.UpdatedAt = DateTime.UtcNow;

            }



            var lineTotal = qty * unitPrice;

            total += lineTotal;



            receipt.Items.Add(new StockReceiptItem

            {

                StockReceipt = receipt,

                ProductId = product.Id,

                ProductLot = lot,

                BatchNumber = batch,

                ExpiryDate = expiry,

                Quantity = qty,

                UnitPrice = unitPrice,

                LineTotal = lineTotal,

                Ncm = product.Type == ProductType.Medication ? "30049099" : "40141000",

                Cfop = "5102",

            });



            db.StockMovements.Add(new StockMovement

            {

                ProductId = product.Id,

                Type = StockMovementType.Inbound,

                Quantity = qty,

                Reason = "Entrada NF demonstração",

                Reference = receipt.InvoiceNumber,

                PatientOrSupplier = receipt.SupplierName,

                BatchNumber = batch,

                ExpiryDate = expiry,

                InvoiceNumber = receipt.InvoiceNumber,

                UnitPrice = unitPrice,

                Location = location,

            });

        }



        receipt.TotalAmount = total + receipt.FreightAmount - receipt.DiscountAmount;

        db.StockReceipts.Add(receipt);

        await db.SaveChangesAsync(cancellationToken);



        logger.LogInformation(

            "Almoxarifado demo: {ProductCount} produtos com saldo, entrada NF {Invoice}, lotes vinculados.",

            products.Count,

            receipt.InvoiceNumber);

    }



    private static async Task SeedOutboundDemoAsync(

        AppDbContext db,

        List<Product> products,

        ILogger logger,

        CancellationToken cancellationToken)

    {

        logger.LogInformation("Aplicando saída FEFO de demonstração (UTI/enfermagem)...");



        var issueTargets = products

            .Where(p => p.QuantityOnHand > 5)

            .OrderBy(p => p.Type)

            .Take(6)

            .ToList();



        if (issueTargets.Count == 0)

        {

            logger.LogWarning("Sem saldo para saída demo do almoxarifado.");

            return;

        }



        var issue = new StockIssue

        {

            SectorName = "UTI — Posto 1",

            ResponsibleName = "Enf. Maria Santos",

            IssueType = StockIssueType.Consumption,

            Notes = $"Saída FEFO demo para UTI. {OutboundDemoMarker}",

        };



        foreach (var product in issueTargets)

        {

            var qty = Math.Min(8, Math.Max(2, product.QuantityOnHand / 10));

            if (qty <= 0 || product.QuantityOnHand < qty)

            {

                continue;

            }



            var lots = await db.ProductLots

                .Where(l => l.ProductId == product.Id && l.IsActive && l.QuantityOnHand > 0)

                .OrderBy(l => l.ExpiryDate)

                .ToListAsync(cancellationToken);



            Guid? lotId = lots.FirstOrDefault()?.Id;

            if (product.Type == ProductType.Medication && lots.Count > 0)

            {

                var lot = lots.First();

                var deduct = Math.Min(qty, lot.QuantityOnHand);

                lot.QuantityOnHand -= deduct;

                lot.UpdatedAt = DateTime.UtcNow;

                lotId = lot.Id;

            }



            product.QuantityOnHand -= qty;

            product.UpdatedAt = DateTime.UtcNow;



            issue.Items.Add(new StockIssueItem

            {

                ProductId = product.Id,

                ProductLotId = lotId,

                Quantity = qty,

            });



            db.StockMovements.Add(new StockMovement

            {

                ProductId = product.Id,

                Type = StockMovementType.Outbound,

                Quantity = qty,

                Reason = "Saída consumo UTI — demonstração",

                Reference = "SAI-DEMO-UTI-001",

                PatientOrSupplier = "UTI — Posto 1",

                BatchNumber = lots.FirstOrDefault()?.BatchNumber,

                ExpiryDate = lots.FirstOrDefault()?.ExpiryDate,

                Location = product.DefaultLocation ?? "Almoxarifado Central — A1",

            });

        }



        if (issue.Items.Count == 0)

        {

            return;

        }



        db.StockIssues.Add(issue);



        var nursingIssue = new StockIssue

        {

            SectorName = "Enfermaria — Ala A",

            ResponsibleName = "Enf. Carla Souza",

            IssueType = StockIssueType.Transfer,

            Notes = $"Transferência demo enfermagem. {OutboundDemoMarker}",

        };



        foreach (var product in issueTargets.Skip(2).Take(3))

        {

            var qty = Math.Min(5, Math.Max(1, product.QuantityOnHand / 15));

            if (qty <= 0 || product.QuantityOnHand < qty)

            {

                continue;

            }



            product.QuantityOnHand -= qty;

            product.UpdatedAt = DateTime.UtcNow;



            nursingIssue.Items.Add(new StockIssueItem

            {

                ProductId = product.Id,

                Quantity = qty,

            });



            db.StockMovements.Add(new StockMovement

            {

                ProductId = product.Id,

                Type = StockMovementType.Outbound,

                Quantity = qty,

                Reason = "Transferência enfermagem — demonstração",

                Reference = "SAI-DEMO-ENF-001",

                PatientOrSupplier = "Enfermaria — Ala A",

                Location = "Enfermaria — Ala A",

            });

        }



        if (nursingIssue.Items.Count > 0)

        {

            db.StockIssues.Add(nursingIssue);

        }



        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Saídas demo registradas: UTI + enfermagem ({ItemCount} itens).", issue.Items.Count);

    }



    private static async Task<List<Product>> EnsureDemoProductsAsync(

        AppDbContext db,

        CancellationToken cancellationToken)

    {

        var catalog = DemoCatalog();

        var existingSkus = await db.Products

            .Where(p => catalog.Select(c => c.Sku).Contains(p.Sku))

            .ToDictionaryAsync(p => p.Sku, cancellationToken);



        var result = new List<Product>();

        foreach (var demo in catalog)

        {

            if (existingSkus.TryGetValue(demo.Sku, out var existing))

            {

                if (existing.QuantityOnHand <= 0)

                {

                    existing.QuantityOnHand = demo.QuantityOnHand;

                    existing.MinimumStock = demo.MinimumStock;

                    existing.UpdatedAt = DateTime.UtcNow;

                }



                if (string.IsNullOrWhiteSpace(existing.DefaultLocation))

                {

                    existing.DefaultLocation = demo.DefaultLocation;

                    existing.UpdatedAt = DateTime.UtcNow;

                }



                if (string.IsNullOrWhiteSpace(existing.Category))

                {

                    existing.Category = demo.Category;

                    existing.UpdatedAt = DateTime.UtcNow;

                }



                if (string.IsNullOrWhiteSpace(existing.Manufacturer))

                {

                    existing.Manufacturer = demo.Manufacturer;

                    existing.UpdatedAt = DateTime.UtcNow;

                }



                if (string.IsNullOrWhiteSpace(existing.Presentation))

                {

                    existing.Presentation = demo.Presentation;

                    existing.UpdatedAt = DateTime.UtcNow;

                }



                result.Add(existing);

                continue;

            }



            db.Products.Add(demo);

            result.Add(demo);

        }



        await db.SaveChangesAsync(cancellationToken);

        return result;

    }



    private static Product[] DemoCatalog() =>
    [
        DemoProduct("Dipirona 500mg", "MED-DIP500", ProductType.Medication, "CP", 500, 100, "Frasco", "EMS", "Farmácia Central"),
        DemoProduct("Soro Fisiológico 500ml", "MED-SF500", ProductType.Medication, "FR", 200, 50, "Bolsa", "Baxter", "Farmácia Central"),
        DemoProduct("Omeprazol 20mg", "MED-OME20", ProductType.Medication, "CP", 120, 80, "Caixa", "Medley", "Farmácia UTI"),
        DemoProduct("Paracetamol 750mg", "MED-PAR750", ProductType.Medication, "CP", 300, 60, "Caixa", "Neo Química", "Farmácia Central"),
        DemoProduct("Luvas procedimento M", "SUP-LUV-M", ProductType.Supply, "CX", 45, 15, "Caixa 100un", "Supermax", "Almoxarifado Central — A1"),
        DemoProduct("Gaze estéril", "SUP-GAZ", ProductType.Supply, "PC", 180, 40, "Pacote", "Cremer", "Almoxarifado Central — B2"),
        DemoProduct("Seringa 10ml", "MAT-SRG10", ProductType.Supply, "UN", 250, 80, "Unidade", "BD", "UTI — Posto 1"),
        DemoProduct("Agulha 25x7", "MAT-AG257", ProductType.Supply, "UN", 400, 100, "Unidade", "BD", "UTI — Posto 2"),
        DemoProduct("Cateter venoso 18G", "MAT-CAT18", ProductType.Supply, "UN", 90, 30, "Unidade", "Helm", "Pronto-Socorro"),
        DemoProduct("Desinfetante hospitalar 5L", "CCIH-DESINF", ProductType.Supply, "GL", 12, 8, "Galão", "Lysoform", "CCIH"),
        DemoProduct("Lençol hospitalar", "HOTEL-LEN", ProductType.Supply, "UN", 60, 25, "Unidade", "Karsten", "Hotelaria"),
        DemoProduct("Kit cirúrgico descartável", "CIR-KIT01", ProductType.Supply, "KIT", 35, 20, "Kit", "Descarpack", "Centro Cirúrgico — Sala 1"),
        DemoProduct("Máscara cirúrgica tripla", "MAT-MASK3", ProductType.Supply, "CX", 28, 12, "Caixa 50un", "3M", "Centro Cirúrgico — Sala 2"),
        DemoProduct("Equipo macrogotas", "MAT-EQMAC", ProductType.Supply, "UN", 75, 25, "Unidade", "Descarpack", "Enfermaria — Ala A"),
        DemoProduct("Álcool gel 500ml", "SUP-ALC500", ProductType.Supply, "FR", 48, 20, "Frasco", "Needs", "Almoxarifado Central — A1"),
        DemoProduct("Monitor multiparamétrico portátil", "PRD-MON01", ProductType.Product, "UN", 8, 2, "Unidade", "Philips", "UTI — Posto 1"),
        DemoProduct("Bomba de infusão", "PRD-BOMBA01", ProductType.Product, "UN", 12, 4, "Unidade", "Braun", "UTI — Posto 2"),
        DemoProduct("Cadeira de rodas", "PRD-CADEIRA01", ProductType.Product, "UN", 15, 5, "Unidade", "Jaguaribe", "Ambulatório — Consultório 101"),
        DemoProduct("Oxímetro de pulso", "PRD-OXI01", ProductType.Product, "UN", 25, 8, "Unidade", "Medix", "Pronto-Socorro"),
        DemoProduct("Termômetro digital", "PRD-TERM01", ProductType.Product, "UN", 30, 10, "Unidade", "G-Tech", "Enfermaria — Ala A"),
        DemoProduct("Material de escritório hospitalar", "GER-ESC01", ProductType.General, "KIT", 20, 5, "Kit", "Genérico", "Almoxarifado Central — B2"),
        DemoProduct("Uniforme hospitalar", "GER-UNIF01", ProductType.General, "UN", 40, 15, "Unidade", "Karsten", "Hotelaria"),
        DemoProduct("Crachá identificação", "GER-CRACHA01", ProductType.General, "UN", 100, 30, "Unidade", "Genérico", "Recepção"),
        DemoProduct("Etiqueta código de barras", "GER-ETIQ01", ProductType.General, "CX", 18, 6, "Caixa 500un", "Pimaco", "Almoxarifado Central — A1"),
        DemoProduct("Papel A4 hospitalar", "GER-PAPEL01", ProductType.General, "CX", 22, 8, "Caixa 10 resmas", "Chamex", "Administrativo"),
    ];

    private static Product DemoProduct(

        string name,

        string sku,

        ProductType type,

        string unit,

        decimal qty,

        decimal min,

        string presentation,

        string manufacturer,

        string defaultLocation)

        => new()

        {

            Name = name,

            Sku = sku,

            Type = type,

            Unit = unit,

            QuantityOnHand = qty,

            MinimumStock = min,

            MaximumStock = min * 10,

            Presentation = presentation,

            Manufacturer = manufacturer,

            Category = type switch

            {

                ProductType.Medication => "Medicamentos",

                ProductType.Supply => "Material hospitalar",

                ProductType.Product => "Equipamentos",

                _ => "Geral",

            },

            DefaultLocation = defaultLocation,

            ExpiryWarningDays = 30,

            AveragePurchasePrice = type == ProductType.Medication ? 12.50m : 8.90m,

            AverageSalePrice = type == ProductType.Medication ? 18.00m : 14.50m,

            AllowOutboundFromRegister = true,

        };



    private static IEnumerable<(Product Product, string Batch, decimal Qty, decimal UnitPrice, DateOnly Expiry, string Location)>

        BuildReceiptLines(IReadOnlyList<Product> products)

    {

        var expiryMed = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(18);

        var expiryMat = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(36);

        var i = 0;

        foreach (var p in products.Take(12))

        {

            i++;

            var batch = $"LOTE-DEMO-2026-{i:D3}";

            var expiry = p.Type == ProductType.Medication ? expiryMed : expiryMat;

            var qty = Math.Min(50, p.QuantityOnHand / 4 + 10);

            var location = p.DefaultLocation ?? "Almoxarifado Central — A1";

            yield return (p, batch, qty, p.AveragePurchasePrice > 0 ? p.AveragePurchasePrice : 10m, expiry, location);

        }

    }



    private static async Task<int> NextRequisitionSequenceAsync(AppDbContext db, CancellationToken cancellationToken)

    {

        var max = await db.StockRequisitions.MaxAsync(r => (int?)r.SequenceNumber, cancellationToken) ?? 0;

        return max + 1;

    }

}


