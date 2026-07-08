using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>Catálogo mestre hospitalar de medicamentos (~300 itens) sem doses fixas de paciente.</summary>
public static class HospitalMedicationMasterSeed
{
    private const string DefaultDosage = "Conforme prescrição médica";

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var group in MedicationGroups)
        {
            foreach (var med in group.Medications)
            {
                await UpsertMedicationAsync(db, group.GroupName, med, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertMedicationAsync(
        AppDbContext db,
        string therapeuticClass,
        MedSeed med,
        CancellationToken ct)
    {
        var catalog = await db.MedicationCatalogs
            .FirstOrDefaultAsync(m => m.Name == med.Name, ct)
            ?? db.MedicationCatalogs.Local.FirstOrDefault(m => m.Name == med.Name);

        if (catalog is null)
        {
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.Type == ProductType.Medication && p.Name == med.Name, ct)
                ?? db.Products.Local.FirstOrDefault(p => p.Type == ProductType.Medication && p.Name == med.Name);

            if (product is null)
            {
                product = new Product
                {
                    Name = med.Name,
                    Sku = BuildSku(med.Name),
                    Type = ProductType.Medication,
                    Unit = med.Unit,
                    QuantityOnHand = 0,
                    MinimumStock = 10,
                    Category = "Medicamentos",
                    Manufacturer = "Genérico",
                    DefaultLocation = "Farmácia Central",
                    Presentation = med.Form,
                };
                db.Products.Add(product);
            }
            else
            {
                product.Category ??= "Medicamentos";
                product.Manufacturer ??= "Genérico";
                product.DefaultLocation ??= "Farmácia Central";
                product.Presentation ??= med.Form;
            }

            db.MedicationCatalogs.Add(new MedicationCatalog
            {
                Name = med.Name,
                ActiveIngredient = med.Ingredient,
                PharmaceuticalForm = med.Form,
                Strength = med.Strength,
                DefaultDosage = DefaultDosage,
                Route = med.Route,
                Notes = $"Classe terapêutica: {therapeuticClass}",
                IsGeneral = true,
                ProductId = product.Id,
            });
            return;
        }

        catalog.ActiveIngredient ??= med.Ingredient;
        catalog.PharmaceuticalForm ??= med.Form;
        catalog.Strength ??= med.Strength;
        catalog.Route ??= med.Route;
        catalog.DefaultDosage = DefaultDosage;
        catalog.Notes ??= $"Classe terapêutica: {therapeuticClass}";

        if (catalog.ProductId is null)
        {
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.Type == ProductType.Medication && p.Name == med.Name, ct)
                ?? db.Products.Local.FirstOrDefault(p => p.Type == ProductType.Medication && p.Name == med.Name);
            if (product is null)
            {
                product = new Product
                {
                    Name = med.Name,
                    Sku = BuildSku(med.Name),
                    Type = ProductType.Medication,
                    Unit = med.Unit,
                    QuantityOnHand = 0,
                    MinimumStock = 10,
                    Category = "Medicamentos",
                    Manufacturer = "Genérico",
                    DefaultLocation = "Farmácia Central",
                    Presentation = med.Form,
                };
                db.Products.Add(product);
            }
            else
            {
                product.Category ??= "Medicamentos";
                product.Manufacturer ??= "Genérico";
                product.DefaultLocation ??= "Farmácia Central";
                product.Presentation ??= med.Form;
            }

            catalog.ProductId = product.Id;
        }
    }

    private static string BuildSku(string name)
    {
        var prefix = new string(name
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(8)
            .ToArray());
        if (prefix.Length == 0)
        {
            prefix = "GEN";
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8];
        return $"MED-{prefix}-{hash}";
    }

    private readonly record struct MedSeed(
        string Name,
        string Ingredient,
        string Form,
        string Strength,
        string Route,
        string Unit = "UN");

    private readonly record struct MedGroup(string GroupName, MedSeed[] Medications);

    private static readonly MedGroup[] MedicationGroups =
    [
        Group("Analgésicos", [
            Med("Dipirona 500mg", "Dipirona sódica", "Comprimido", "500mg", "VO"),
            Med("Dipirona 1g", "Dipirona sódica", "Comprimido", "1g", "VO"),
            Med("Dipirona 500mg/mL", "Dipirona sódica", "Solução injetável", "500mg/mL", "IV/IM"),
            Med("Paracetamol 500mg", "Paracetamol", "Comprimido", "500mg", "VO"),
            Med("Paracetamol 750mg", "Paracetamol", "Comprimido", "750mg", "VO"),
            Med("Paracetamol 200mg/mL", "Paracetamol", "Solução oral", "200mg/mL", "VO"),
            Med("Paracetamol 10mg/mL", "Paracetamol", "Solução injetável", "10mg/mL", "IV"),
            Med("Tramadol 50mg", "Cloridrato de tramadol", "Cápsula", "50mg", "VO"),
            Med("Tramadol 100mg", "Cloridrato de tramadol", "Comprimido", "100mg", "VO"),
            Med("Tramadol 50mg/mL", "Cloridrato de tramadol", "Solução injetável", "50mg/mL", "IV/IM"),
            Med("Codeína 30mg", "Fosfato de codeína", "Comprimido", "30mg", "VO"),
            Med("Morfina 10mg/mL", "Sulfato de morfina", "Solução injetável", "10mg/mL", "IV/SC"),
            Med("Morfina 30mg", "Sulfato de morfina", "Comprimido de liberação prolongada", "30mg", "VO"),
            Med("Fentanil 50mcg/mL", "Citrato de fentanila", "Solução injetável", "50mcg/mL", "IV"),
            Med("Petidina 50mg/mL", "Cloridrato de petidina", "Solução injetável", "50mg/mL", "IM/IV"),
            Med("Nalbuphina 10mg/mL", "Cloridrato de nalbuphina", "Solução injetável", "10mg/mL", "IV/IM"),
            Med("Metadona 10mg", "Cloridrato de metadona", "Comprimido", "10mg", "VO"),
            Med("Naproxeno 500mg", "Naproxeno", "Comprimido", "500mg", "VO"),
            Med("Meloxicam 15mg", "Meloxicam", "Comprimido", "15mg", "VO"),
            Med("Cetorolaco 30mg/mL", "Trometamol cetorolaco", "Solução injetável", "30mg/mL", "IM/IV"),
        ]),
        Group("Antibióticos", [
            Med("Amoxicilina 500mg", "Amoxicilina", "Cápsula", "500mg", "VO"),
            Med("Amoxicilina 875mg + Clavulanato 125mg", "Amoxicilina + clavulanato", "Comprimido", "875/125mg", "VO"),
            Med("Ampicilina 500mg", "Ampicilina", "Cápsula", "500mg", "VO"),
            Med("Ampicilina 1g", "Ampicilina sódica", "Pó para injetável", "1g", "IV/IM"),
            Med("Azitromicina 500mg", "Azitromicina", "Comprimido", "500mg", "VO"),
            Med("Azitromicina 200mg/5mL", "Azitromicina", "Suspensão oral", "200mg/5mL", "VO"),
            Med("Cefalexina 500mg", "Cefalexina", "Cápsula", "500mg", "VO"),
            Med("Ceftriaxona 1g", "Ceftriaxona sódica", "Pó para injetável", "1g", "IV/IM"),
            Med("Cefepime 1g", "Cefepima", "Pó para injetável", "1g", "IV"),
            Med("Cefazolina 1g", "Cefazolina sódica", "Pó para injetável", "1g", "IV/IM"),
            Med("Cefuroxima 750mg", "Cefuroxima sódica", "Pó para injetável", "750mg", "IV/IM"),
            Med("Piperacilina 4g + Tazobactam 500mg", "Piperacilina + tazobactam", "Pó para injetável", "4,5g", "IV"),
            Med("Meropenem 1g", "Meropenem", "Pó para injetável", "1g", "IV"),
            Med("Imipenem 500mg + Cilastatina", "Imipenem + cilastatina", "Pó para injetável", "500mg", "IV"),
            Med("Vancomicina 500mg", "Cloridrato de vancomicina", "Pó para injetável", "500mg", "IV"),
            Med("Linezolida 600mg", "Linezolida", "Comprimido", "600mg", "VO"),
            Med("Linezolida 2mg/mL", "Linezolida", "Solução injetável", "2mg/mL", "IV"),
            Med("Clindamicina 300mg", "Cloridrato de clindamicina", "Cápsula", "300mg", "VO"),
            Med("Clindamicina 150mg/mL", "Cloridrato de clindamicina", "Solução injetável", "150mg/mL", "IV/IM"),
            Med("Metronidazol 250mg", "Metronidazol", "Comprimido", "250mg", "VO"),
            Med("Metronidazol 500mg/100mL", "Metronidazol", "Solução injetável", "500mg/100mL", "IV"),
            Med("Ciprofloxacino 500mg", "Cloridrato de ciprofloxacino", "Comprimido", "500mg", "VO"),
            Med("Ciprofloxacino 200mg/100mL", "Cloridrato de ciprofloxacino", "Solução injetável", "200mg/100mL", "IV"),
            Med("Levofloxacino 500mg", "Levofloxacino", "Comprimido", "500mg", "VO"),
            Med("Levofloxacino 5mg/mL", "Levofloxacino", "Solução injetável", "5mg/mL", "IV"),
            Med("Gentamicina 80mg/2mL", "Sulfato de gentamicina", "Solução injetável", "80mg/2mL", "IV/IM"),
            Med("Amicacina 500mg/2mL", "Sulfato de amicacina", "Solução injetável", "500mg/2mL", "IV/IM"),
            Med("Sulfametoxazol 400mg + Trimetoprima 80mg", "SMX + TMP", "Comprimido", "400/80mg", "VO"),
            Med("Doxiciclina 100mg", "Doxiciclina", "Comprimido", "100mg", "VO"),
            Med("Eritromicina 500mg", "Estearato de eritromicina", "Comprimido", "500mg", "VO"),
            Med("Fluconazol 150mg", "Fluconazol", "Cápsula", "150mg", "VO"),
            Med("Fluconazol 200mg/100mL", "Fluconazol", "Solução injetável", "200mg/100mL", "IV"),
            Med("Anfotericina B lipossomal", "Anfotericina B", "Pó liofilizado", "50mg", "IV"),
            Med("Aciclovir 200mg", "Aciclovir", "Comprimido", "200mg", "VO"),
            Med("Aciclovir 250mg", "Aciclovir", "Pó para injetável", "250mg", "IV"),
            Med("Oseltamivir 75mg", "Oseltamivir", "Cápsula", "75mg", "VO"),
            Med("Rifampicina 300mg", "Rifampicina", "Cápsula", "300mg", "VO"),
            Med("Isoniazida 300mg", "Isoniazida", "Comprimido", "300mg", "VO"),
            Med("Pirazinamida 500mg", "Pirazinamida", "Comprimido", "500mg", "VO"),
            Med("Etambutol 400mg", "Cloridrato de etambutol", "Comprimido", "400mg", "VO"),
            Med("Colistimetato 150mg", "Colistimetato de sódio", "Pó para injetável", "150mg", "IV"),
            Med("Tigeciclina 50mg", "Tigeciclina", "Pó liofilizado", "50mg", "IV"),
            Med("Daptomicina 500mg", "Daptomicina", "Pó liofilizado", "500mg", "IV"),
            Med("Teicoplanina 400mg", "Teicoplanina", "Pó liofilizado", "400mg", "IV/IM"),
        ]),
        Group("Anti-inflamatórios", [
            Med("Ibuprofeno 600mg", "Ibuprofeno", "Comprimido", "600mg", "VO"),
            Med("Ibuprofeno 100mg/5mL", "Ibuprofeno", "Suspensão oral", "100mg/5mL", "VO"),
            Med("Diclofenaco 50mg", "Diclofenaco sódico", "Comprimido", "50mg", "VO"),
            Med("Diclofenaco 75mg/3mL", "Diclofenaco sódico", "Solução injetável", "75mg/3mL", "IM"),
            Med("Cetoprofeno 100mg", "Cetoprofeno", "Comprimido", "100mg", "VO"),
            Med("Nimesulida 100mg", "Nimesulida", "Comprimido", "100mg", "VO"),
            Med("Piroxicam 20mg", "Piroxicam", "Cápsula", "20mg", "VO"),
            Med("Indometacina 25mg", "Indometacina", "Cápsula", "25mg", "VO"),
            Med("Naproxeno 250mg", "Naproxeno sódico", "Comprimido", "250mg", "VO"),
            Med("Celecoxibe 200mg", "Celecoxibe", "Cápsula", "200mg", "VO"),
            Med("Etoricoxibe 90mg", "Etoricoxibe", "Comprimido", "90mg", "VO"),
            Med("Ácido acetilsalicílico 100mg", "Ácido acetilsalicílico", "Comprimido", "100mg", "VO"),
            Med("Ácido acetilsalicílico 500mg", "Ácido acetilsalicílico", "Comprimido", "500mg", "VO"),
            Med("Tenoxicam 20mg", "Tenoxicam", "Comprimido", "20mg", "VO"),
            Med("Sulindaco 200mg", "Sulindaco", "Comprimido", "200mg", "VO"),
        ]),
        Group("Corticoides", [
            Med("Prednisona 5mg", "Prednisona", "Comprimido", "5mg", "VO"),
            Med("Prednisona 20mg", "Prednisona", "Comprimido", "20mg", "VO"),
            Med("Prednisolona 3mg/mL", "Prednisolona", "Solução oral", "3mg/mL", "VO"),
            Med("Dexametasona 4mg", "Dexametasona", "Comprimido", "4mg", "VO"),
            Med("Dexametasona 4mg/mL", "Dexametasona", "Solução injetável", "4mg/mL", "IV/IM"),
            Med("Hidrocortisona 100mg", "Succinato sódico de hidrocortisona", "Pó para injetável", "100mg", "IV/IM"),
            Med("Hidrocortisona 500mg", "Succinato sódico de hidrocortisona", "Pó para injetável", "500mg", "IV"),
            Med("Metilprednisolona 500mg", "Succinato sódico de metilprednisolona", "Pó para injetável", "500mg", "IV"),
            Med("Metilprednisolona 40mg", "Acetato de metilprednisolona", "Comprimido", "40mg", "VO"),
            Med("Betametasona 4mg/mL", "Betametasona", "Solução injetável", "4mg/mL", "IV/IM"),
            Med("Hidrocortisona tópica 1%", "Hidrocortisona", "Creme", "1%", "Tópica"),
            Med("Budesonida inalatória 200mcg", "Budesonida", "Cápsula inalatória", "200mcg", "Inalatória"),
            Med("Fluticasona spray 50mcg", "Propionato de fluticasona", "Spray nasal", "50mcg/dose", "Nasal"),
        ]),
        Group("Cardiologia", [
            Med("Losartana 50mg", "Losartana potássica", "Comprimido", "50mg", "VO"),
            Med("Losartana 100mg", "Losartana potássica", "Comprimido", "100mg", "VO"),
            Med("Enalapril 10mg", "Maleato de enalapril", "Comprimido", "10mg", "VO"),
            Med("Enalapril 20mg", "Maleato de enalapril", "Comprimido", "20mg", "VO"),
            Med("Captopril 25mg", "Captopril", "Comprimido", "25mg", "VO"),
            Med("Anlodipino 5mg", "Besilato de anlodipino", "Comprimido", "5mg", "VO"),
            Med("Anlodipino 10mg", "Besilato de anlodipino", "Comprimido", "10mg", "VO"),
            Med("Atenolol 50mg", "Atenolol", "Comprimido", "50mg", "VO"),
            Med("Metoprolol 50mg", "Succinato de metoprolol", "Comprimido", "50mg", "VO"),
            Med("Metoprolol 5mg/5mL", "Tartrato de metoprolol", "Solução injetável", "5mg/5mL", "IV"),
            Med("Carvedilol 25mg", "Carvedilol", "Comprimido", "25mg", "VO"),
            Med("Propranolol 40mg", "Cloridrato de propranolol", "Comprimido", "40mg", "VO"),
            Med("Hidroclorotiazida 25mg", "Hidroclorotiazida", "Comprimido", "25mg", "VO"),
            Med("Furosemida 40mg", "Furosemida", "Comprimido", "40mg", "VO"),
            Med("Furosemida 10mg/mL", "Furosemida", "Solução injetável", "10mg/mL", "IV/IM"),
            Med("Espironolactona 25mg", "Espironolactona", "Comprimido", "25mg", "VO"),
            Med("Sinvastatina 20mg", "Sinvastatina", "Comprimido", "20mg", "VO"),
            Med("Atorvastatina 20mg", "Atorvastatina cálcica", "Comprimido", "20mg", "VO"),
            Med("Atorvastatina 40mg", "Atorvastatina cálcica", "Comprimido", "40mg", "VO"),
            Med("Rosuvastatina 10mg", "Rosuvastatina cálcica", "Comprimido", "10mg", "VO"),
            Med("AAS 100mg", "Ácido acetilsalicílico", "Comprimido", "100mg", "VO"),
            Med("Clopidogrel 75mg", "Bissulfato de clopidogrel", "Comprimido", "75mg", "VO"),
            Med("Varfarina 5mg", "Varfarina sódica", "Comprimido", "5mg", "VO"),
            Med("Amiodarona 200mg", "Cloridrato de amiodarona", "Comprimido", "200mg", "VO"),
            Med("Amiodarona 150mg/3mL", "Cloridrato de amiodarona", "Solução injetável", "150mg/3mL", "IV"),
            Med("Digoxina 0,25mg", "Digoxina", "Comprimido", "0,25mg", "VO"),
            Med("Dobutamina 250mg/20mL", "Cloridrato de dobutamina", "Solução injetável", "250mg/20mL", "IV"),
            Med("Dopamina 50mg/10mL", "Cloridrato de dopamina", "Solução injetável", "50mg/10mL", "IV"),
            Med("Noradrenalina 4mg/4mL", "Bitartarato de noradrenalina", "Solução injetável", "4mg/4mL", "IV"),
            Med("Adrenalina 1mg/mL", "Epinefrina", "Solução injetável", "1mg/mL", "IV/IM/SC"),
            Med("Nitroglicerina 5mg/mL", "Nitroglicerina", "Solução injetável", "5mg/mL", "IV"),
            Med("Nitroprussiato 50mg", "Nitroprussiato de sódio", "Pó liofilizado", "50mg", "IV"),
            Med("Isosorbida 5mg", "Dinitrato de isossorbida", "Comprimido sublingual", "5mg", "SL"),
            Med("Ivabradina 5mg", "Cloridrato de ivabradina", "Comprimido", "5mg", "VO"),
            Med("Sacubitril + Valsartana 50mg", "Sacubitril + valsartana", "Comprimido", "50mg", "VO"),
        ]),
        Group("UTI", [
            Med("Midazolam 5mg/mL", "Midazolam", "Solução injetável", "5mg/mL", "IV/IM"),
            Med("Propofol 10mg/mL", "Propofol", "Emulsão injetável", "10mg/mL", "IV"),
            Med("Cetamina 50mg/mL", "Cloridrato de cetamina", "Solução injetável", "50mg/mL", "IV/IM"),
            Med("Rocurônio 10mg/mL", "Brometo de rocurônio", "Solução injetável", "10mg/mL", "IV"),
            Med("Succinilcolina 100mg", "Cloreto de succinilcolina", "Pó para injetável", "100mg", "IV"),
            Med("Atracúrio 10mg/mL", "Besilato de atracúrio", "Solução injetável", "10mg/mL", "IV"),
            Med("Cisatracúrio 2mg/mL", "Besilato de cisatracúrio", "Solução injetável", "2mg/mL", "IV"),
            Med("Fentanil 50mcg/mL", "Citrato de fentanila", "Solução injetável", "50mcg/mL", "IV"),
            Med("Remifentanil 1mg", "Cloridrato de remifentanila", "Pó liofilizado", "1mg", "IV"),
            Med("Naloxona 0,4mg/mL", "Cloridrato de naloxona", "Solução injetável", "0,4mg/mL", "IV/IM"),
            Med("Flumazenil 0,5mg/5mL", "Flumazenil", "Solução injetável", "0,5mg/5mL", "IV"),
            Med("Fenilefrina 10mg/mL", "Cloridrato de fenilefrina", "Solução injetável", "10mg/mL", "IV"),
            Med("Vasopressina 20UI/mL", "Acetato de vasopressina", "Solução injetável", "20UI/mL", "IV"),
            Med("Milrinona 1mg/mL", "Lactato de milrinona", "Solução injetável", "1mg/mL", "IV"),
            Med("Levosimendan 2,5mg/mL", "Levosimendan", "Solução injetável", "2,5mg/mL", "IV"),
            Med("Heparina 5000UI/mL", "Heparina sódica", "Solução injetável", "5000UI/mL", "IV/SC"),
            Med("Enoxaparina 40mg/0,4mL", "Enoxaparina sódica", "Solução injetável", "40mg/0,4mL", "SC"),
            Med("Enoxaparina 60mg/0,6mL", "Enoxaparina sódica", "Solução injetável", "60mg/0,6mL", "SC"),
            Med("Protamina 10mg/mL", "Sulfato de protamina", "Solução injetável", "10mg/mL", "IV"),
            Med("Atropina 0,25mg/mL", "Sulfato de atropina", "Solução injetável", "0,25mg/mL", "IV/IM"),
            Med("Bicarbonato de sódio 8,4%", "Bicarbonato de sódio", "Solução injetável", "8,4%", "IV"),
            Med("Cloreto de cálcio 10%", "Cloreto de cálcio", "Solução injetável", "10%", "IV"),
            Med("Gluconato de cálcio 10%", "Gluconato de cálcio", "Solução injetável", "10%", "IV"),
            Med("Sulfato de magnésio 10%", "Sulfato de magnésio", "Solução injetável", "10%", "IV/IM"),
            Med("Manitol 20%", "Manitol", "Solução injetável", "20%", "IV"),
        ]),
        Group("Sedação", [
            Med("Diazepam 10mg", "Diazepam", "Comprimido", "10mg", "VO"),
            Med("Diazepam 5mg/mL", "Diazepam", "Solução injetável", "5mg/mL", "IV/IM"),
            Med("Lorazepam 2mg", "Lorazepam", "Comprimido", "2mg", "VO"),
            Med("Lorazepam 2mg/mL", "Lorazepam", "Solução injetável", "2mg/mL", "IV/IM"),
            Med("Clonazepam 2mg", "Clonazepam", "Comprimido", "2mg", "VO"),
            Med("Clonazepam 2,5mg/mL", "Clonazepam", "Solução oral", "2,5mg/mL", "VO"),
            Med("Haloperidol 5mg", "Haloperidol", "Comprimido", "5mg", "VO"),
            Med("Haloperidol 5mg/mL", "Haloperidol", "Solução injetável", "5mg/mL", "IV/IM"),
            Med("Quetiapina 25mg", "Fumarato de quetiapina", "Comprimido", "25mg", "VO"),
            Med("Quetiapina 100mg", "Fumarato de quetiapina", "Comprimido", "100mg", "VO"),
            Med("Olanzapina 10mg", "Olanzapina", "Comprimido", "10mg", "VO"),
            Med("Risperidona 2mg", "Risperidona", "Comprimido", "2mg", "VO"),
            Med("Prometazina 25mg", "Cloridrato de prometazina", "Comprimido", "25mg", "VO"),
            Med("Prometazina 25mg/mL", "Cloridrato de prometazina", "Solução injetável", "25mg/mL", "IV/IM"),
            Med("Dexmedetomidina 100mcg/mL", "Cloridrato de dexmedetomidina", "Solução injetável", "100mcg/mL", "IV"),
        ]),
        Group("Insulinas", [
            Med("Insulina NPH 100UI/mL", "Insulina humana NPH", "Suspensão injetável", "100UI/mL", "SC"),
            Med("Insulina Regular 100UI/mL", "Insulina humana regular", "Solução injetável", "100UI/mL", "SC/IV"),
            Med("Insulina Glargina 100UI/mL", "Insulina glargina", "Solução injetável", "100UI/mL", "SC"),
            Med("Insulina Detemir 100UI/mL", "Insulina detemir", "Solução injetável", "100UI/mL", "SC"),
            Med("Insulina Aspart 100UI/mL", "Insulina aspart", "Solução injetável", "100UI/mL", "SC"),
            Med("Insulina Lispro 100UI/mL", "Insulina lispro", "Solução injetável", "100UI/mL", "SC"),
            Med("Insulina Degludeca 100UI/mL", "Insulina degludeca", "Solução injetável", "100UI/mL", "SC"),
            Med("Insulina 70/30 100UI/mL", "Insulina NPH + regular", "Suspensão injetável", "100UI/mL", "SC"),
            Med("Glucagon 1mg", "Glucagon", "Pó liofilizado", "1mg", "SC/IV/IM"),
            Med("Metformina 850mg", "Cloridrato de metformina", "Comprimido", "850mg", "VO"),
            Med("Metformina 500mg", "Cloridrato de metformina", "Comprimido", "500mg", "VO"),
            Med("Glicazida 30mg", "Cloridrato de glicazida", "Comprimido", "30mg", "VO"),
        ]),
        Group("Gastro", [
            Med("Omeprazol 20mg", "Omeprazol", "Cápsula", "20mg", "VO"),
            Med("Omeprazol 40mg", "Omeprazol", "Cápsula", "40mg", "VO"),
            Med("Omeprazol 40mg injetável", "Omeprazol sódico", "Pó para injetável", "40mg", "IV"),
            Med("Pantoprazol 40mg", "Pantoprazol sódico", "Comprimido", "40mg", "VO"),
            Med("Esomeprazol 40mg", "Esomeprazol magnésico", "Comprimido", "40mg", "VO"),
            Med("Ranitidina 150mg", "Cloridrato de ranitidina", "Comprimido", "150mg", "VO"),
            Med("Domperidona 10mg", "Domperidona", "Comprimido", "10mg", "VO"),
            Med("Metoclopramida 10mg", "Cloridrato de metoclopramida", "Comprimido", "10mg", "VO"),
            Med("Metoclopramida 5mg/mL", "Cloridrato de metoclopramida", "Solução injetável", "5mg/mL", "IV/IM"),
            Med("Ondansetrona 8mg", "Cloridrato de ondansetrona", "Comprimido", "8mg", "VO"),
            Med("Ondansetrona 4mg/2mL", "Cloridrato de ondansetrona", "Solução injetável", "4mg/2mL", "IV/IM"),
            Med("Bromoprida 10mg", "Bromoprida", "Comprimido", "10mg", "VO"),
            Med("Simeticona 40mg", "Simeticona", "Comprimido", "40mg", "VO"),
            Med("Lactulose 667mg/mL", "Lactulose", "Xarope", "667mg/mL", "VO"),
            Med("Polietilenoglicol 3350", "Macrogol 3350", "Pó oral", "sachê", "VO"),
            Med("Mesalazina 500mg", "Mesalazina", "Comprimido", "500mg", "VO"),
            Med("Sulfasalazina 500mg", "Sulfasalazina", "Comprimido", "500mg", "VO"),
            Med("Butilbrometo de escopolamina 10mg", "Butilbrometo de escopolamina", "Comprimido", "10mg", "VO"),
            Med("Hioscina 20mg/mL", "Butilbrometo de escopolamina", "Solução injetável", "20mg/mL", "IV/IM/SC"),
            Med("Sucralfato 1g", "Sucralfato", "Comprimido", "1g", "VO"),
            Med("Ursodiol 300mg", "Ácido ursodesoxicólico", "Comprimido", "300mg", "VO"),
            Med("Octreotida 0,1mg/mL", "Acetato de octreotida", "Solução injetável", "0,1mg/mL", "SC/IV"),
            Med("Pancreatina 25000UI", "Pancreatina", "Cápsula", "25000UI", "VO"),
            Med("Bisacodil 5mg", "Bisacodil", "Comprimido", "5mg", "VO"),
        ]),
        Group("Respiratório", [
            Med("Salbutamol spray 100mcg", "Sulfato de salbutamol", "Spray inalatório", "100mcg/dose", "Inalatória"),
            Med("Salbutamol 5mg/mL", "Sulfato de salbutamol", "Solução inalatória", "5mg/mL", "Inalatória"),
            Med("Fenoterol spray 100mcg", "Bromidrato de fenoterol", "Spray inalatório", "100mcg/dose", "Inalatória"),
            Med("Ipratrópio 20mcg", "Brometo de ipratrópio", "Spray inalatório", "20mcg/dose", "Inalatória"),
            Med("Brometo de ipratrópio 0,25mg/mL", "Brometo de ipratrópio", "Solução inalatória", "0,25mg/mL", "Inalatória"),
            Med("Formoterol 12mcg", "Fumarato de formoterol", "Cápsula inalatória", "12mcg", "Inalatória"),
            Med("Salmeterol 50mcg", "Xinafoato de salmeterol", "Cápsula inalatória", "50mcg", "Inalatória"),
            Med("Budesonida + Formoterol", "Budesonida + formoterol", "Cápsula inalatória", "400/12mcg", "Inalatória"),
            Med("Montelucaste 10mg", "Montelucaste de sódio", "Comprimido", "10mg", "VO"),
            Med("Teofilina 200mg", "Teofilina", "Comprimido", "200mg", "VO"),
            Med("Aminofilina 240mg", "Aminofilina", "Comprimido", "240mg", "VO"),
            Med("Prednisolona inalatória", "Prednisolona", "Solução inalatória", "variável", "Inalatória"),
            Med("Acetilcisteína 600mg", "Acetilcisteína", "Comprimido efervescente", "600mg", "VO"),
            Med("Acetilcisteína 300mg/3mL", "Acetilcisteína", "Solução inalatória", "300mg/3mL", "Inalatória"),
            Med("Ambroxol 30mg", "Cloridrato de ambroxol", "Comprimido", "30mg", "VO"),
            Med("Codeína + Guaifenesina", "Codeína + guaifenesina", "Xarope", "variável", "VO"),
            Med("Dextrometorfano 15mg", "Bromidrato de dextrometorfano", "Comprimido", "15mg", "VO"),
            Med("Oxigenoterapia", "Oxigênio medicinal", "Gás medicinal", "variável", "Inalatória"),
            Med("Heliox", "Hélio + oxigênio", "Gás medicinal", "variável", "Inalatória"),
            Med("Surfactante pulmonar", "Fosfolipídio pulmonar", "Suspensão", "variável", "Intratraqueal"),
        ]),
        Group("Anticoagulantes", [
            Med("Varfarina 5mg", "Varfarina sódica", "Comprimido", "5mg", "VO"),
            Med("Heparina 5000UI/mL", "Heparina sódica", "Solução injetável", "5000UI/mL", "IV/SC"),
            Med("Enoxaparina 40mg/0,4mL", "Enoxaparina sódica", "Solução injetável", "40mg/0,4mL", "SC"),
            Med("Enoxaparina 80mg/0,8mL", "Enoxaparina sódica", "Solução injetável", "80mg/0,8mL", "SC"),
            Med("Rivaroxabana 20mg", "Rivaroxabana", "Comprimido", "20mg", "VO"),
            Med("Rivaroxabana 15mg", "Rivaroxabana", "Comprimido", "15mg", "VO"),
            Med("Apixabana 5mg", "Apixabana", "Comprimido", "5mg", "VO"),
            Med("Dabigatrana 150mg", "Etexilato de dabigatrana", "Cápsula", "150mg", "VO"),
            Med("Fondaparinux 2,5mg/0,5mL", "Fondaparinux sódico", "Solução injetável", "2,5mg/0,5mL", "SC"),
            Med("Protamina 10mg/mL", "Sulfato de protamina", "Solução injetável", "10mg/mL", "IV"),
            Med("Ácido tranexâmico 250mg", "Ácido tranexâmico", "Comprimido", "250mg", "VO"),
            Med("Ácido tranexâmico 100mg/mL", "Ácido tranexâmico", "Solução injetável", "100mg/mL", "IV"),
            Med("Clopidogrel 75mg", "Bissulfato de clopidogrel", "Comprimido", "75mg", "VO"),
            Med("Ticagrelor 90mg", "Ticagrelor", "Comprimido", "90mg", "VO"),
            Med("Prasugrel 10mg", "Prasugrel", "Comprimido", "10mg", "VO"),
        ]),
        Group("Emergência", [
            Med("Adrenalina 1mg/mL", "Epinefrina", "Solução injetável", "1mg/mL", "IV/IM/SC"),
            Med("Atropina 0,25mg/mL", "Sulfato de atropina", "Solução injetável", "0,25mg/mL", "IV/IM"),
            Med("Amiodarona 150mg/3mL", "Cloridrato de amiodarona", "Solução injetável", "150mg/3mL", "IV"),
            Med("Lidocaína 2% 20mg/mL", "Cloridrato de lidocaína", "Solução injetável", "20mg/mL", "IV"),
            Med("Bicarbonato de sódio 8,4%", "Bicarbonato de sódio", "Solução injetável", "8,4%", "IV"),
            Med("Glicose 50% 10g/20mL", "Glicose", "Solução injetável", "50%", "IV"),
            Med("Glicose 10% 500mL", "Glicose", "Solução", "10%", "IV"),
            Med("Glucagon 1mg", "Glucagon", "Pó liofilizado", "1mg", "SC/IV/IM"),
            Med("Diazepam 5mg/mL", "Diazepam", "Solução injetável", "5mg/mL", "IV/IM"),
            Med("Fenitoína 250mg/5mL", "Fenitoína sódica", "Solução injetável", "250mg/5mL", "IV"),
            Med("Fenobarbital 100mg/mL", "Fenobarbital sódico", "Solução injetável", "100mg/mL", "IV/IM"),
            Med("Hidrocortisona 500mg", "Succinato sódico de hidrocortisona", "Pó para injetável", "500mg", "IV"),
            Med("Difenidramina 50mg/mL", "Cloridrato de difenidramina", "Solução injetável", "50mg/mL", "IV/IM"),
            Med("Prometazina 25mg/mL", "Cloridrato de prometazina", "Solução injetável", "25mg/mL", "IV/IM"),
            Med("Naloxona 0,4mg/mL", "Cloridrato de naloxona", "Solução injetável", "0,4mg/mL", "IV/IM"),
            Med("Flumazenil 0,5mg/5mL", "Flumazenil", "Solução injetável", "0,5mg/5mL", "IV"),
            Med("Carvão ativado", "Carvão ativado", "Pó oral", "sachê", "VO"),
            Med("Sulfato de magnésio 10%", "Sulfato de magnésio", "Solução injetável", "10%", "IV/IM"),
            Med("Cloreto de cálcio 10%", "Cloreto de cálcio", "Solução injetável", "10%", "IV"),
            Med("Manitol 20%", "Manitol", "Solução injetável", "20%", "IV"),
        ]),
        Group("Soluções", [
            Med("Soro fisiológico 0,9% 500mL", "Cloreto de sódio", "Solução", "500mL", "IV", "FR"),
            Med("Soro fisiológico 0,9% 100mL", "Cloreto de sódio", "Solução", "100mL", "IV", "FR"),
            Med("Soro fisiológico 0,9% 1000mL", "Cloreto de sódio", "Solução", "1000mL", "IV", "FR"),
            Med("Ringer lactato 500mL", "Ringer lactato", "Solução", "500mL", "IV", "FR"),
            Med("Ringer lactato 1000mL", "Ringer lactato", "Solução", "1000mL", "IV", "FR"),
            Med("Glicose 5% 500mL", "Glicose", "Solução", "5%", "IV", "FR"),
            Med("Glicose 5% 1000mL", "Glicose", "Solução", "5%", "IV", "FR"),
            Med("Glicose 10% 500mL", "Glicose", "Solução", "10%", "IV", "FR"),
            Med("Glicose 50% 10g/20mL", "Glicose", "Solução injetável", "50%", "IV", "AMP"),
            Med("Solução de manutenção pediátrica", "Eletrólitos + glicose", "Solução", "variável", "IV", "FR"),
            Med("Água para injeção 10mL", "Água para injeção", "Solução", "10mL", "IV/IM", "AMP"),
            Med("Água para injeção 20mL", "Água para injeção", "Solução", "20mL", "IV/IM", "AMP"),
            Med("Albumina humana 20% 50mL", "Albumina humana", "Solução", "20%", "IV", "FR"),
            Med("Plasma fresco congelado", "Plasma", "Hemoderivado", "unidade", "IV", "UN"),
            Med("Concentrado de hemácias", "Hemácias", "Hemoderivado", "unidade", "IV", "UN"),
        ]),
        Group("Psiquiatria", [
            Med("Sertralina 50mg", "Cloridrato de sertralina", "Comprimido", "50mg", "VO"),
            Med("Sertralina 100mg", "Cloridrato de sertralina", "Comprimido", "100mg", "VO"),
            Med("Fluoxetina 20mg", "Cloridrato de fluoxetina", "Cápsula", "20mg", "VO"),
            Med("Escitalopram 10mg", "Oxalato de escitalopram", "Comprimido", "10mg", "VO"),
            Med("Escitalopram 20mg", "Oxalato de escitalopram", "Comprimido", "20mg", "VO"),
            Med("Venlafaxina 75mg", "Cloridrato de venlafaxina", "Cápsula", "75mg", "VO"),
            Med("Amitriptilina 25mg", "Cloridrato de amitriptilina", "Comprimido", "25mg", "VO"),
            Med("Nortriptilina 25mg", "Cloridrato de nortriptilina", "Cápsula", "25mg", "VO"),
            Med("Bupropiona 150mg", "Cloridrato de bupropiona", "Comprimido", "150mg", "VO"),
            Med("Mirtazapina 30mg", "Mirtazapina", "Comprimido", "30mg", "VO"),
            Med("Trazodona 100mg", "Cloridrato de trazodona", "Comprimido", "100mg", "VO"),
            Med("Lítio 300mg", "Carbonato de lítio", "Comprimido", "300mg", "VO"),
            Med("Valproato 250mg", "Ácido valproico", "Comprimido", "250mg", "VO"),
            Med("Valproato 500mg", "Ácido valproico", "Comprimido", "500mg", "VO"),
            Med("Carbamazepina 200mg", "Carbamazepina", "Comprimido", "200mg", "VO"),
            Med("Carbamazepina 400mg", "Carbamazepina", "Comprimido", "400mg", "VO"),
            Med("Lamotrigina 100mg", "Lamotrigina", "Comprimido", "100mg", "VO"),
            Med("Risperidona 2mg", "Risperidona", "Comprimido", "2mg", "VO"),
            Med("Olanzapina 10mg", "Olanzapina", "Comprimido", "10mg", "VO"),
            Med("Quetiapina 25mg", "Fumarato de quetiapina", "Comprimido", "25mg", "VO"),
            Med("Quetiapina 100mg", "Fumarato de quetiapina", "Comprimido", "100mg", "VO"),
            Med("Haloperidol 5mg", "Haloperidol", "Comprimido", "5mg", "VO"),
            Med("Clozapina 100mg", "Clozapina", "Comprimido", "100mg", "VO"),
            Med("Aripiprazol 10mg", "Aripiprazol", "Comprimido", "10mg", "VO"),
            Med("Clonazepam 2mg", "Clonazepam", "Comprimido", "2mg", "VO"),
        ]),
        Group("Neurologia", [
            Med("Levetiracetam 500mg", "Levetiracetam", "Comprimido", "500mg", "VO"),
            Med("Levetiracetam 100mg/mL", "Levetiracetam", "Solução injetável", "100mg/mL", "IV"),
            Med("Fenitoína 100mg", "Fenitoína", "Comprimido", "100mg", "VO"),
            Med("Fenitoína 250mg/5mL", "Fenitoína sódica", "Solução injetável", "250mg/5mL", "IV"),
            Med("Carbamazepina 200mg", "Carbamazepina", "Comprimido", "200mg", "VO"),
            Med("Valproato 500mg", "Ácido valproico", "Comprimido", "500mg", "VO"),
            Med("Topiramato 50mg", "Topiramato", "Comprimido", "50mg", "VO"),
            Med("Topiramato 100mg", "Topiramato", "Comprimido", "100mg", "VO"),
            Med("Lamotrigina 100mg", "Lamotrigina", "Comprimido", "100mg", "VO"),
            Med("Gabapentina 300mg", "Gabapentina", "Cápsula", "300mg", "VO"),
            Med("Pregabalina 75mg", "Pregabalina", "Cápsula", "75mg", "VO"),
            Med("Baclofeno 10mg", "Baclofeno", "Comprimido", "10mg", "VO"),
            Med("Donepezila 10mg", "Cloridrato de donepezila", "Comprimido", "10mg", "VO"),
            Med("Memantina 10mg", "Cloridrato de memantina", "Comprimido", "10mg", "VO"),
            Med("Rivastigmina 3mg", "Rivastigmina", "Adesivo transdérmico", "9mg/24h", "TD"),
            Med("Sumatriptano 50mg", "Succinato de sumatriptano", "Comprimido", "50mg", "VO"),
            Med("Flunarizina 10mg", "Dicloridrato de flunarizina", "Comprimido", "10mg", "VO"),
            Med("Manitol 20%", "Manitol", "Solução injetável", "20%", "IV"),
            Med("Piracetam 800mg", "Piracetam", "Comprimido", "800mg", "VO"),
            Med("Metilfenidato 10mg", "Cloridrato de metilfenidato", "Comprimido", "10mg", "VO"),
        ]),
        Group("Oncológicos", [
            Med("Cisplatina 50mg", "Cisplatina", "Pó liofilizado", "50mg", "IV"),
            Med("Carboplatina 450mg", "Carboplatina", "Pó liofilizado", "450mg", "IV"),
            Med("Oxaliplatina 100mg", "Oxaliplatina", "Pó liofilizado", "100mg", "IV"),
            Med("Doxorrubicina 50mg", "Cloridrato de doxorrubicina", "Pó liofilizado", "50mg", "IV"),
            Med("Ciclofosfamida 1g", "Ciclofosfamida", "Pó liofilizado", "1g", "IV"),
            Med("Paclitaxel 100mg", "Paclitaxel", "Solução injetável", "100mg", "IV"),
            Med("Docetaxel 80mg", "Docetaxel", "Solução injetável", "80mg", "IV"),
            Med("Metotrexato 50mg/2mL", "Metotrexato", "Solução injetável", "50mg/2mL", "IV/IM"),
            Med("Fluoruracila 500mg/10mL", "Fluoruracila", "Solução injetável", "500mg/10mL", "IV"),
            Med("Gemcitabina 1g", "Cloridrato de gemcitabina", "Pó liofilizado", "1g", "IV"),
            Med("Vincristina 1mg/mL", "Sulfato de vincristina", "Solução injetável", "1mg/mL", "IV"),
            Med("Bleomicina 15UI", "Sulfato de bleomicina", "Pó liofilizado", "15UI", "IV/IM/SC"),
            Med("Imatinibe 400mg", "Mesilato de imatinibe", "Comprimido", "400mg", "VO"),
            Med("Tamoxifeno 20mg", "Citrato de tamoxifeno", "Comprimido", "20mg", "VO"),
            Med("Letrozol 2,5mg", "Letrozol", "Comprimido", "2,5mg", "VO"),
            Med("Filgrastim 300mcg/mL", "Filgrastim", "Solução injetável", "300mcg/mL", "SC"),
            Med("Pegfilgrastim 6mg", "Pegfilgrastim", "Solução injetável", "6mg", "SC"),
            Med("Ondansetrona 8mg", "Cloridrato de ondansetrona", "Comprimido", "8mg", "VO"),
            Med("Aprepitanto 125mg", "Aprepitanto", "Cápsula", "125mg", "VO"),
            Med("Dexametasona 4mg", "Dexametasona", "Comprimido", "4mg", "VO"),
            Med("Mesna 400mg", "Mesna", "Comprimido", "400mg", "VO"),
            Med("Leucovorina 50mg", "Leucovorina cálcica", "Pó liofilizado", "50mg", "IV/IM"),
            Med("Bortezomibe 3,5mg", "Bortezomibe", "Pó liofilizado", "3,5mg", "IV/SC"),
            Med("Rituximabe 500mg", "Rituximabe", "Solução injetável", "500mg", "IV"),
        ]),
    ];

    private static MedGroup Group(string name, MedSeed[] meds) => new(name, meds);

    private static MedSeed Med(
        string name,
        string ingredient,
        string form,
        string strength,
        string route,
        string unit = "UN")
        => new(name, ingredient, form, strength, route, unit);
}
