namespace SistemaHospitalar.Infrastructure.Persistence;

internal static partial class HospitalErpCatalogSeedData
{
    internal static readonly (string Group, string[] Items)[] LabExams =
    [
        ("Hematologia", [
            "Hemograma completo", "Hemograma com plaquetas", "Hematócrito", "Hemoglobina",
            "VCM", "HCM", "CHCM", "RDW", "Reticulócitos", "Contagem de plaquetas",
            "Esfregaço de sangue periférico", "Tipagem sanguínea ABO/Rh", "Prova do laço",
            "Tempo de sangramento", "Eletroforese de hemoglobina", "VHS", "Hemossedimentação",
            "Contagem de eosinófilos", "Contagem de reticulócitos", "Mielograma",
            "Tempo de protrombina (TP/INR)", "TTPA", "Fibrinogênio", "D-dímero",
            "Anticoagulante lúpico", "Proteína C", "Proteína S", "Fator V Leiden",
        ]),
        ("Bioquímica", [
            "Glicemia em jejum", "Glicemia pós-prandial", "HbA1c", "Glicemia capilar",
            "Ureia", "Creatinina", "Clearance de creatinina", "Ácido úrico",
            "Colesterol total", "HDL colesterol", "LDL colesterol", "VLDL colesterol",
            "Triglicerídeos", "TGO (AST)", "TGP (ALT)", "GGT", "Fosfatase alcalina",
            "Bilirrubina total", "Bilirrubina direta", "Bilirrubina indireta",
            "Proteínas totais", "Albumina", "Amilase", "Lipase", "LDH", "CK total",
            "Cálcio total", "Cálcio iônico", "Fósforo", "Magnésio", "Sódio", "Potássio",
            "Cloro", "Osmolaridade sérica", "Lactato", "PCR ultrassensível",
            "Procalcitonina", "Ferritina", "Ferro sérico", "Transferrina",
            "Capacidade total de ligação do ferro", "Vitamina D", "Vitamina B12",
            "Ácido fólico", "Perfil lipídico completo", "Proteína C reativa",
        ]),
        ("Hormônios", [
            "TSH", "T4 livre", "T3 livre", "T4 total", "Anti-TPO", "Anti-tireoglobulina",
            "Tireoglobulina", "PTH", "Calcitonina", "Insulina basal", "Insulina pós-prandial",
            "Peptídeo C", "Cortisol basal", "Cortisol após dexametasona", "ACTH",
            "Prolactina", "FSH", "LH", "Estradiol", "Progesterona", "Testosterona total",
            "Testosterona livre", "SHBG", "DHEA-S", "17-OH progesterona", "Androstenediona",
            "GH", "IGF-1", "Beta-HCG quantitativo", "Aldosterona", "Renina",
        ]),
        ("Sorologia", [
            "HIV 1/2", "HBsAg", "Anti-HBs", "Anti-HBc total", "Anti-HCV",
            "VDRL", "FTA-ABS", "Toxoplasmose IgG/IgM", "Rubéola IgG/IgM",
            "CMV IgG/IgM", "Epstein-Barr IgG/IgM", "Chagas IgG", "Dengue NS1/IgG/IgM",
            "COVID-19 Ag/PCR", "Influenza A/B", "ASLO", "Fator reumatoide",
            "Anti-CCP", "ANA", "Anti-DNA", "Complemento C3/C4", "IgE total",
            "IgE específico", "Widal", "Brucella", "Leptospirose IgM",
        ]),
        ("Urinálise", [
            "Urina tipo I", "Urina tipo II (24h)", "Urocultura", "Sumário de urina",
            "Microalbuminúria", "Relação albumina/creatinina", "Clearance de creatinina urinária",
            "Sódio urinário", "Potássio urinário", "Cálcio urinário", "Ácido úrico urinário",
            "Citrato urinário", "Osmolaridade urinária", "Densidade urinária",
            "Pesquisa de corpos cetônicos", "Pesquisa de bilirrubina urinária",
        ]),
        ("Parasitologia", [
            "Parasitológico de fezes", "Parasitológico seriado (3 amostras)",
            "Pesquisa de sangue oculto nas fezes", "Coprocultura", "Pesquisa de Giardia",
            "Pesquisa de Cryptosporidium", "Baermann (strongyloides)", "Kato-Katz",
            "Pesquisa de ovos de Schistosoma", "Hemocultura para parasitas",
        ]),
        ("Microbiologia", [
            "Hemocultura", "Urocultura", "Coprocultura", "Cultura de secreção traqueal",
            "Cultura de ferida", "Cultura de LCR", "Cultura de aspirado endocervical",
            "Antibiograma", "Pesquisa de BAAR", "Cultura para fungos",
            "Cultura para micobactérias", "Teste rápido Streptococcus A",
            "Pesquisa de Clostridium difficile", "MRSA screening", "Cultura de cateter",
            "Contagem bacteriana", "Teste de sensibilidade antifúngica",
        ]),
    ];

