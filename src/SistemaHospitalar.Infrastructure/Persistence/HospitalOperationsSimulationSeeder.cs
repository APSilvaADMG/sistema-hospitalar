using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services.Payroll;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Simulação operacional completa: farmácia, estoque, faturamento de produtos e financeiro ampliado.
/// </summary>
public static class HospitalOperationsSimulationSeeder
{
    public const string SkuPrefix = "GTH-LOAD-";

    private static readonly (string Name, string Presentation, string Unit, string Manufacturer)[] MedicationCatalog =
    [
        ("Dipirona", "500mg comp", "UN", "Medley"),
        ("Omeprazol", "20mg cáps", "UN", "Eurofarma"),
        ("Losartana", "50mg comp", "UN", "EMS"),
        ("Metformina", "850mg comp", "UN", "Sandoz"),
        ("Insulina NPH", "100UI/mL fr", "FR", "Novo Nordisk"),
        ("Amoxicilina", "500mg cáps", "UN", "Teuto"),
        ("Paracetamol", "750mg comp", "UN", "Neo Química"),
        ("Ibuprofeno", "600mg comp", "UN", "Bayer"),
        ("Captopril", "25mg comp", "UN", "Biolab"),
        ("Enalapril", "10mg comp", "UN", "Merck"),
        ("Atenolol", "50mg comp", "UN", "AstraZeneca"),
        ("Propranolol", "40mg comp", "UN", "Cristália"),
        ("Hidroclorotiazida", "25mg comp", "UN", "EMS"),
        ("Furosemida", "40mg comp", "UN", "Sanofi"),
        ("Espironolactona", "25mg comp", "UN", "Pfizer"),
        ("Sinvastatina", "20mg comp", "UN", "Merck"),
        ("Atorvastatina", "20mg comp", "UN", "Pfizer"),
        ("AAS", "100mg comp", "UN", "Bayer"),
        ("Clopidogrel", "75mg comp", "UN", "Sanofi"),
        ("Warfarina", "5mg comp", "UN", "União Química"),
        ("Heparina", "5000UI/mL amp", "AMP", "Cristália"),
        ("Enoxaparina", "40mg seringa", "UN", "Sanofi"),
        ("Prednisona", "20mg comp", "UN", "EMS"),
        ("Dexametasona", "4mg comp", "UN", "Aché"),
        ("Hidrocortisona", "100mg fr", "FR", "Fresenius"),
        ("Ranitidina", "150mg comp", "UN", "Medley"),
        ("Pantoprazol", "40mg comp", "UN", "Eurofarma"),
        ("Domperidona", "10mg comp", "UN", "Janssen"),
        ("Metoclopramida", "10mg comp", "UN", "Teuto"),
        ("Ondansetrona", "8mg comp", "UN", "Novartis"),
        ("Lorazepam", "2mg comp", "UN", "Biolab"),
        ("Diazepam", "10mg comp", "UN", "Cristália"),
        ("Haloperidol", "5mg comp", "UN", "Janssen"),
        ("Amitriptilina", "25mg comp", "UN", "EMS"),
        ("Sertralina", "50mg comp", "UN", "Pfizer"),
        ("Fluoxetina", "20mg cáps", "UN", "EMS"),
        ("Tramadol", "50mg cáps", "UN", "Cristália"),
        ("Morfina", "10mg/mL amp", "AMP", "Cristália"),
        ("Codeína", "30mg comp", "UN", "Teuto"),
        ("Cefalexina", "500mg cáps", "UN", "EMS"),
        ("Azitromicina", "500mg comp", "UN", "Pfizer"),
        ("Ciprofloxacino", "500mg comp", "UN", "Bayer"),
        ("Gentamicina", "80mg/2mL amp", "AMP", "União Química"),
        ("Vancomicina", "500mg fr", "FR", "Eurofarma"),
        ("Meropeném", "1g fr", "FR", "Pfizer"),
        ("Piperacilina-Tazobactam", "4,5g fr", "FR", "Wyeth"),
        ("Fluconazol", "150mg cáps", "UN", "EMS"),
        ("Aciclovir", "200mg comp", "UN", "Merck"),
        ("Oseltamivir", "75mg cáps", "UN", "Roche"),
        ("Salbutamol", "100mcg/dose inal", "UN", "GSK"),
        ("Budesonida", "200mcg/dose inal", "UN", "AstraZeneca"),
        ("Levotiroxina", "50mcg comp", "UN", "Merck"),
        ("Carvedilol", "25mg comp", "UN", "Roche"),
        ("Nitroglicerina", "5mg comp SL", "UN", "Cristália"),
        ("Ceftriaxona", "1g fr", "FR", "Roche"),
        ("Ampicilina", "500mg cáps", "UN", "Teuto"),
        ("Clindamicina", "300mg cáps", "UN", "Pfizer"),
        ("Metronidazol", "250mg comp", "UN", "Sanofi"),
        ("Rivaroxabana", "20mg comp", "UN", "Bayer"),
        ("Gabapentina", "300mg cáps", "UN", "Pfizer"),
        ("Escitalopram", "10mg comp", "UN", "Lundbeck"),
        ("Quetiapina", "25mg comp", "UN", "AstraZeneca"),
        ("Clonazepam", "2mg comp", "UN", "Roche"),
        ("Adrenalina", "1mg/mL amp", "AMP", "Hipolabor"),
        ("Soro fisiológico", "0,9% 500mL", "UN", "Fresenius"),
        ("Soro glicosado", "5% 500mL", "UN", "Baxter"),
        ("Midazolam", "5mg/mL amp", "AMP", "Cristália"),
        ("Propofol", "10mg/mL amp", "AMP", "Fresenius"),
        ("Noradrenalina", "2mg/mL amp", "AMP", "Hipolabor"),
        ("Amiodarona", "150mg/3mL amp", "AMP", "Sanofi"),
        ("Esomeprazol", "40mg comp", "UN", "AstraZeneca"),
        ("Cetoprofeno", "100mg comp", "UN", "EMS"),
        ("Levofloxacino", "500mg comp", "UN", "Sanofi"),
        ("Claritromicina", "500mg comp", "UN", "Abbott"),
        ("Linezolida", "600mg comp", "UN", "Pfizer"),
        ("Insulina glargina", "100UI/mL caneta", "UN", "Sanofi"),
        ("Empagliflozina", "25mg comp", "UN", "Boehringer"),
        ("Montelucaste", "10mg comp", "UN", "MSD"),
        ("Loratadina", "10mg comp", "UN", "Schering-Plough"),
        ("Acetilcisteína", "600mg sachê", "UN", "Zambon"),
        ("Lidocaína", "2% 20mL amp", "AMP", "Hipolabor"),
        ("Rocurônio", "10mg/mL fr", "FR", "Organon"),
        ("Fentanil", "50mcg/mL amp", "AMP", "Cristália"),
        ("Vitamina K", "10mg/mL amp", "AMP", "Hipolabor"),
        ("Gluconato de cálcio", "10% 10mL amp", "AMP", "Hipolabor"),
        ("Solução Ringer lactato", "500mL", "UN", "Baxter"),
        ("Omeprazol IV", "40mg fr", "FR", "Eurofarma"),
        ("Ondansetrona IV", "8mg/4mL amp", "AMP", "Novartis"),
        ("Dexametasona IV", "4mg/mL amp", "AMP", "Aché"),
        ("Furosemida IV", "10mg/mL amp", "AMP", "Sanofi"),
        ("Heparina não fracionada", "5000UI/mL amp", "AMP", "Cristália"),
        ("Amoxicilina + Clavulanato", "875/125mg comp", "UN", "GSK"),
        ("Dipirona gotas", "500mg/mL fr", "FR", "Medley"),
        ("Paracetamol gotas", "200mg/mL fr", "FR", "Neo Química"),
        ("Prednisolona susp", "3mg/mL fr", "FR", "EMS"),
        ("Salbutamol nebulização", "5mg/mL amp", "AMP", "GSK"),
        ("Ivermectina", "6mg comp", "UN", "Vitamedic"),
        ("Nimesulida", "100mg comp", "UN", "Medley"),
        ("Meloxicam", "15mg comp", "UN", "Boehringer"),
        ("Bromoprida", "10mg comp", "UN", "EMS"),
        ("Lactulose", "667mg/mL fr", "FR", "Abbott"),
        ("Digoxina", "0,25mg comp", "UN", "Sanofi"),
        ("Carbamazepina", "200mg comp", "UN", "Novartis"),
        ("Valproato", "250mg comp", "UN", "Abbott"),
        ("Bupropiona", "150mg comp", "UN", "GSK"),
        ("Olanzapina", "10mg comp", "UN", "Eli Lilly"),
        ("Metadona", "10mg comp", "UN", "Cristália"),
        ("Naloxona", "0,4mg/mL amp", "AMP", "Cristália"),
        ("Filgrastim", "300mcg seringa", "UN", "Amgen"),
        ("Eritropoetina", "4000UI seringa", "UN", "Roche"),
        ("Albumina humana", "20% 50mL fr", "FR", "CSL Behring"),
        ("Nutrição parenteral", "bag 2L", "UN", "Baxter"),
        ("Clorexidina degermante", "2% 100mL", "UN", "Rioquímica"),
        ("Álcool gel 70%", "500mL", "UN", "Rioquímica"),
        ("Sugamadex", "100mg/mL fr", "FR", "MSD"),
        ("Remifentanil", "1mg fr", "FR", "GSK"),
        ("Dexmedetomidina", "100mcg/mL fr", "FR", "Pfizer"),
        ("Cisatracúrio", "2mg/mL amp", "AMP", "GSK"),
        ("Succinilcolina", "100mg fr", "FR", "Cristália"),
        ("Cetamina", "50mg/mL fr", "FR", "Cristália"),
        ("Ácido tranexâmico", "250mg/5mL amp", "AMP", "Pfizer"),
        ("Desmopressina", "0,2mg comp", "UN", "Ferring"),
        ("Sitagliptina", "100mg comp", "UN", "MSD"),
        ("Gliclazida", "60mg comp", "UN", "Servier"),
        ("Glibenclamida", "5mg comp", "UN", "EMS"),
        ("Metimazol", "10mg comp", "UN", "EMS"),
        ("Prednisolona", "20mg comp", "UN", "EMS"),
        ("Betametasona", "4mg comp", "UN", "EMS"),
        ("Permetrina", "5% loção", "UN", "EMS"),
        ("Albendazol", "400mg comp", "UN", "GSK"),
        ("Mebendazol", "100mg comp", "UN", "Janssen"),
        ("Artesunato", "60mg amp", "AMP", "Farmanguinhos"),
        ("Desloratadina", "5mg comp", "UN", "MSD"),
        ("Cetirizina", "10mg comp", "UN", "UCB"),
        ("Formoterol", "12mcg cáps inal", "UN", "Novartis"),
        ("Tiotrópio", "18mcg cáps inal", "UN", "Boehringer"),
        ("Ambroxol", "30mg comp", "UN", "Boehringer"),
        ("Dextrometorfano", "15mg comp", "UN", "EMS"),
        ("Beclometasona", "200mcg/dose inal", "UN", "GSK"),
        ("Budesonida nasal", "64mcg/dose spray", "UN", "AstraZeneca"),
        ("Fexofenadina", "120mg comp", "UN", "Sanofi"),
        ("Teofilina", "200mg comp", "UN", "EMS"),
        ("Codeína xarope", "3mg/mL fr", "FR", "Teuto"),
        ("Dobutamina", "250mg/20mL fr", "FR", "Lilly"),
        ("Milrinona", "10mg/10mL amp", "AMP", "Sanofi"),
        ("Vasopressina", "20UI/mL amp", "AMP", "Ferring"),
        ("Dopamina", "50mg/10mL amp", "AMP", "Cristália"),
        ("Nitroprussiato", "50mg fr", "FR", "Cristália"),
        ("Protamina", "10mg/mL amp", "AMP", "Hipolabor"),
        ("Manitol", "20% 250mL", "UN", "Baxter"),
        ("Cloreto de sódio hipertônico", "3% 250mL", "UN", "Fresenius"),
        ("Potássio cloreto", "19,1% 10mL amp", "AMP", "Cristália"),
        ("Bicarbonato de sódio", "8,4% 10mL amp", "AMP", "Halex"),
        ("Povidona iodada", "10% 100mL", "UN", "Rioquímica"),
        ("Bupivacaína", "0,5% 20mL amp", "AMP", "Cristália"),
        ("Ropivacaína", "7,5mg/mL amp", "AMP", "AstraZeneca"),
        ("Neostigmina", "0,5mg/mL amp", "AMP", "Cristália"),
        ("Atropina", "0,25mg/mL amp", "AMP", "Hipolabor"),
        ("Etomidato", "2mg/mL amp", "AMP", "Janssen"),
        ("Nalbufina", "10mg/mL amp", "AMP", "Cristália"),
        ("Petidina", "50mg/mL amp", "AMP", "Cristália"),
        ("Hioscina", "20mg/mL amp", "AMP", "Cristália"),
        ("Difenidramina", "50mg/mL amp", "AMP", "EMS"),
        ("Prometazina IV", "25mg/mL amp", "AMP", "Sanofi"),
        ("Cefuroxima", "750mg fr", "FR", "GSK"),
        ("Colistina", "1MI UI fr", "FR", "Cristália"),
        ("Voriconazol", "200mg comp", "UN", "Pfizer"),
        ("Sulfametoxazol-Trimetoprima", "400/80mg comp", "UN", "EMS"),
        ("Nitrofurantoína", "100mg cáps", "UN", "EMS"),
        ("Fenobarbital", "100mg comp", "UN", "Cristália"),
        ("Levetiracetam", "500mg comp", "UN", "UCB"),
        ("Venlafaxina", "75mg cáps", "UN", "Pfizer"),
        ("Mirtazapina", "30mg comp", "UN", "Organon"),
        ("Risperidona", "2mg comp", "UN", "Janssen"),
        ("Alprazolam", "0,5mg comp", "UN", "Pfizer"),
        ("Zolpidem", "10mg comp", "UN", "Sanofi"),
        ("Tiamina", "300mg comp", "UN", "EMS"),
        ("Ácido fólico", "5mg comp", "UN", "EMS"),
        ("Ferro sulfato", "40mg comp", "UN", "EMS"),
        ("Semaglutida", "1mg caneta", "UN", "Novo Nordisk"),
        ("Insulina regular", "100UI/mL fr", "FR", "Novo Nordisk"),
        ("Norfloxacino", "400mg comp", "UN", "EMS"),
        ("Doxiciclina", "100mg comp", "UN", "EMS"),
        ("Rifampicina", "300mg cáps", "UN", "Sanofi"),
        ("Isoniazida", "300mg comp", "UN", "Fiocruz"),
        ("Anfotericina B", "50mg fr", "FR", "Gilead"),
        ("Apixabana", "5mg comp", "UN", "Pfizer"),
        ("Fenitoína", "100mg comp", "UN", "EMS"),
        ("Pregabalina", "75mg cáps", "UN", "Pfizer"),
        ("Clozapina", "100mg comp", "UN", "Novartis"),
        ("Buprenorfina", "2mg SL comp", "UN", "Mundipharma"),
        ("Naltrexona", "50mg comp", "UN", "EMS"),
        ("Cianocobalamina", "1mg/mL amp", "AMP", "Hipolabor"),
        ("Octreotida", "0,1mg/mL amp", "AMP", "Novartis"),
        ("Somatostatina", "3mg fr", "FR", "Pfizer"),
        ("Heparina de baixo peso", "60mg seringa", "UN", "Sanofi"),
        ("Nistatina", "100.000UI/mL susp", "FR", "Teuto"),
        ("Clotrimazol", "100mg creme", "UN", "Bayer"),
        ("Miconazol", "2% creme", "UN", "Janssen"),
        ("Aciclovir tópico", "5% pomada", "UN", "EMS"),
        ("Praziquantel", "600mg comp", "UN", "Bayer"),
        ("Quinina", "500mg comp", "UN", "Fiocruz"),
        ("Doxiciclina pediátrica", "100mg/5mL susp", "FR", "EMS"),
        ("Azitromicina susp", "200mg/5mL fr", "FR", "Pfizer"),
        ("Ibuprofeno gotas", "50mg/mL fr", "FR", "Bayer"),
        ("Amoxicilina susp", "250mg/5mL fr", "FR", "Teuto"),
        ("Cefalexina susp", "250mg/5mL fr", "FR", "EMS"),
        ("Azitromicina ped", "200mg/5mL fr", "FR", "Pfizer"),
        ("Dexametasona elixir", "0,1mg/mL fr", "FR", "Aché"),
        ("Ipratrópio nebulização", "0,25mg/mL amp", "AMP", "Boehringer"),
        ("Adrenalina nebulização", "1mg/mL amp", "AMP", "Hipolabor"),
        ("Budesonida nebulização", "0,5mg/mL amp", "AMP", "AstraZeneca"),
        ("Solução de manutenção", "500mL", "UN", "Baxter"),
        ("Solução polarizante", "500mL", "UN", "Baxter"),
        ("Albumina 5%", "250mL fr", "FR", "CSL Behring"),
        ("Imunoglobulina humana", "5g fr", "FR", "Grifols"),
        ("Concentrado de hemácias", "unidade", "UN", "Hemocentro"),
        ("Plasma fresco", "unidade", "UN", "Hemocentro"),
        ("Plaquetas", "unidade", "UN", "Hemocentro"),
        ("Crioprecipitado", "unidade", "UN", "Hemocentro"),
    ];

