using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class ClinicalCatalogSeed
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var specialties = await EnsureSpecialtiesAsync(db, cancellationToken);
        await SeedLabExamsAsync(db, specialties, cancellationToken);
        await SeedImagingAsync(db, specialties, cancellationToken);
        await SeedMedicationsAsync(db, specialties, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Specialty>> EnsureSpecialtiesAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        var names = new[]
        {
            ("Clínica Geral", "225125"),
            ("Cardiologia", "225120"),
            ("Pediatria", "225124"),
            ("Ortopedia e Traumatologia", "225270"),
            ("Ginecologia e Obstetrícia", "225250"),
            ("Neurologia", "225112"),
            ("Endocrinologia e Metabologia", "225155"),
            ("Dermatologia", "225135"),
            ("Alergia e Imunologia", "225103"),
            ("Anestesiologia", "225109"),
            ("Angiologia", "225110"),
            ("Cancerologia", "225115"),
            ("Cirurgia Cardiovascular", "225118"),
            ("Cirurgia de Cabeça e Pescoço", "225121"),
            ("Cirurgia do Aparelho Digestivo", "225122"),
            ("Cirurgia Geral", "225123"),
            ("Cirurgia Pediátrica", "225127"),
            ("Cirurgia Plástica", "225128"),
            ("Cirurgia Torácica", "225129"),
            ("Cirurgia Vascular", "225130"),
            ("Coloproctologia", "225133"),
            ("Gastroenterologia", "225136"),
            ("Geriatria", "225139"),
            ("Hematologia e Hemoterapia", "225140"),
            ("Infectologia", "225145"),
            ("Mastologia", "225148"),
            ("Medicina de Família e Comunidade", "225150"),
            ("Medicina Física e Reabilitação", "225154"),
            ("Medicina Intensiva", "225160"),
            ("Nefrologia", "225165"),
            ("Neurocirurgia", "225170"),
            ("Obstetrícia", "225175"),
            ("Oftalmologia", "225180"),
            ("Oncologia Clínica", "225185"),
            ("Otorrinolaringologia", "225195"),
            ("Patologia", "225203"),
            ("Pneumologia", "225210"),
            ("Psiquiatria", "225215"),
            ("Radiologia e Diagnóstico por Imagem", "225220"),
            ("Reumatologia", "225225"),
            ("Urologia", "225230"),
        };

        foreach (var (name, cbo) in names)
        {
            var specialty = await db.Specialties.FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
            if (specialty is null)
            {
                db.Specialties.Add(new Specialty { Name = name, CboCode = cbo });
            }
            else if (string.IsNullOrWhiteSpace(specialty.CboCode))
            {
                specialty.CboCode = cbo;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return await db.Specialties
            .ToDictionaryAsync(s => s.Name, cancellationToken);
    }

    private static async Task SeedLabExamsAsync(
        AppDbContext db, Dictionary<string, Specialty> specs, CancellationToken ct)
    {
        var exams = new (string Name, string Tuss, string Sample, string Ref, string Unit, string Cat, bool General, string[] Specs)[]
        {
            ("Hemograma completo", "40304361", "Sangue", "Variável", "-", "Hematologia", true, []),
            ("Glicemia em jejum", "40302040", "Sangue", "70-99", "mg/dL", "Bioquímica", true, []),
            ("Creatinina", "40301630", "Sangue", "0.7-1.3", "mg/dL", "Bioquímica", true, []),
            ("Ureia", "40302547", "Sangue", "10-50", "mg/dL", "Bioquímica", true, []),
            ("TSH", "40316521", "Sangue", "0.4-4.0", "mUI/L", "Hormônios", true, []),
            ("Urina tipo I", "40311210", "Urina", "Variável", "-", "Urinálise", true, []),
            ("PCR ultrassensível", "40308391", "Sangue", "<3", "mg/L", "Bioquímica", true, []),
            ("Sódio", "40302237", "Sangue", "136-145", "mEq/L", "Bioquímica", true, []),
            ("Potássio", "40302318", "Sangue", "3.5-5.1", "mEq/L", "Bioquímica", true, []),
            ("TGO (AST)", "40302580", "Sangue", "<40", "U/L", "Bioquímica", true, []),
            ("TGP (ALT)", "40302572", "Sangue", "<41", "U/L", "Bioquímica", true, []),
            ("Troponina I", "40307488", "Sangue", "<0.04", "ng/mL", "Marcadores cardíacos", false, ["Cardiologia"]),
            ("CK-MB", "40301444", "Sangue", "<5", "ng/mL", "Marcadores cardíacos", false, ["Cardiologia"]),
            ("BNP", "40307291", "Sangue", "<100", "pg/mL", "Marcadores cardíacos", false, ["Cardiologia"]),
            ("Perfil lipídico completo", "40302717", "Sangue", "Variável", "mg/dL", "Bioquímica", false, ["Cardiologia", "Endocrinologia e Metabologia", "Clínica Geral"]),
            ("Holter 24h (solicitação)", "40101037", "Exame", "-", "-", "Cardiologia", false, ["Cardiologia"]),
            ("Teste ergométrico (solicitação)", "40101045", "Exame", "-", "-", "Cardiologia", false, ["Cardiologia"]),
            ("HbA1c", "40302169", "Sangue", "<5.7", "%", "Bioquímica", false, ["Endocrinologia e Metabologia", "Clínica Geral"]),
            ("Insulina basal", "40316300", "Sangue", "2.6-24.9", "µUI/mL", "Hormônios", false, ["Endocrinologia e Metabologia"]),
            ("T4 livre", "40316490", "Sangue", "0.8-1.8", "ng/dL", "Hormônios", false, ["Endocrinologia e Metabologia"]),
            ("PTH", "40316297", "Sangue", "15-65", "pg/mL", "Hormônios", false, ["Endocrinologia e Metabologia"]),
            ("Hemograma pediátrico", "40304361", "Sangue", "Variável", "-", "Pediatria", false, ["Pediatria"]),
            ("Coprocultura", "40310213", "Fezes", "Negativo", "-", "Pediatria", false, ["Pediatria"]),
            ("Parasitológico de fezes", "40310124", "Fezes", "Negativo", "-", "Pediatria", false, ["Pediatria"]),
            ("Teste do pezinho ampliado", "40314181", "Sangue", "Variável", "-", "Pediatria", false, ["Pediatria"]),
            ("VHS", "40304310", "Sangue", "Variável", "mm/h", "Hematologia", false, ["Ortopedia e Traumatologia"]),
            ("Fator reumatoide", "40306881", "Sangue", "<14", "UI/mL", "Sorologia", false, ["Ortopedia e Traumatologia"]),
            ("Ácido úrico", "40301183", "Sangue", "3.4-7.0", "mg/dL", "Bioquímica", false, ["Ortopedia e Traumatologia"]),
            ("Beta-HCG quantitativo", "40305367", "Sangue", "Variável", "mUI/mL", "Hormônios", false, ["Ginecologia e Obstetrícia"]),
            ("Progesterona", "40316203", "Sangue", "Variável", "ng/mL", "Hormônios", false, ["Ginecologia e Obstetrícia"]),
            ("Papanicolau (solicitação)", "40601137", "Citologia", "-", "-", "Ginecologia e Obstetrícia", false, ["Ginecologia e Obstetrícia"]),
            ("EEG (solicitação)", "40105075", "Exame", "-", "-", "Neurologia", false, ["Neurologia"]),
            ("LCR — análise bioquímica", "40308170", "LCR", "Variável", "-", "Neurologia", false, ["Neurologia"]),
            ("Cultura de pele", "40310140", "Pele", "Negativo", "-", "Dermatologia", false, ["Dermatologia"]),
            ("IgE total", "40306970", "Sangue", "<100", "UI/mL", "Sorologia", false, ["Dermatologia", "Pediatria"]),
            ("Tipagem sanguínea ABO/Rh", "40304298", "Sangue", "-", "-", "Hematologia", true, []),
            ("Tempo de protrombina", "40304515", "Sangue", "11-13.5", "s", "Coagulação", true, []),
            ("TTPA", "40304612", "Sangue", "25-35", "s", "Coagulação", true, []),
            ("Ferritina", "40316246", "Sangue", "30-400", "ng/mL", "Bioquímica", false, ["Clínica Geral", "Ginecologia e Obstetrícia"]),
            ("Vitamina D", "40302806", "Sangue", "30-100", "ng/mL", "Bioquímica", false, ["Ortopedia e Traumatologia", "Endocrinologia e Metabologia"]),
            ("Reticulócitos", "40304345", "Sangue", "0.5-2.5", "%", "Hematologia", true, []),
            ("Coombs direto", "40304263", "Sangue", "Negativo", "-", "Hematologia", true, []),
            ("Fibrinogênio", "40304574", "Sangue", "200-400", "mg/dL", "Hematologia", true, []),
            ("D-dímero", "40308367", "Sangue", "<500", "ng/mL", "Hematologia", true, []),
            ("HIV 1/2", "40307198", "Sangue", "Não reagente", "-", "Sorologia", true, []),
            ("HBsAg", "40306935", "Sangue", "Não reagente", "-", "Sorologia", true, []),
            ("Anti-HCV", "40307029", "Sangue", "Não reagente", "-", "Sorologia", true, []),
            ("VDRL", "40307780", "Sangue", "Não reagente", "-", "Sorologia", true, []),
            ("Urocultura", "40310221", "Urina", "Negativo", "-", "Microbiologia", true, []),
            ("Hemocultura", "40310116", "Sangue", "Negativo", "-", "Microbiologia", true, []),
            ("Proteinúria 24h", "40311245", "Urina", "Variável", "mg/24h", "Urinálise", true, []),
            ("Microalbuminúria", "40311229", "Urina", "<30", "mg/L", "Urinálise", true, []),
            ("Pesquisa de sangue oculto nas fezes", "40310159", "Fezes", "Negativo", "-", "Parasitologia", true, []),
            ("Estradiol", "40316211", "Sangue", "Variável", "pg/mL", "Hormônios", false, ["Ginecologia e Obstetrícia"]),
            ("Testosterona total", "40316505", "Sangue", "Variável", "ng/dL", "Hormônios", false, ["Endocrinologia e Metabologia"]),
            ("Cortisol basal", "40316190", "Sangue", "5-25", "µg/dL", "Hormônios", false, ["Endocrinologia e Metabologia"]),
            ("ASLO", "40306830", "Sangue", "<200", "UI/mL", "Sorologia", false, ["Pediatria"]),
            ("Antibiograma", "40310183", "Cultura", "Sensível", "-", "Microbiologia", true, []),
        };

        foreach (var e in exams)
        {
            var specNames = e.Specs.Length == 0 && e.General ? Array.Empty<string>() : e.Specs;
            await UpsertLabExamAsync(db, specs, e.Name, e.Tuss, e.Sample, e.Ref, e.Unit, e.Cat, e.General, specNames, ct);
        }
    }

    private static async Task UpsertLabExamAsync(
        AppDbContext db,
        Dictionary<string, Specialty> specs,
        string name, string tuss, string sample, string range, string unit,
        string category, bool isGeneral, string[] specialtyNames,
        CancellationToken ct)
    {
        var exam = await db.LabExamCatalogs
            .Include(e => e.SpecialtyLinks)
            .FirstOrDefaultAsync(e => e.Name == name, ct);

        if (exam is null)
        {
            exam = new LabExamCatalog
            {
                Name = name,
                TussCode = tuss,
                SampleType = sample,
                ReferenceRange = range,
                Unit = unit,
                Category = category,
                IsGeneral = isGeneral,
            };
            db.LabExamCatalogs.Add(exam);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            exam.Category = category;
            exam.IsGeneral = isGeneral;
            exam.TussCode ??= tuss;
            exam.SampleType ??= sample;
            exam.ReferenceRange ??= range;
            exam.Unit ??= unit;
        }

        if (isGeneral) return;

        foreach (var specName in specialtyNames)
        {
            if (!specs.TryGetValue(specName, out var specialty)) continue;
            if (exam.SpecialtyLinks.Any(l => l.SpecialtyId == specialty.Id)) continue;
            exam.SpecialtyLinks.Add(new LabExamCatalogSpecialty
            {
                LabExamCatalogId = exam.Id,
                SpecialtyId = specialty.Id,
            });
        }
    }

    private static async Task SeedImagingAsync(
        AppDbContext db, Dictionary<string, Specialty> specs, CancellationToken ct)
    {
        var items = new (string Name, string Tuss, ImagingModality Mod, string Body, bool General, string[] Specs)[]
        {
            ("Radiografia de tórax PA e perfil", "40801063", ImagingModality.XRay, "Tórax", true, []),
            ("Radiografia de abdome", "40801012", ImagingModality.XRay, "Abdome", true, []),
            ("Radiografia de coluna lombar", "40801128", ImagingModality.XRay, "Coluna", false, ["Ortopedia e Traumatologia"]),
            ("Radiografia de crânio", "40801047", ImagingModality.XRay, "Crânio", true, []),
            ("Ultrassom abdominal total", "40901071", ImagingModality.Ultrasound, "Abdome", true, []),
            ("TC de crânio sem contraste", "41001010", ImagingModality.CT, "Crânio", false, ["Neurologia", "Clínica Geral"]),
            ("RM de crânio", "41101014", ImagingModality.MRI, "Crânio", false, ["Neurologia"]),
            ("Ecocardiograma transtorácico", "40901098", ImagingModality.Ultrasound, "Coração", false, ["Cardiologia"]),
            ("Teste de esforço com imagem", "40101045", ImagingModality.Ultrasound, "Coração", false, ["Cardiologia"]),
            ("Angiotomografia de coronárias", "41001150", ImagingModality.CT, "Coração", false, ["Cardiologia"]),
            ("RM de joelho", "41101111", ImagingModality.MRI, "Joelho", false, ["Ortopedia e Traumatologia"]),
            ("RM de coluna cervical", "41101103", ImagingModality.MRI, "Coluna", false, ["Ortopedia e Traumatologia", "Neurologia"]),
            ("Ultrassom obstétrico morfológico", "40901214", ImagingModality.Ultrasound, "Obstétrico", false, ["Ginecologia e Obstetrícia"]),
            ("Ultrassom transvaginal", "40901222", ImagingModality.Ultrasound, "Pelve", false, ["Ginecologia e Obstetrícia"]),
            ("Mamografia bilateral", "40801187", ImagingModality.Mammography, "Mamas", false, ["Ginecologia e Obstetrícia"]),
            ("Ultrassom pediátrico abdominal", "40901071", ImagingModality.Ultrasound, "Abdome", false, ["Pediatria"]),
            ("Radiografia de tórax pediátrica", "40801063", ImagingModality.XRay, "Tórax", false, ["Pediatria"]),
            ("TC de tórax", "41001052", ImagingModality.CT, "Tórax", false, ["Clínica Geral", "Cardiologia"]),
            ("Ultrassom de tireoide", "40901109", ImagingModality.Ultrasound, "Tireoide", false, ["Endocrinologia e Metabologia"]),
            ("Densitometria óssea", "40801216", ImagingModality.XRay, "Ossos", false, ["Endocrinologia e Metabologia", "Ortopedia e Traumatologia"]),
            ("Holter 24h (solicitação)", "40101037", ImagingModality.Ultrasound, "Coração", false, ["Cardiologia"]),
            ("MAPA 24h", "40101053", ImagingModality.Ultrasound, "Coração", false, ["Cardiologia"]),
            ("Ecocardiograma transesofágico", "40901117", ImagingModality.Ultrasound, "Coração", false, ["Cardiologia"]),
            ("TC de abdome total", "41001079", ImagingModality.CT, "Abdome", true, []),
            ("RM de mamas", "41101146", ImagingModality.MRI, "Mamas", false, ["Ginecologia e Obstetrícia"]),
            ("Ultrassom doppler de carótidas", "40901168", ImagingModality.Ultrasound, "Carótidas", false, ["Cardiologia", "Neurologia"]),
        };

        foreach (var i in items)
        {
            await UpsertImagingAsync(db, specs, i.Name, i.Tuss, i.Mod, i.Body, i.General, i.Specs, ct);
        }
    }

    private static async Task UpsertImagingAsync(
        AppDbContext db,
        Dictionary<string, Specialty> specs,
        string name, string tuss, ImagingModality mod, string body,
        bool isGeneral, string[] specialtyNames,
        CancellationToken ct)
    {
        var proc = await db.ImagingProcedureCatalogs
            .Include(e => e.SpecialtyLinks)
            .FirstOrDefaultAsync(e => e.Name == name, ct);

        if (proc is null)
        {
            proc = new ImagingProcedureCatalog
            {
                Name = name,
                TussCode = tuss,
                Modality = mod,
                BodyPart = body,
                IsGeneral = isGeneral,
            };
            db.ImagingProcedureCatalogs.Add(proc);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            proc.TussCode ??= tuss;
            proc.BodyPart ??= body;
            proc.IsGeneral = isGeneral;
        }

        if (isGeneral) return;

        foreach (var specName in specialtyNames)
        {
            if (!specs.TryGetValue(specName, out var specialty)) continue;
            if (proc.SpecialtyLinks.Any(l => l.SpecialtyId == specialty.Id)) continue;
            proc.SpecialtyLinks.Add(new ImagingProcedureSpecialty
            {
                ImagingProcedureCatalogId = proc.Id,
                SpecialtyId = specialty.Id,
            });
        }
    }

    private static async Task SeedMedicationsAsync(
        AppDbContext db, Dictionary<string, Specialty> specs, CancellationToken ct)
    {
        if (await db.MedicationCatalogs.AnyAsync(ct)) return;

        var products = await db.Products.Where(p => p.Type == ProductType.Medication).ToListAsync(ct);

        var meds = new (string Name, string Ingredient, string Form, string Strength, string Dose, string Route, bool General, string[] Specs)[]
        {
            ("Dipirona 500mg", "Dipirona sódica", "Comprimido", "500mg", "1 cp 6/6h se dor ou febre", "VO", true, []),
            ("Paracetamol 750mg", "Paracetamol", "Comprimido", "750mg", "1 cp 6/6h", "VO", true, []),
            ("Omeprazol 20mg", "Omeprazol", "Cápsula", "20mg", "1 cp em jejum", "VO", true, []),
            ("Losartana 50mg", "Losartana potássica", "Comprimido", "50mg", "1 cp/dia", "VO", false, ["Cardiologia", "Clínica Geral"]),
            ("Atenolol 50mg", "Atenolol", "Comprimido", "50mg", "1 cp/dia", "VO", false, ["Cardiologia"]),
            ("Sinvastatina 20mg", "Sinvastatina", "Comprimido", "20mg", "1 cp à noite", "VO", false, ["Cardiologia", "Endocrinologia"]),
            ("AAS 100mg", "Ácido acetilsalicílico", "Comprimido", "100mg", "1 cp/dia", "VO", false, ["Cardiologia"]),
            ("Furosemida 40mg", "Furosemida", "Comprimido", "40mg", "1 cp/dia", "VO", false, ["Cardiologia", "Clínica Geral"]),
            ("Metformina 850mg", "Cloridrato de metformina", "Comprimido", "850mg", "1 cp 12/12h", "VO", false, ["Endocrinologia"]),
            ("Levotiroxina 50mcg", "Levotiroxina sódica", "Comprimido", "50mcg", "1 cp em jejum", "VO", false, ["Endocrinologia"]),
            ("Insulina NPH", "Insulina humana NPH", "Solução injetável", "100UI/mL", "Conforme esquema", "SC", false, ["Endocrinologia"]),
            ("Amoxicilina 500mg", "Amoxicilina", "Cápsula", "500mg", "1 cp 8/8h 7 dias", "VO", false, ["Pediatria", "Clínica Geral"]),
            ("Ibuprofeno 100mg/mL", "Ibuprofeno", "Suspensão oral", "100mg/5mL", "Conforme peso", "VO", false, ["Pediatria"]),
            ("Prednisolona 3mg/mL", "Prednisolona", "Solução oral", "3mg/mL", "Conforme prescrição", "VO", false, ["Pediatria", "Dermatologia"]),
            ("Cetoprofeno 100mg", "Cetoprofeno", "Comprimido", "100mg", "1 cp 12/12h", "VO", false, ["Ortopedia"]),
            ("Tramadol 50mg", "Tramadol", "Cápsula", "50mg", "1 cp 8/8h se dor intensa", "VO", false, ["Ortopedia"]),
            ("Ácido tranexâmico 250mg", "Ácido tranexâmico", "Comprimido", "250mg", "Conforme prescrição", "VO", false, ["Ginecologia"]),
            ("Noretisterona 0.35mg", "Noretisterona", "Comprimido", "0.35mg", "1 cp/dia", "VO", false, ["Ginecologia"]),
            ("Carbamazepina 200mg", "Carbamazepina", "Comprimido", "200mg", "Conforme neurologista", "VO", false, ["Neurologia"]),
            ("Topiramato 50mg", "Topiramato", "Comprimido", "50mg", "Conforme neurologista", "VO", false, ["Neurologia"]),
            ("Hidrocortisona tópica 1%", "Hidrocortisona", "Creme", "1%", "Aplicar 2x/dia", "Tópica", false, ["Dermatologia"]),
            ("Loratadina 10mg", "Loratadina", "Comprimido", "10mg", "1 cp/dia", "VO", false, ["Dermatologia", "Clínica Geral"]),
            ("Soro fisiológico 0,9% 500mL", "Cloreto de sódio", "Solução", "500mL", "Conforme prescrição", "IV", true, []),
            ("Dexametasona 4mg", "Dexametasona", "Comprimido", "4mg", "Conforme prescrição", "VO", false, ["Clínica Geral", "Pediatria"]),
        };

        foreach (var m in meds)
        {
            var product = products.FirstOrDefault(p =>
                p.Name.Contains(m.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

            var med = new MedicationCatalog
            {
                Name = m.Name,
                ActiveIngredient = m.Ingredient,
                PharmaceuticalForm = m.Form,
                Strength = m.Strength,
                DefaultDosage = m.Dose,
                Route = m.Route,
                IsGeneral = m.General,
                ProductId = product?.Id,
            };

            if (!m.General)
            {
                foreach (var sn in m.Specs)
                {
                    if (specs.TryGetValue(sn, out var sp))
                    {
                        med.SpecialtyLinks.Add(new MedicationCatalogSpecialty { SpecialtyId = sp.Id });
                    }
                }
            }

            db.MedicationCatalogs.Add(med);
        }
    }
}