    internal static readonly (string Group, string[] Items)[] ImagingExams =
    [
        ("Radiologia", [
            "Radiografia de tórax PA e perfil", "Radiografia de abdome agudo",
            "Radiografia de coluna cervical", "Radiografia de coluna torácica",
            "Radiografia de coluna lombar", "Radiografia de crânio",
            "Radiografia de seios da face", "Radiografia de articulações",
            "Radiografia de mãos e punhos", "Radiografia de fêmur",
            "Radiografia de joelho", "Radiografia de tornozelo",
            "Radiografia de pelve", "Radiografia pediátrica de tórax",
            "Mamografia bilateral", "Mamografia com magnificação",
            "Densitometria óssea", "Radiografia contrastada (EED)",
        ]),
        ("Ultrassom", [
            "Ultrassom abdominal total", "Ultrassom abdominal superior",
            "Ultrassom de vias urinárias", "Ultrassom de próstata",
            "Ultrassom transvaginal", "Ultrassom obstétrico morfológico",
            "Ultrassom obstétrico 1º trimestre", "Ultrassom de tireoide",
            "Ultrassom de partes moles", "Ultrassom de mama",
            "Ultrassom doppler venoso MMII", "Ultrassom doppler arterial MMII",
            "Ultrassom doppler carótidas", "Ultrassom doppler de aorta",
            "Ecocardiograma transtorácico", "Ecocardiograma transesofágico",
            "Ultrassom pediátrico abdominal", "Ultrassom de crânio (fontanela)",
        ]),
        ("Tomografia", [
            "TC de crânio sem contraste", "TC de crânio com contraste",
            "TC de tórax", "TC de abdome total", "TC de pelve",
            "TC de coluna cervical", "TC de coluna lombar",
            "Angiotomografia de coronárias", "Angiotomografia de aorta",
            "Angiotomografia de crânio", "TC de seios da face",
            "TC de ouvidos (temporal)", "TC de articulações",
            "TC de tórax alta resolução", "Enterotomografia",
        ]),
        ("Ressonância", [
            "RM de crânio", "RM de crânio com contraste",
            "RM de coluna cervical", "RM de coluna torácica", "RM de coluna lombar",
            "RM de joelho", "RM de ombro", "RM de quadril",
            "RM de abdome superior", "RM de pelve", "RM de mama",
            "RM de coração", "RM de vias biliares (colangio-RM)",
            "RM funcional de crânio", "RM de plexo braquial",
            "Angio-RM de vasos cerebrais", "RM de próstata multiparamétrica",
        ]),
        ("Cardiologia", [
            "Ecocardiograma transtorácico", "Ecocardiograma com stress",
            "Ecocardiograma transesofágico", "Teste ergométrico",
            "Holter 24 horas", "MAPA 24 horas", "Eletrocardiograma de repouso",
            "Cintilografia miocárdica", "Angiotomografia de coronárias",
            "Cateterismo cardíaco diagnóstico", "Ressonância cardíaca",
            "Doppler de carótidas e vertebrais", "Doppler venoso de MMII",
        ]),
    ];
}
