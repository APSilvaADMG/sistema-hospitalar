using Microsoft.EntityFrameworkCore;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class MedicationBulaSeed
{
    private static readonly Dictionary<string, string> Bulas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dipirona 500mg"] = Bula(
            "Analgésico e antitérmico para dor leve a moderada e febre.",
            "Adultos: 500mg a 1g VO a cada 6h, máx. 4g/dia. Ajustar em insuficiência hepática/renal.",
            "Hipersensibilidade a pirazolonas; porfiria; primeiro trimestre de gestação; lactação.",
            "Reações alérgicas, agranulocitose (rara), hipotensão com uso IV rápido.",
            "Potencializa depressores do SNC e anticoagulantes.",
            "VO com água. Evitar uso prolongado sem orientação médica."),
        ["Paracetamol 750mg"] = Bula(
            "Analgésico e antitérmico.",
            "Adultos: 750mg VO a cada 6h, máx. 3g/dia.",
            "Insuficiência hepática grave; hipersensibilidade.",
            "Hepatotoxicidade em superdose; rash cutâneo.",
            "Cuidado com outros produtos contendo paracetamol.",
            "Não exceder dose diária. Preferir menor tempo de uso."),
        ["Omeprazol 20mg"] = Bula(
            "Inibidor de bomba de prótons para DRGE, úlcera péptica e erradicação de H. pylori.",
            "20mg VO 1x/dia em jejum, 4 a 8 semanas conforme indicação.",
            "Hipersensibilidade aos IBPs.",
            "Cefaleia, diarreia, náusea; uso prolongado: deficiência de B12 e magnésio.",
            "Clopidogrel, metotrexato, digoxina.",
            "Engolir cápsula inteira; não mastigar."),
        ["Losartana 50mg"] = Bula(
            "Anti-hipertensivo (BRA) para HAS e nefroproteção em DM2.",
            "50mg VO 1x/dia; pode aumentar até 100mg.",
            "Gestação; estenose bilateral de artéria renal; hipercalemia.",
            "Tontura, hipercalemia, tosse (menos que IECA), angioedema (raro).",
            "Potássio, AINEs, litio.",
            "Monitorar função renal e potássio."),
        ["Atenolol 50mg"] = Bula(
            "Betabloqueador para HAS, angina e arritmias.",
            "50mg VO 1x/dia; titular conforme PA e FC.",
            "Bradicardia, BAV, IC descompensada, asma grave.",
            "Bradicardia, fadiga, broncoespasmo, impotência.",
            "Bloqueadores de canal de cálcio não diidropiridínicos, insulina.",
            "Não suspender abruptamente."),
        ["Sinvastatina 20mg"] = Bula(
            "Hipolipemiante (estatina) para dislipidemia e prevenção cardiovascular.",
            "20mg VO à noite.",
            "Doença hepática ativa; gestação; lactação.",
            "Mialgia, elevação de transaminases, rabdomiólise (rara).",
            "Fibratos, antifúngicos azólicos, inibidores de protease.",
            "Orientar sinais de mialgia; dosar CK se necessário."),
        ["AAS 100mg"] = Bula(
            "Antiagregante plaquetário em prevenção cardiovascular.",
            "100mg VO 1x/dia após refeição.",
            "Úlcera péptica ativa, hemorragia ativa, asma induzida por AAS.",
            "Gastrite, sangramento GI, urticária.",
            "Anticoagulantes, AINEs, metotrexato.",
            "Suspender antes de cirurgias conforme orientação."),
        ["Furosemida 40mg"] = Bula(
            "Diurético de alça para edema e IC.",
            "40mg VO 1x/dia ou conforme prescrição.",
            "Anúria, hipovolemia grave, hipersensibilidade.",
            "Hipocalemia, hipotensão, ototoxicidade (doses altas IV).",
            "Aminoglicosídeos, lítio, digoxina.",
            "Monitorar eletrólitos e função renal."),
        ["Metformina 850mg"] = Bula(
            "Antidiabético oral para DM2.",
            "850mg VO 12/12h com refeições.",
            "Insuficiência renal (TFG <30), acidose metabólica, desidratação.",
            "Distúrbios GI, acidose lática (rara), deficiência de B12.",
            "Contraste iodado: suspender 48h antes/depois.",
            "Titular gradualmente para reduzir náusea."),
        ["Levotiroxina 50mcg"] = Bula(
            "Reposição hormonal tireoidiana.",
            "50mcg VO em jejum; ajustar por TSH.",
            "Tireotoxicose não tratada, IAM recente.",
            "Palpitações, perda de peso, insônia se superdosagem.",
            "Cálcio, ferro, omeprazol (separar 4h).",
            "Tomar sempre no mesmo horário, em jejum."),
        ["Insulina NPH"] = Bula(
            "Insulina intermediária para diabetes.",
            "Dose conforme esquema; geralmente 2x/dia SC.",
            "Hipoglicemia.",
            "Hipoglicemia, lipodistrofia no local da aplicação.",
            "Betabloqueadores mascaram hipoglicemia.",
            "Rodar locais de aplicação; não agitar vigorosamente."),
        ["Amoxicilina 500mg"] = Bula(
            "Antibiótico beta-lactâmico para infecções bacterianas.",
            "500mg VO 8/8h por 7 dias (ajustar conforme foco).",
            "Alergia a penicilinas.",
            "Diarreia, rash, candidíase.",
            "Metotrexato, anticoagulantes orais.",
            "Completar o ciclo prescrito."),
        ["Ibuprofeno 100mg/mL"] = Bula(
            "AINE pediátrico para dor e febre.",
            "Dose por peso: 5-10mg/kg VO 6/6-8/8h.",
            "Úlcera péptica, insuficiência renal grave, <6 meses.",
            "Gastrite, rash, retenção hídrica.",
            "Outros AINEs, anticoagulantes.",
            "Usar seringa dosadora; administrar com alimento."),
        ["Prednisolona 3mg/mL"] = Bula(
            "Corticosteroide sistêmico anti-inflamatório e imunossupressor.",
            "Dose conforme peso e indicação clínica.",
            "Infecções sistêmicas não tratadas.",
            "Supressão adrenal, hiperglicemia, insônia, ganho de peso.",
            "AINEs, anticoagulantes, vacinas de vírus vivos.",
            "Não interromper abruptamente após uso prolongado."),
        ["Cetoprofeno 100mg"] = Bula(
            "AINE para dor musculoesquelética e inflamatória.",
            "100mg VO 12/12h com alimento.",
            "Úlcera péptica ativa, insuficiência cardíaca grave.",
            "Gastrite, edema, elevação de PA.",
            "Anticoagulantes, outros AINEs.",
            "Menor tempo e menor dose eficaz."),
        ["Tramadol 50mg"] = Bula(
            "Analgésico opioide fraco para dor moderada a intensa.",
            "50-100mg VO 6/6-8/8h; máx. 400mg/dia.",
            "Intoxicação aguda por álcool/depressores; epilepsia não controlada.",
            "Náusea, tontura, constipação, convulsões (dose alta).",
            "ISRS, IMAO, carbamazepina.",
            "Risco de dependência; evitar dirigir."),
        ["Ácido tranexâmico 250mg"] = Bula(
            "Antifibrinolítico para sangramentos e menorragia.",
            "Conforme protocolo clínico (ex.: 1g 8/8h).",
            "Trombose ativa, coagulação intravascular disseminada.",
            "Náusea, trombose, distúrbios visuais (uso prolongado).",
            "Anticoagulantes.",
            "Avaliar risco trombótico."),
        ["Noretisterona 0.35mg"] = Bula(
            "Progestagênio para contracepção e distúrbios menstruais.",
            "1 comprimido VO diário, mesmo horário.",
            "Trombose, câncer de mama, sangramento vaginal inexplicado.",
            "Sangramento irregular, cefaleia, ganho de peso.",
            "Anticonvulsivantes enzimaindutores.",
            "Orientar esquecimento de dose."),
        ["Carbamazepina 200mg"] = Bula(
            "Anticonvulsivante e estabilizador do humor.",
            "Iniciar 200mg VO 12/12h; titular lentamente.",
            "Bloqueio AV, porfiria, hipersensibilidade a tricíclicos.",
            "Ataxia, diplopia, leucopenia, SJS (raro).",
            "Indutores/inibidores CYP3A4.",
            "Dosar níveis séricos quando disponível."),
        ["Topiramato 50mg"] = Bula(
            "Anticonvulsivante; profilaxia de enxaqueca.",
            "50mg VO 12/12h; titulação gradual.",
            "Glaucoma agudo de ângulo fechado.",
            "Parestesias, déficit cognitivo, perda de peso, acidose metabólica.",
            "Anticoncepcionais orais (reduz eficácia).",
            "Aumentar hidratação; evitar calor excessivo."),
        ["Hidrocortisona tópica 1%"] = Bula(
            "Corticosteroide tópico para dermatite e eczema.",
            "Aplicar fina camada 1-2x/dia na lesão.",
            "Infecções cutâneas não tratadas no local.",
            "Atrofia cutânea, estrias, acne com uso prolongado.",
            "Poucas interações sistêmicas em uso tópico.",
            "Evitar face e dobras por tempo prolongado."),
        ["Loratadina 10mg"] = Bula(
            "Anti-histamínico para rinite e urticária.",
            "10mg VO 1x/dia.",
            "Hipersensibilidade.",
            "Sonolência leve, cefaleia, boca seca.",
            "Inibidores CYP3A4 (aumentam níveis).",
            "Preferir à noite se houver sonolência."),
        ["Soro fisiológico 0,9% 500mL"] = Bula(
            "Solução eletrolítica para hidratação e diluição de medicamentos IV.",
            "Conforme prescrição médica e balanço hídrico.",
            "Hipervolemia, hipernatremia.",
            "Sobrecarga volêmica, flebite no local.",
            "Verificar compatibilidade na Y com outros fármacos.",
            "Usar equipo estéril; monitorar sinais de sobrecarga."),
        ["Dexametasona 4mg"] = Bula(
            "Corticosteroide potente anti-inflamatório e imunossupressor.",
            "4-20mg/dia VO ou conforme protocolo.",
            "Infecções fúngicas sistêmicas não tratadas.",
            "Hiperglicemia, insônia, imunossupressão.",
            "AINEs, anticoagulantes, vacinas vivas.",
            "Reduzir gradualmente após uso >7 dias."),
    };

    public static async Task ApplyAsync(AppDbContext db, CancellationToken ct = default)
    {
        var meds = await db.MedicationCatalogs
            .Where(m => m.PackageInsert == null || m.PackageInsert == "")
            .ToListAsync(ct);

        foreach (var med in meds)
        {
            if (Bulas.TryGetValue(med.Name, out var bula))
            {
                med.PackageInsert = bula;
            }
            else
            {
                med.PackageInsert = Bula(
                    med.Notes ?? $"Medicamento hospitalar: {med.Name}.",
                    med.DefaultDosage ?? "Conforme prescrição médica.",
                    "Hipersensibilidade ao princípio ativo.",
                    "Consultar literatura específica e farmacovigilância.",
                    "Verificar interações no momento da prescrição.",
                    $"Via: {med.Route ?? "conforme prescrição"}. Forma: {med.PharmaceuticalForm ?? "—"}.");
            }
        }
    }

    private static string Bula(
        string indicacoes,
        string posologia,
        string contraindicacoes,
        string efeitos,
        string interacoes,
        string cuidados) =>
        $"INDICAÇÕES\n{indicacoes}\n\n" +
        $"POSOLOGIA\n{posologia}\n\n" +
        $"CONTRAINDICAÇÕES\n{contraindicacoes}\n\n" +
        $"EFEITOS ADVERSOS\n{efeitos}\n\n" +
        $"INTERAÇÕES\n{interacoes}\n\n" +
        $"CUIDADOS NA ADMINISTRAÇÃO\n{cuidados}";
}