    private static readonly (string Name, string Presentation, string Unit, string Manufacturer)[] SupplyCatalog =
    [
        ("Seringa descartável 10mL", "10mL Luer lock", "UN", "Descarpack"),
        ("Agulha hipodérmica 25x7", "25G x 7mm", "UN", "Descarpack"),
        ("Luva procedimento M", "látex M", "PAR", "Supermax"),
        ("Máscara N95", "PFF2", "UN", "3M"),
        ("Sonda vesical Foley 16Fr", "silicone 2 vias", "UN", "Curity"),
        ("Equipo macro gotas", "macrogotas", "UN", "Descarpack"),
        ("Gaze estéril 7,5x7,5", "pacote 10 un", "PCT", "Cremer"),
        ("Atadura de crepe 10cm", "4,5m", "UN", "Cremer"),
        ("Esparadrapo micropore", "10cm x 4,5m", "UN", "3M"),
        ("Lâmina bisturi nº 22", "aço carbono", "UN", "B Braun"),
        ("Cateter venoso periférico", "18G", "UN", "B Braun"),
        ("Jelco 22G", "azul", "UN", "B Braun"),
        ("Algodão hidrófilo", "500g rolo", "UN", "Cremer"),
        ("Álcool 70% 1L", "solução", "UN", "Rioquímica"),
        ("Clorexidina aquosa 2%", "100mL", "UN", "Rioquímica"),
    ];

    private static readonly string[] ProposalBudgetLabels =
    [
        "consulta + exames laboratoriais",
        "cirurgia ambulatorial",
        "internação 3 diárias",
        "fisioterapia 10 sessões",
        "quimioterapia protocolo",
        "check-up executivo",
        "parto humanizado",
        "endoscopia digestiva",
        "ressonância magnética",
        "tomografia com contraste",
        "hemodiálise mensal",
        "odontologia hospitalar",
        "psicoterapia trimestral",
        "vacinação corporativa",
        "telemedicina anual"
    ];

    private sealed record ProductSeedInfo(Guid Id, ProductType Type, string Sku, decimal AverageSalePrice);
    private sealed record ProfessionalSeedInfo(Guid Id, string FullName);

    public static async Task RunAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        HospitalLoadDataOptions options,
        Random rnd,
        string marker,
        HospitalSimulationResult result,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var simulationStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-options.SimulationDays));

        result.ProductsCreated = await SeedProductsAsync(db, rnd, marker, logger, cancellationToken);
        var productIds = await db.Products
            .AsNoTracking()
            .Where(p => p.Sku.StartsWith(SkuPrefix))
            .Select(p => new ProductSeedInfo(p.Id, p.Type, p.Sku, p.AverageSalePrice))
            .ToListAsync(cancellationToken);

        result.StockMovementsCreated = await SeedStockMovementsAsync(
            db, productIds.Select(p => p.Id).ToList(), rnd, marker, simulationStart, logger, cancellationToken);

        result.ProductBillingRulesCreated = await SeedProductBillingRulesAsync(
            db, productIds.Where(p => p.Type == ProductType.Medication).Select(p => p.Id).ToList(),
            rnd, logger, cancellationToken);

        var professionals = await db.Professionals
            .AsNoTracking()
            .Select(p => new ProfessionalSeedInfo(p.Id, p.FullName))
            .ToListAsync(cancellationToken);

        var medicationProducts = productIds.Where(p => p.Type == ProductType.Medication).ToList();
        var dispensingStats = await SeedPharmacyDispensingAsync(
            db, markedPatientIds, medicationProducts, professionals, rnd, marker, logger, cancellationToken);
        result.PharmacyDispensingsCreated = dispensingStats.Dispensings;
        result.PharmacyBillingEntriesCreated = dispensingStats.BillingEntries;

        var financeStats = await SeedComprehensiveFinanceAsync(
            db, markedPatientIds, professionals, rnd, marker, options, logger, cancellationToken);
        result.FinancialAccountsCreated += financeStats.AccountsCreated;
        result.ProposalsCreated += financeStats.ProposalsCreated;
        result.HonorariosCreated = financeStats.HonorariosCreated;
        result.PayablesCreated += financeStats.PayablesCreated;
        result.FinancialPaymentsCreated += financeStats.PaymentsCreated;
        result.LineItemsCreated += financeStats.LineItemsCreated;
        result.MiscellaneousReceiptsCreated = financeStats.MiscellaneousReceiptsCreated;
        result.CashSessionsCreated = financeStats.CashSessionsCreated;
        result.PayrollRunsCreated = financeStats.PayrollRunsCreated;
        result.TpaClaimsCreated = financeStats.TpaClaimsCreated;
        result.TissGuidesCreated += financeStats.TissGuidesCreated;
        result.TissBatchesCreated += financeStats.TissBatchesCreated;
    }

    private static async Task<int> SeedProductsAsync(
        AppDbContext db,
        Random rnd,
        string marker,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existingSkus = await db.Products
            .AsNoTracking()
            .Where(p => p.Sku.StartsWith(SkuPrefix))
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);
        var existingSet = existingSkus.ToHashSet();

        var products = new List<Product>();
        var medIndex = 1;

        foreach (var med in MedicationCatalog.Take(50))
        {
            var sku = $"{SkuPrefix}MED-{medIndex:D3}";
            medIndex++;
            if (existingSet.Contains(sku))
            {
                continue;
            }

            var purchase = Math.Round((decimal)(rnd.NextDouble() * 40 + 2), 2);
            products.Add(new Product
            {
                Name = med.Name,
                Sku = sku,
                Type = ProductType.Medication,
                Unit = med.Unit,
                QuantityOnHand = 0,
                MinimumStock = rnd.Next(20, 120),
                MaximumStock = rnd.Next(500, 2000),
                Category = "Medicamentos",
                Manufacturer = med.Manufacturer,
                Barcode = $"789{rnd.Next(100000000, 999999999)}",
                Presentation = med.Presentation,
                DefaultLocation = "Farmácia Central",
                AveragePurchasePrice = purchase,
                AverageSalePrice = Math.Round(purchase * (decimal)(1.25 + rnd.NextDouble() * 0.5), 2),
                Description = $"{marker}|{med.Name}"
            });
        }

        var supplyIndex = 1;
        foreach (var supply in SupplyCatalog)
        {
            var sku = $"{SkuPrefix}SUP-{supplyIndex:D3}";
            supplyIndex++;
            if (existingSet.Contains(sku))
            {
                continue;
            }

            var purchase = Math.Round((decimal)(rnd.NextDouble() * 15 + 1), 2);
            products.Add(new Product
            {
                Name = supply.Name,
                Sku = sku,
                Type = ProductType.Supply,
                Unit = supply.Unit,
                QuantityOnHand = 0,
                MinimumStock = rnd.Next(50, 300),
                MaximumStock = rnd.Next(1000, 5000),
                Category = "Material hospitalar",
                Manufacturer = supply.Manufacturer,
                Barcode = $"789{rnd.Next(100000000, 999999999)}",
                Presentation = supply.Presentation,
                DefaultLocation = "Farmácia Central",
                AveragePurchasePrice = purchase,
                AverageSalePrice = Math.Round(purchase * (decimal)(1.15 + rnd.NextDouble() * 0.35), 2),
                Description = $"{marker}|{supply.Name}"
            });
        }

        if (products.Count == 0)
        {
            return 0;
        }

        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Simulação ops: {Count} produtos farmacêuticos criados.", products.Count);
        return products.Count;
    }

    private static async Task<int> SeedStockMovementsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> productIds,
        Random rnd,
        string marker,
        DateOnly simulationStart,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return 0;
        }

        var alreadySeeded = await db.StockMovements
            .AsNoTracking()
            .AnyAsync(m => m.Reference != null && m.Reference.StartsWith(marker), cancellationToken);

        if (alreadySeeded)
        {
            return 0;
        }

        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var movements = new List<StockMovement>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var product in products)
        {
            var onHand = 0m;
            var inboundCount = rnd.Next(1, 3);
            for (var i = 0; i < inboundCount; i++)
            {
                var qty = rnd.Next(50, 401);
                var daysAgo = rnd.Next(1, 61);
                var moveDate = today.AddDays(-daysAgo);
                if (moveDate < simulationStart)
                {
                    moveDate = simulationStart.AddDays(rnd.Next(0, 30));
                }

                var batch = $"L{moveDate:yyyyMMdd}-{rnd.Next(1000, 9999)}";
                movements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    Type = StockMovementType.Inbound,
                    Quantity = qty,
                    Reason = "Compra fornecedor",
                    Reference = $"{marker}|NF-{rnd.Next(10000, 99999)}",
                    PatientOrSupplier = "GTH Fornecedor Simulação",
                    ResponsibleName = "Farmácia Central",
                    UserName = "gth-simulation@load-seed.local",
                    BatchNumber = batch,
                    ExpiryDate = moveDate.AddMonths(rnd.Next(12, 37)),
                    InvoiceNumber = $"NF-{rnd.Next(100000, 999999)}",
                    UnitPrice = product.AveragePurchasePrice,
                    Location = product.DefaultLocation,
                    CreatedAt = moveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                });
                onHand += qty;
            }

            var outboundCount = rnd.Next(1, 4);
            for (var i = 0; i < outboundCount; i++)
            {
                var maxQty = Math.Max(1, (int)Math.Min(onHand, 80));
                if (maxQty <= 0)
                {
                    break;
                }

                var qty = rnd.Next(1, maxQty + 1);
                var daysAgo = rnd.Next(1, 45);
                var moveDate = today.AddDays(-daysAgo);

                movements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    Type = StockMovementType.Outbound,
                    Quantity = qty,
                    Reason = i % 2 == 0 ? "Dispensação ambulatorial" : "Consumo enfermaria",
                    Reference = $"{marker}|disp-{product.Sku}-{i}",
                    ResponsibleName = "Farmácia Central",
                    UserName = "gth-simulation@load-seed.local",
                    Location = product.DefaultLocation,
                    CreatedAt = moveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                });
                onHand -= qty;
            }

            product.QuantityOnHand = Math.Max(0, onHand);
        }

        if (movements.Count == 0)
        {
            return 0;
        }

        db.StockMovements.AddRange(movements);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Simulação ops: {Count} movimentações de estoque criadas.", movements.Count);
        return movements.Count;
    }

    private static async Task<int> SeedProductBillingRulesAsync(
        AppDbContext db,
        IReadOnlyList<Guid> medicationProductIds,
        Random rnd,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (medicationProductIds.Count == 0)
        {
            return 0;
        }

        var existingRuleProductIds = await db.ProductBillingRules
            .AsNoTracking()
            .Where(r => medicationProductIds.Contains(r.ProductId) && r.PriceTable == "CMED")
            .Select(r => r.ProductId)
            .ToListAsync(cancellationToken);
        var existingSet = existingRuleProductIds.ToHashSet();

        var products = await db.Products
            .AsNoTracking()
            .Where(p => medicationProductIds.Contains(p.Id))
            .Select(p => new { p.Id, p.AverageSalePrice, p.Sku })
            .ToListAsync(cancellationToken);

        var rules = new List<ProductBillingRule>();
        var target = Math.Min(40, products.Count);
        var selected = products.OrderBy(_ => rnd.Next()).Take(target).ToList();

        foreach (var product in selected)
        {
            if (existingSet.Contains(product.Id))
            {
                continue;
            }

            var pfb = Math.Round(product.AverageSalePrice * (decimal)(0.7 + rnd.NextDouble() * 0.2), 2);
            rules.Add(new ProductBillingRule
            {
                ProductId = product.Id,
                PriceTable = "CMED",
                Code = product.Sku.Replace(SkuPrefix, ""),
                PricePfb = pfb,
                Pmc = Math.Round(pfb * (decimal)(1.2 + rnd.NextDouble() * 0.3), 2),
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-rnd.Next(1, 12))),
                Edition = $"{DateTime.UtcNow:yyyy-MM}"
            });
        }

        if (rules.Count == 0)
        {
            return 0;
        }

        db.ProductBillingRules.AddRange(rules);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Simulação ops: {Count} regras CMED criadas.", rules.Count);
        return rules.Count;
    }

    private static async Task<(int Dispensings, int BillingEntries)> SeedPharmacyDispensingAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        IReadOnlyList<ProductSeedInfo> medicationProducts,
        IReadOnlyList<ProfessionalSeedInfo> professionals,
        Random rnd,
        string marker,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (markedPatientIds.Count == 0 || medicationProducts.Count == 0 || professionals.Count == 0)
        {
            return (0, 0);
        }

        var existingCount = await db.PharmacyDispensings
            .AsNoTracking()
            .CountAsync(d => d.Notes != null && d.Notes.StartsWith(marker), cancellationToken);

        var target = 80;
        var toCreate = Math.Max(0, target - existingCount);
        if (toCreate == 0)
        {
            return (0, 0);
        }

        var insurances = await db.HealthInsurances.AsNoTracking().ToListAsync(cancellationToken);
        var privateInsurance = insurances.FirstOrDefault(i =>
            !i.Name.Equals("SUS", StringComparison.OrdinalIgnoreCase)
            && !i.Name.Equals("Particular", StringComparison.OrdinalIgnoreCase))
            ?? insurances.FirstOrDefault(i => i.Name.Equals("Particular", StringComparison.OrdinalIgnoreCase));

        var dispensings = new List<PharmacyDispensing>();
        var billingEntries = new List<PharmacyBillingEntry>();
        var patients = markedPatientIds.OrderBy(_ => rnd.Next()).Take(Math.Min(toCreate, markedPatientIds.Count)).ToList();

        for (var i = 0; i < toCreate; i++)
        {
            var patientId = patients[i % patients.Count];
            var product = medicationProducts[rnd.Next(medicationProducts.Count)];
            var professional = professionals[rnd.Next(professionals.Count)];
            var qty = rnd.Next(1, 6);
            var unitPrice = product.AverageSalePrice;

            var dispensing = new PharmacyDispensing
            {
                PatientId = patientId,
                ProductId = product.Id,
                ProfessionalId = professional.Id,
                Quantity = qty,
                DispensedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 45)),
                Notes = marker
            };
            dispensings.Add(dispensing);

            billingEntries.Add(new PharmacyBillingEntry
            {
                Dispensing = dispensing,
                PayerType = rnd.NextDouble() < 0.65 ? PharmacyBillingPayerType.Private : PharmacyBillingPayerType.Insurance,
                HealthInsuranceId = rnd.NextDouble() < 0.65 ? null : privateInsurance?.Id,
                UnitPrice = unitPrice,
                TotalAmount = Math.Round(unitPrice * qty, 2),
                Paid = rnd.NextDouble() < 0.4,
                PaidAt = rnd.NextDouble() < 0.4 ? DateTime.UtcNow.AddDays(-rnd.Next(1, 20)) : null,
                Notes = marker
            });
        }

        db.PharmacyDispensings.AddRange(dispensings);
        await db.SaveChangesAsync(cancellationToken);
        db.PharmacyBillingEntries.AddRange(billingEntries);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Simulação ops: {Disp} dispensações, {Bill} lançamentos farmácia.",
            dispensings.Count,
            billingEntries.Count);

        return (dispensings.Count, billingEntries.Count);
    }

    private sealed record FinanceSeedStats(
        int AccountsCreated,
        int ProposalsCreated,
        int HonorariosCreated,
        int PayablesCreated,
        int PaymentsCreated,
        int LineItemsCreated,
        int MiscellaneousReceiptsCreated,
        int CashSessionsCreated,
        int PayrollRunsCreated,
        int TpaClaimsCreated,
        int TissGuidesCreated,
        int TissBatchesCreated);

    private static async Task<FinanceSeedStats> SeedComprehensiveFinanceAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        IReadOnlyList<ProfessionalSeedInfo> professionals,
        Random rnd,
        string marker,
        HospitalLoadDataOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var accountsCreated = 0;
        var proposalsCreated = 0;
        var honorariosCreated = 0;
        var payablesCreated = 0;
        var paymentsCreated = 0;
        var lineItemsCreated = 0;

        var existingProposals = await db.FinancialAccounts
            .AsNoTracking()
            .CountAsync(f => f.Notes != null && f.Notes.StartsWith(marker)
                && f.Description.Contains("Proposta —"), cancellationToken);
        var proposalGap = Math.Max(0, 80 - existingProposals);
        if (proposalGap > 0 && markedPatientIds.Count > 0)
        {
            var stats = await SeedProposalsAsync(db, markedPatientIds, rnd, marker, proposalGap, cancellationToken);
            accountsCreated += stats.Accounts;
            proposalsCreated += stats.Accounts;
            lineItemsCreated += stats.LineItems;
        }

        var existingHonorarios = await db.FinancialAccounts
            .AsNoTracking()
            .CountAsync(f => f.Notes != null && f.Notes.StartsWith(marker)
                && f.Description.Contains("Honorários"), cancellationToken);
        var honorGap = Math.Max(0, 60 - existingHonorarios);
        if (honorGap > 0 && markedPatientIds.Count > 0 && professionals.Count > 0)
        {
            honorariosCreated = await SeedHonorariosAsync(
                db, markedPatientIds, professionals, rnd, marker, honorGap, cancellationToken);
            accountsCreated += honorariosCreated;
        }

        var existingConsultationExtras = await db.FinancialAccounts
            .AsNoTracking()
            .CountAsync(f => f.Notes != null && f.Notes.StartsWith(marker)
                && f.Category == FinancialAccountCategory.Exam
                && f.LineItems.Any(), cancellationToken);
        var consultGap = Math.Max(0, 40 - existingConsultationExtras);
        if (consultGap > 0 && markedPatientIds.Count > 0)
        {
            var extra = await SeedConsultationExamAccountsAsync(
                db, markedPatientIds, rnd, marker, consultGap, cancellationToken);
            accountsCreated += extra.Accounts;
            lineItemsCreated += extra.LineItems;
            paymentsCreated += extra.Payments;
        }

        var existingPartial = await db.FinancialAccounts
            .AsNoTracking()
            .CountAsync(f => f.Notes != null && f.Notes.StartsWith(marker)
                && f.Status == FinancialAccountStatus.PartiallyPaid, cancellationToken);
        var partialGap = Math.Max(0, 30 - existingPartial);
        if (partialGap > 0 && markedPatientIds.Count > 0)
        {
            var partialStats = await SeedPartialPaymentAccountsAsync(
                db, markedPatientIds, rnd, marker, partialGap, cancellationToken);
            accountsCreated += partialStats.Accounts;
            paymentsCreated += partialStats.Payments;
        }

        var existingPayables = await db.FinancialAccounts
            .AsNoTracking()
            .CountAsync(f => f.Notes != null && f.Notes.StartsWith(marker)
                && f.Direction == FinancialAccountDirection.Payable, cancellationToken);
        var payableGap = Math.Max(0, 45 - existingPayables);
        if (payableGap > 0)
        {
            payablesCreated = await SeedExtendedPayablesAsync(db, rnd, marker, payableGap, cancellationToken);
            accountsCreated += payablesCreated;
        }

        var paymentVarietyStats = await SeedPaymentMethodVarietyAsync(db, markedPatientIds, rnd, marker, cancellationToken);
        accountsCreated += paymentVarietyStats.Accounts;
        paymentsCreated += paymentVarietyStats.Payments;

        var existingLineItems = await db.FinancialAccountLineItems
            .AsNoTracking()
            .CountAsync(i => i.Notes != null && i.Notes.StartsWith(marker), cancellationToken);
        var lineItemGap = Math.Max(0, 50 - existingLineItems);
        if (lineItemGap > 0)
        {
            lineItemsCreated += await SeedAdditionalLineItemsAsync(db, markedPatientIds, rnd, marker, lineItemGap, cancellationToken);
        }

        var miscCreated = await SeedMiscellaneousReceiptsAsync(db, rnd, marker, cancellationToken);
        var cashCreated = await SeedCashSessionsAsync(db, rnd, marker, options, cancellationToken);
        var payrollCreated = await SeedPayrollAsync(db, rnd, marker, cancellationToken);
        var tpaCreated = await SeedTpaClaimsAsync(db, markedPatientIds, rnd, marker, cancellationToken);
        var tissStats = await SeedSupplementalTissAsync(db, markedPatientIds, rnd, marker, cancellationToken);

        accountsCreated += payrollCreated.Accounts + tissStats.Accounts;

        logger.LogInformation(
            "Simulação ops financeiro: +{Accounts} contas, {Proposals} propostas, {Honor} honorários, {Pay} pagamentos.",
            accountsCreated,
            proposalsCreated,
            honorariosCreated,
            paymentsCreated);

        return new FinanceSeedStats(
            accountsCreated,
            proposalsCreated,
            honorariosCreated,
            payablesCreated,
            paymentsCreated,
            lineItemsCreated,
            miscCreated,
            cashCreated,
            payrollCreated.Runs,
            tpaCreated,
            tissStats.Guides,
            tissStats.Batches);
    }

    private static async Task<(int Accounts, int LineItems)> SeedProposalsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        int count,
        CancellationToken cancellationToken)
    {
        var accounts = new List<FinancialAccount>();
        var lineItems = new List<FinancialAccountLineItem>();
        var categories = new[]
        {
            FinancialAccountCategory.Consultation,
            FinancialAccountCategory.Exam,
            FinancialAccountCategory.Hospitalization,
            FinancialAccountCategory.Copayment
        };

        for (var i = 0; i < count; i++)
        {
            var patientId = markedPatientIds[rnd.Next(markedPatientIds.Count)];
            var label = ProposalBudgetLabels[rnd.Next(ProposalBudgetLabels.Length)];
            var amount = rnd.Next(300, 12001);

            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patientId,
                Category = categories[rnd.Next(categories.Length)],
                Description = $"Proposta — orçamento {label}",
                Amount = amount,
                Status = FinancialAccountStatus.Open,
                DueDate = DateTime.UtcNow.AddDays(rnd.Next(7, 90)),
                Notes = marker
            };
            accounts.Add(account);

            if (rnd.NextDouble() < 0.4)
            {
                var itemCount = rnd.Next(2, 5);
                var unitBase = decimal.Round((decimal)amount / itemCount, 2);
                for (var j = 0; j < itemCount; j++)
                {
                    lineItems.Add(new FinancialAccountLineItem
                    {
                        FinancialAccount = account,
                        Description = $"Item {j + 1} — {label}",
                        Quantity = 1,
                        UnitAmount = unitBase,
                        TotalAmount = unitBase,
                        Notes = marker
                    });
                }
            }
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
        return (accounts.Count, lineItems.Count);
    }

    private static async Task<int> SeedHonorariosAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        IReadOnlyList<ProfessionalSeedInfo> professionals,
        Random rnd,
        string marker,
        int count,
        CancellationToken cancellationToken)
    {
        var accounts = new List<FinancialAccount>();
        var payments = new List<FinancialPayment>();

        for (var i = 0; i < count; i++)
        {
            var patientId = markedPatientIds[rnd.Next(markedPatientIds.Count)];
            var professional = professionals[rnd.Next(professionals.Count)];
            var amount = rnd.Next(200, 5001);
            var isPaid = rnd.NextDouble() < 0.45;

            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patientId,
                Category = FinancialAccountCategory.Consultation,
                Description = $"Honorários médicos — Dr. {professional.FullName}",
                Amount = amount,
                Status = isPaid ? FinancialAccountStatus.Paid : FinancialAccountStatus.Open,
                PaidAmount = isPaid ? amount : 0,
                PaidAt = isPaid ? DateTime.UtcNow.AddDays(-rnd.Next(1, 30)) : null,
                DueDate = DateTime.UtcNow.AddDays(rnd.Next(-15, 45)),
                Notes = marker
            };
            accounts.Add(account);

            if (isPaid)
            {
                payments.Add(new FinancialPayment
                {
                    FinancialAccount = account,
                    Amount = amount,
                    Method = rnd.NextDouble() < 0.5 ? PaymentMethod.BankTransfer : PaymentMethod.Pix,
                    PaidAt = account.PaidAt!.Value,
                    Notes = marker
                });
            }
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
        if (payments.Count > 0)
        {
            db.FinancialPayments.AddRange(payments);
            await db.SaveChangesAsync(cancellationToken);
        }

        return accounts.Count;
    }

    private static async Task<(int Accounts, int LineItems, int Payments)> SeedConsultationExamAccountsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        int count,
        CancellationToken cancellationToken)
    {
        var accounts = new List<FinancialAccount>();
        var lineItems = new List<FinancialAccountLineItem>();
        var payments = new List<FinancialPayment>();
        var examLabels = new[] { "Hemograma completo", "TSH/T4 livre", "RX tórax", "ECG", "Ultrassom abdome" };

        for (var i = 0; i < count; i++)
        {
            var patientId = markedPatientIds[rnd.Next(markedPatientIds.Count)];
            var amount = rnd.Next(80, 801);
            var isPaid = rnd.NextDouble() < 0.5;

            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patientId,
                Category = FinancialAccountCategory.Exam,
                Description = "Consulta/exame ambulatorial — pacote laboratorial",
                Amount = amount,
                Status = isPaid ? FinancialAccountStatus.Paid : FinancialAccountStatus.Open,
                PaidAmount = isPaid ? amount : 0,
                PaidAt = isPaid ? DateTime.UtcNow.AddDays(-rnd.Next(1, 25)) : null,
                DueDate = DateTime.UtcNow.AddDays(-rnd.Next(0, 30)),
                Notes = marker
            };
            accounts.Add(account);

            var itemCount = rnd.Next(2, 4);
            var unitBase = decimal.Round((decimal)amount / itemCount, 2);
            for (var j = 0; j < itemCount; j++)
            {
                lineItems.Add(new FinancialAccountLineItem
                {
                    FinancialAccount = account,
                    Description = examLabels[j % examLabels.Length],
                    Quantity = 1,
                    UnitAmount = unitBase,
                    TotalAmount = unitBase,
                    Notes = marker
                });
            }

            if (isPaid)
            {
                payments.Add(new FinancialPayment
                {
                    FinancialAccount = account,
                    Amount = amount,
                    Method = PaymentMethod.Pix,
                    PaidAt = account.PaidAt!.Value,
                    Notes = marker
                });
            }
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
        if (payments.Count > 0)
        {
            db.FinancialPayments.AddRange(payments);
            await db.SaveChangesAsync(cancellationToken);
        }

        return (accounts.Count, lineItems.Count, payments.Count);
    }

    private static async Task<(int Accounts, int Payments)> SeedPartialPaymentAccountsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        int count,
        CancellationToken cancellationToken)
    {
        var accounts = new List<FinancialAccount>();
        var payments = new List<FinancialPayment>();
        var methods = new[] { PaymentMethod.Pix, PaymentMethod.Cash, PaymentMethod.DebitCard };

        for (var i = 0; i < count; i++)
        {
            var patientId = markedPatientIds[rnd.Next(markedPatientIds.Count)];
            var amount = rnd.Next(200, 3001);
            var paidAmount = Math.Round(amount * (decimal)(0.15 + rnd.NextDouble() * 0.55), 2);

            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patientId,
                Category = FinancialAccountCategory.Copayment,
                Description = "Coparticipação convênio — desconto parcial aplicado",
                Amount = amount,
                PaidAmount = paidAmount,
                Status = FinancialAccountStatus.PartiallyPaid,
                DueDate = DateTime.UtcNow.AddDays(rnd.Next(-10, 30)),
                Notes = $"{marker}|desconto-parcial"
            };
            accounts.Add(account);

            payments.Add(new FinancialPayment
            {
                FinancialAccount = account,
                Amount = paidAmount,
                Method = methods[rnd.Next(methods.Length)],
                PaidAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 20)),
                Notes = $"{marker}|desconto parcial"
            });
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
        db.FinancialPayments.AddRange(payments);
        await db.SaveChangesAsync(cancellationToken);
        return (accounts.Count, payments.Count);
    }

    private static async Task<int> SeedExtendedPayablesAsync(
        AppDbContext db,
        Random rnd,
        string marker,
        int count,
        CancellationToken cancellationToken)
    {
        var suppliers = await db.Suppliers.AsNoTracking().ToListAsync(cancellationToken);
        var templates = new (FinancialAccountCategory Cat, string Counterparty, string Desc)[]
        {
            (FinancialAccountCategory.Utilities, "Companhia de Energia", "Energia elétrica — unidade principal"),
            (FinancialAccountCategory.Utilities, "Sabesp", "Água e esgoto — unidade"),
            (FinancialAccountCategory.Payroll, "Folha de pagamento", "Folha pagamento — mês assistencial"),
            (FinancialAccountCategory.Payroll, "Folha de pagamento", "Folha pagamento — administrativo"),
            (FinancialAccountCategory.Maintenance, "Manutenção predial", "Manutenção HVAC e elétrica"),
            (FinancialAccountCategory.Maintenance, "Terceirizada limpeza", "Serviços de limpeza hospitalar"),
            (FinancialAccountCategory.SupplierPurchase, "Fornecedor materiais", "Pedido materiais médicos"),
            (FinancialAccountCategory.OtherExpense, "Aluguel unidade", "Aluguel unidade centro"),
            (FinancialAccountCategory.Taxes, "Receita Federal", "DARF impostos retidos"),
            (FinancialAccountCategory.Utilities, "Telefonia", "Telefonia e internet corporativa"),
        };

        var accounts = new List<FinancialAccount>();
        var payments = new List<FinancialPayment>();

        for (var i = 0; i < count; i++)
        {
            var template = templates[i % templates.Length];
            var supplier = suppliers.Count > 0 && template.Cat == FinancialAccountCategory.SupplierPurchase
                ? suppliers[rnd.Next(suppliers.Count)]
                : null;
            var amount = template.Cat switch
            {
                FinancialAccountCategory.Payroll => rnd.Next(80000, 250000),
                FinancialAccountCategory.Utilities => rnd.Next(800, 6000),
                FinancialAccountCategory.SupplierPurchase => rnd.Next(2000, 25000),
                _ => rnd.Next(500, 8000)
            };

            var roll = rnd.NextDouble();
            FinancialAccountStatus status;
            decimal paidAmount = 0;
            DateTime? paidAt = null;

            if (roll < 0.35)
            {
                status = FinancialAccountStatus.Open;
            }
            else if (roll < 0.55)
            {
                status = FinancialAccountStatus.PartiallyPaid;
                paidAmount = Math.Round(amount * (decimal)(0.25 + rnd.NextDouble() * 0.4), 2);
            }
            else
            {
                status = FinancialAccountStatus.Paid;
                paidAmount = amount;
                paidAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 20));
            }

            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Payable,
                SupplierId = supplier?.Id,
                CounterpartyName = supplier?.Name ?? template.Counterparty,
                Category = template.Cat,
                Description = template.Desc,
                Amount = amount,
                PaidAmount = paidAmount,
                Status = status,
                DueDate = DateTime.UtcNow.AddDays(rnd.Next(-10, 35)),
                PaidAt = paidAt,
                Notes = marker
            };
            accounts.Add(account);

            if (paidAmount > 0)
            {
                payments.Add(new FinancialPayment
                {
                    FinancialAccount = account,
                    Amount = paidAmount,
                    Method = template.Cat == FinancialAccountCategory.Payroll
                        ? PaymentMethod.BankTransfer
                        : PaymentMethod.Pix,
                    PaidAt = paidAt ?? DateTime.UtcNow.AddDays(-rnd.Next(1, 10)),
                    Notes = marker
                });
            }
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
        if (payments.Count > 0)
        {
            db.FinancialPayments.AddRange(payments);
            await db.SaveChangesAsync(cancellationToken);
        }

        return accounts.Count;
    }

    private static async Task<(int Accounts, int Payments)> SeedPaymentMethodVarietyAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        CancellationToken cancellationToken)
    {
        var alreadySeeded = await db.FinancialPayments
            .AsNoTracking()
            .AnyAsync(p => p.Notes != null && p.Notes.Contains("TEF cartão crédito"), cancellationToken);

        if (alreadySeeded || markedPatientIds.Count == 0)
        {
            return (0, 0);
        }

        var accounts = new List<FinancialAccount>();
        var payments = new List<FinancialPayment>();
        var patientPool = markedPatientIds.OrderBy(_ => rnd.Next()).Take(20).ToList();

        void AddPaidAccount(string description, string notes, PaymentMethod method, decimal amount, string? accountNotes = null)
        {
            var patientId = patientPool[rnd.Next(patientPool.Count)];
            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patientId,
                Category = FinancialAccountCategory.Consultation,
                Description = description,
                Amount = amount,
                PaidAmount = amount,
                Status = FinancialAccountStatus.Paid,
                PaidAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 15)),
                DueDate = DateTime.UtcNow.AddDays(-rnd.Next(1, 15)),
                ExpectedPaymentMethod = method,
                Notes = accountNotes ?? marker
            };
            accounts.Add(account);
            payments.Add(new FinancialPayment
            {
                FinancialAccount = account,
                Amount = amount,
                Method = method,
                PaidAt = account.PaidAt!.Value,
                Notes = notes
            });
        }

        for (var i = 0; i < 4; i++)
        {
            AddPaidAccount(
                $"Recebimento consulta — cartão crédito TEF #{i + 1}",
                $"{marker}|TEF cartão crédito",
                PaymentMethod.CreditCard,
                rnd.Next(150, 1201),
                $"{marker}|TEF cartão crédito");
        }

        for (var i = 0; i < 3; i++)
        {
            AddPaidAccount(
                $"Recebimento exame — cartão débito TEF #{i + 1}",
                $"{marker}|TEF débito",
                PaymentMethod.DebitCard,
                rnd.Next(80, 601),
                $"{marker}|TEF débito");
        }

        for (var i = 0; i < 5; i++)
        {
            AddPaidAccount(
                $"Repasse profissional — transferência #{i + 1}",
                marker,
                PaymentMethod.BankTransfer,
                rnd.Next(500, 5001));
        }

        for (var i = 0; i < 3; i++)
        {
            AddPaidAccount("Recebimento em dinheiro — ambulatório", marker, PaymentMethod.Cash, rnd.Next(50, 401));
        }

        for (var i = 0; i < 4; i++)
        {
            AddPaidAccount("Recebimento via Pix — telemedicina", marker, PaymentMethod.Pix, rnd.Next(100, 801));
        }

        for (var i = 0; i < 3; i++)
        {
            var chequeNo = rnd.Next(10000, 99999);
            AddPaidAccount(
                $"Recebimento cheque nº {chequeNo}",
                $"{marker}|cheque nº {chequeNo}",
                PaymentMethod.Cash,
                rnd.Next(300, 2501),
                $"{marker}|cheque nº {chequeNo}");
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
        db.FinancialPayments.AddRange(payments);
        await db.SaveChangesAsync(cancellationToken);
        return (accounts.Count, payments.Count);
    }

    private static async Task<int> SeedAdditionalLineItemsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        int count,
        CancellationToken cancellationToken)
    {
        var accounts = await db.FinancialAccounts
            .Where(f => f.Notes != null && f.Notes.StartsWith(marker)
                && f.PatientId != null
                && markedPatientIds.Contains(f.PatientId.Value)
                && !f.LineItems.Any())
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .ToListAsync(cancellationToken);

        var lineItems = new List<FinancialAccountLineItem>();
        foreach (var account in accounts)
        {
            var itemCount = rnd.Next(2, 4);
            var unitBase = Math.Round(account.Amount / itemCount, 2);
            for (var j = 0; j < itemCount; j++)
            {
                lineItems.Add(new FinancialAccountLineItem
                {
                    FinancialAccountId = account.Id,
                    Description = $"Detalhamento item {j + 1}",
                    Quantity = 1,
                    UnitAmount = unitBase,
                    TotalAmount = unitBase,
                    Notes = marker
                });
            }
        }

        if (lineItems.Count == 0)
        {
            return 0;
        }

        db.FinancialAccountLineItems.AddRange(lineItems);
        await db.SaveChangesAsync(cancellationToken);
        return lineItems.Count;
    }

    private static async Task<int> SeedMiscellaneousReceiptsAsync(
        AppDbContext db,
        Random rnd,
        string marker,
        CancellationToken cancellationToken)
    {
        var existing = await db.MiscellaneousReceipts
            .AsNoTracking()
            .CountAsync(r => r.ReceiptNumber.StartsWith(SkuPrefix), cancellationToken);
        var gap = Math.Max(0, 25 - existing);
        if (gap == 0)
        {
            return 0;
        }

        var methods = new[] { PaymentMethod.Cash, PaymentMethod.Pix, PaymentMethod.CreditCard, PaymentMethod.DebitCard };
        var descriptions = new[]
        {
            "Doação institucional",
            "Venda material reciclável",
            "Estacionamento visitantes",
            "Taxa evento corporativo",
            "Reembolso caução",
            "Venda lanche cantina",
            "Aluguel espaço comercial",
            "Curso capacitação externa"
        };

        var receipts = new List<MiscellaneousReceipt>();
        var seq = existing + 1;
        for (var i = 0; i < gap; i++)
        {
            receipts.Add(new MiscellaneousReceipt
            {
                ReceiptNumber = $"{SkuPrefix}REC-{seq:D5}",
                ReceiptDate = DateTime.UtcNow.AddDays(-rnd.Next(1, 60)),
                PayerName = $"Pagador simulação {seq}",
                ReceiverName = "GTH Hospital Teste",
                Amount = rnd.Next(50, 2501),
                Description = descriptions[rnd.Next(descriptions.Length)],
                PaymentMethod = methods[rnd.Next(methods.Length)],
                Reference = marker
            });
            seq++;
        }

        db.MiscellaneousReceipts.AddRange(receipts);
        await db.SaveChangesAsync(cancellationToken);
        return receipts.Count;
    }

    private static async Task<int> SeedCashSessionsAsync(
        AppDbContext db,
        Random rnd,
        string marker,
        HospitalLoadDataOptions options,
        CancellationToken cancellationToken)
    {
        var existing = await db.FinancialCashSessions
            .AsNoTracking()
            .CountAsync(s => s.Notes != null && s.Notes.StartsWith(marker), cancellationToken);

        if (existing > 0)
        {
            return 0;
        }

        var sessions = new List<FinancialCashSession>();
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var openingToday = rnd.Next(800, 2501);

        sessions.Add(new FinancialCashSession
        {
            Label = "Caixa recepção — hoje",
            OpenedAt = today.AddHours(8),
            OpeningBalance = openingToday,
            ExpectedBalance = openingToday,
            Status = FinancialCashSessionStatus.Open,
            Notes = marker
        });

        var simulationStart = today.AddDays(-options.SimulationDays);
        for (var d = 0; d < 20; d++)
        {
            var day = simulationStart.AddDays(d * (options.SimulationDays / 20.0));
            if (day >= today)
            {
                continue;
            }

            var opening = rnd.Next(800, 2501);
            var counterMovement = rnd.Next(45_000, 185_001);
            var cashReceived = decimal.Round(counterMovement * 0.18m, 2);
            var cashPaidOut = rnd.Next(0, 3) == 0 ? rnd.Next(200, 1501) : rnd.Next(50, 401);
            var expected = opening + cashReceived - cashPaidOut;
            var variance = rnd.Next(-80, 81);
            var closing = expected + variance;

            sessions.Add(new FinancialCashSession
            {
                Label = $"Caixa dia {day:dd/MM/yyyy}",
                OpenedAt = day.AddHours(7 + rnd.Next(0, 2)),
                ClosedAt = day.AddHours(17 + rnd.Next(0, 3)),
                OpeningBalance = opening,
                ClosingBalance = closing,
                ExpectedBalance = expected,
                Status = FinancialCashSessionStatus.Closed,
                Notes = $"{marker}|fechamento|mov:{counterMovement:F0}"
            });
        }

        db.FinancialCashSessions.AddRange(sessions);
        await db.SaveChangesAsync(cancellationToken);
        return sessions.Count;
    }

    private static async Task<(int Runs, int Accounts)> SeedPayrollAsync(
        AppDbContext db,
        Random rnd,
        string marker,
        CancellationToken cancellationToken)
    {
        var existingRuns = await db.PayrollRuns
            .AsNoTracking()
            .CountAsync(r => r.Notes != null && r.Notes.StartsWith(marker), cancellationToken);

        if (existingRuns >= 3)
        {
            return (0, 0);
        }

        var employees = await PayrollSeedHelper.GetActiveEmployeesAsync(db, cancellationToken);
        if (employees.Count == 0)
        {
            return (0, 0);
        }

        var runsCreated = 0;
        var accountsCreated = 0;
        var now = DateTime.UtcNow;

        for (var m = 1; m <= 3; m++)
        {
            var refDate = DateOnly.FromDateTime(now.AddMonths(-m));
            if (await db.PayrollRuns.AnyAsync(r =>
                    r.Notes != null && r.Notes.StartsWith(marker)
                    && r.Year == refDate.Year && r.Month == refDate.Month, cancellationToken))
            {
                continue;
            }

            var periodStart = new DateOnly(refDate.Year, refDate.Month, 1);
            var periodEnd = new DateOnly(refDate.Year, refDate.Month, DateTime.DaysInMonth(refDate.Year, refDate.Month));
            var shiftStats = await PayrollSeedHelper.GetShiftStatsAsync(
                db,
                employees.Select(e => e.Id).ToList(),
                periodStart,
                periodEnd,
                cancellationToken);

            var items = employees
                .Select(employee =>
                {
                    shiftStats.TryGetValue(employee.Id, out var shifts);
                    return PayrollSeedHelper.BuildItemForEmployee(employee, refDate.Year, refDate.Month, shifts, rnd);
                })
                .ToList();

            var isPaid = m > 1;
            var run = PayrollSeedHelper.BuildRun(
                refDate.Year,
                refDate.Month,
                items,
                isPaid ? PayrollRunStatus.Paid : PayrollRunStatus.Approved,
                marker,
                refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(25),
                refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(28),
                isPaid ? refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(30) : null);

            db.PayrollRuns.Add(run);
            await db.SaveChangesAsync(cancellationToken);
            runsCreated++;

            if (isPaid)
            {
                var payable = new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Payable,
                    CounterpartyName = "Folha de pagamento",
                    Category = FinancialAccountCategory.Payroll,
                    Description = $"Folha pagamento — {refDate:MM/yyyy}",
                    Amount = run.TotalNet,
                    PaidAmount = run.TotalNet,
                    Status = FinancialAccountStatus.Paid,
                    DueDate = refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(30),
                    PaidAt = run.PaidAt,
                    Notes = $"Conta consolidada da folha com {items.Count} colaborador(es). {marker}"
                };
                db.FinancialAccounts.Add(payable);
                await db.SaveChangesAsync(cancellationToken);
                run.ConsolidatedFinancialAccountId = payable.Id;
                accountsCreated++;

                db.FinancialPayments.Add(new FinancialPayment
                {
                    FinancialAccountId = payable.Id,
                    Amount = run.TotalNet,
                    Method = PaymentMethod.BankTransfer,
                    PaidAt = run.PaidAt!.Value,
                    Notes = $"{marker}|repasse folha"
                });
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return (runsCreated, accountsCreated);
    }

    private static async Task<int> SeedTpaClaimsAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        CancellationToken cancellationToken)
    {
        var existing = await db.TpaClaims
            .AsNoTracking()
            .CountAsync(c => c.Notes != null && c.Notes.StartsWith(marker), cancellationToken);
        var gap = Math.Max(0, 15 - existing);
        if (gap == 0 || markedPatientIds.Count == 0)
        {
            return 0;
        }

        var administrators = await db.TpaAdministrators.AsNoTracking().ToListAsync(cancellationToken);
        if (administrators.Count == 0)
        {
            return 0;
        }

        var insurances = await db.HealthInsurances.AsNoTracking().ToListAsync(cancellationToken);
        var privatePlans = insurances
            .Where(i => !i.Name.Equals("SUS", StringComparison.OrdinalIgnoreCase)
                && !i.Name.Equals("Particular", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statuses = new[] { TpaClaimStatus.Draft, TpaClaimStatus.Submitted, TpaClaimStatus.Approved, TpaClaimStatus.Paid, TpaClaimStatus.Denied };
        var claims = new List<TpaClaim>();

        for (var i = 0; i < gap; i++)
        {
            var gross = rnd.Next(500, 8001);
            var admin = administrators[rnd.Next(administrators.Count)];
            var commission = Math.Round(gross * admin.CommissionPercent / 100m, 2);
            var discount = Math.Round(gross * admin.DiscountPercent / 100m, 2);
            var net = gross - commission - discount;

            claims.Add(new TpaClaim
            {
                TpaAdministratorId = admin.Id,
                PatientId = markedPatientIds[rnd.Next(markedPatientIds.Count)],
                HealthInsuranceId = privatePlans.Count > 0 ? privatePlans[rnd.Next(privatePlans.Count)].Id : null,
                ServiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-rnd.Next(1, 90))),
                GrossAmount = gross,
                CommissionAmount = commission,
                DiscountAmount = discount,
                NetAmount = net,
                Status = statuses[rnd.Next(statuses.Length)],
                Notes = marker
            });
        }

        db.TpaClaims.AddRange(claims);
        await db.SaveChangesAsync(cancellationToken);
        return claims.Count;
    }

    private static async Task<(int Guides, int Batches, int Accounts)> SeedSupplementalTissAsync(
        AppDbContext db,
        IReadOnlyList<Guid> markedPatientIds,
        Random rnd,
        string marker,
        CancellationToken cancellationToken)
    {
        var existingGuides = await db.TissGuides
            .AsNoTracking()
            .CountAsync(g => g.Notes != null && g.Notes.StartsWith(marker), cancellationToken);

        var targetGuides = 20;
        var gap = Math.Max(0, targetGuides - existingGuides);
        if (gap == 0 || markedPatientIds.Count == 0)
        {
            return (0, 0, 0);
        }

        var appointments = await db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.Insurances).ThenInclude(i => i.HealthInsurance)
            .Where(a => markedPatientIds.Contains(a.PatientId)
                && a.Status == AppointmentStatus.Completed
                && a.Notes != null
                && a.Notes.StartsWith(marker))
            .ToListAsync(cancellationToken);

        var alreadyGuidedAppointmentIds = await db.TissGuides
            .AsNoTracking()
            .Where(g => g.AppointmentId != null && g.Notes != null && g.Notes.StartsWith(marker))
            .Select(g => g.AppointmentId!.Value)
            .ToListAsync(cancellationToken);
        var guidedSet = alreadyGuidedAppointmentIds.ToHashSet();

        var eligible = appointments
            .Where(a => !guidedSet.Contains(a.Id))
            .Where(a =>
            {
                var insurance = a.Patient.Insurances.FirstOrDefault(i => i.IsPrimary) ?? a.Patient.Insurances.FirstOrDefault();
                if (insurance?.HealthInsurance is null)
                {
                    return false;
                }

                var name = insurance.HealthInsurance.Name;
                return !name.Equals("SUS", StringComparison.OrdinalIgnoreCase)
                    && !name.Equals("Particular", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(_ => rnd.Next())
            .Take(gap)
            .ToList();

        if (eligible.Count == 0)
        {
            return (0, 0, 0);
        }

        var guideCount = await db.TissGuides.CountAsync(cancellationToken);
        var guides = new List<TissGuide>();
        var accountsLinked = 0;

        foreach (var appointment in eligible)
        {
            var insurance = appointment.Patient.Insurances.FirstOrDefault(i => i.IsPrimary)
                ?? appointment.Patient.Insurances.First();
            guideCount++;
            var amount = rnd.Next(200, 1500);

            var guide = new TissGuide
            {
                GuideNumber = $"GTH-OPS-TISS-{DateTime.UtcNow:yyyy}-{guideCount:D6}",
                PatientId = appointment.PatientId,
                HealthInsuranceId = insurance.HealthInsuranceId,
                AppointmentId = appointment.Id,
                GuideType = TissGuideType.Consultation,
                Status = rnd.NextDouble() < 0.5 ? TissGuideStatus.Sent : TissGuideStatus.Draft,
                TotalAmount = amount,
                SentAt = rnd.NextDouble() < 0.5 ? DateTime.UtcNow.AddDays(-rnd.Next(1, 25)) : null,
                BeneficiaryCardNumber = insurance.CardNumber,
                BeneficiaryPlanName = insurance.PlanName,
                RequestingProfessionalId = appointment.ProfessionalId,
                ExecutingProfessionalId = appointment.ProfessionalId,
                Notes = marker,
                ClientRequestId = $"{marker}-ops-guide-{appointment.Id:N}",
                Items =
                [
                    new TissGuideItem
                    {
                        TussCode = "10101012",
                        Description = "Consulta em consultório",
                        Quantity = 1,
                        UnitPrice = amount,
                        IsAudited = rnd.NextDouble() < 0.4
                    }
                ]
            };
            guides.Add(guide);

            var existingAccount = await db.FinancialAccounts
                .FirstOrDefaultAsync(f => f.AppointmentId == appointment.Id, cancellationToken);

            if (existingAccount is not null)
            {
                existingAccount.TissGuide = guide;
                existingAccount.HealthInsuranceId = insurance.HealthInsuranceId;
                existingAccount.Category = FinancialAccountCategory.InsuranceReceivable;
                existingAccount.Description = $"Faturamento TISS — guia {guide.GuideNumber}";
                existingAccount.Amount = amount;
                existingAccount.Notes = marker;
            }
            else
            {
                db.FinancialAccounts.Add(new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Receivable,
                    PatientId = appointment.PatientId,
                    AppointmentId = appointment.Id,
                    HealthInsuranceId = insurance.HealthInsuranceId,
                    Category = FinancialAccountCategory.InsuranceReceivable,
                    Description = $"Faturamento TISS — guia {guide.GuideNumber}",
                    Amount = amount,
                    Status = FinancialAccountStatus.Open,
                    DueDate = DateTime.UtcNow.AddDays(45),
                    Notes = marker,
                    TissGuide = guide
                });
            }

            accountsLinked++;
        }

        db.TissGuides.AddRange(guides);
        await db.SaveChangesAsync(cancellationToken);

        var batchesCreated = 0;
        foreach (var insuranceGroup in guides.Where(g => g.Status == TissGuideStatus.Sent).GroupBy(g => g.HealthInsuranceId))
        {
            var batchCount = await db.TissBatches.CountAsync(cancellationToken);
            var competence = DateTime.UtcNow.ToString("yyyy-MM");
            var groupGuides = insuranceGroup.ToList();
            var batch = new TissBatch
            {
                BatchNumber = $"GTH-LOAD-{competence.Replace("-", "")}-{(batchCount + 1):D4}",
                HealthInsuranceId = insuranceGroup.Key,
                Competence = competence,
                Status = TissBatchStatus.Generated,
                TotalAmount = groupGuides.Sum(g => g.TotalAmount),
                GuideCount = groupGuides.Count,
            };

            db.TissBatches.Add(batch);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var guide in groupGuides)
            {
                guide.TissBatchId = batch.Id;
            }

            await db.SaveChangesAsync(cancellationToken);
            batchesCreated++;
        }

        return (guides.Count, batchesCreated, accountsLinked);
    }
}
