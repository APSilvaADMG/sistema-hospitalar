namespace SistemaHospitalar.Infrastructure.Persistence;

internal static partial class HospitalErpCatalogSeedData
{
    internal static readonly (string Group, string[] Items)[] UserTypes =
    [
        ("Administrativos", ["Administrador Geral", "Diretor Hospitalar", "Gerente Administrativo", "Supervisor Administrativo", "Coordenador Administrativo", "Secretária", "Recepcionista", "Telefonista", "Ouvidoria", "Atendimento ao Cliente", "Controladoria", "Auditor Interno"]),
        ("Assistenciais", ["Médico", "Médico Plantonista", "Médico Especialista", "Enfermeiro", "Técnico de Enfermagem", "Auxiliar de Enfermagem", "Fisioterapeuta", "Psicólogo", "Nutricionista", "Fonoaudiólogo", "Terapeuta Ocupacional", "Assistente Social", "Farmacêutico", "Biomédico"]),
        ("Diagnóstico", ["Técnico de Radiologia", "Radiologista", "Técnico de Laboratório", "Bioquímico", "Patologista"]),
        ("Apoio", ["Almoxarifado", "Compras", "Patrimônio", "Engenharia Clínica", "TI", "Higienização", "Lavanderia", "Portaria", "Segurança"]),
        ("Financeiro", ["Faturista", "Analista Financeiro", "Contador", "Tesouraria", "Auditor de Convênios"]),
    ];

    internal static readonly (string Group, string[] Items)[] Sectors =
    [
        ("Atendimento", ["Recepção", "Cadastro", "Agendamento", "Call Center", "Atendimento Online", "Pronto Atendimento", "Pronto Socorro", "Ambulatório", "Consultórios"]),
        ("Assistenciais", ["Internação", "Centro Cirúrgico", "UTI Adulto", "UTI Pediátrica", "UTI Neonatal", "Observação", "Farmácia", "Nutrição"]),
        ("Diagnóstico", ["Laboratório", "Radiologia", "Ultrassonografia", "Tomografia", "Ressonância", "Hemodinâmica", "Endoscopia"]),
        ("Apoio", ["Almoxarifado", "CME", "Lavanderia", "Higienização", "Manutenção"]),
        ("Administrativo", ["RH", "Financeiro", "Compras", "Faturamento", "TI", "Diretoria"]),
    ];

    internal static readonly (string Group, string[] Items)[] Wards =
    [
        ("Internação", ["Ala Masculina", "Ala Feminina", "Ala Pediátrica", "Ala Obstétrica", "Ala Cirúrgica", "Ala Clínica"]),
        ("Terapia Intensiva", ["UTI Adulto", "UTI Pediátrica", "UTI Neonatal", "Semi Intensiva"]),
        ("Especializadas", ["Ala Oncológica", "Ala Cardiológica", "Ala Neurológica", "Ala Infectologia"]),
    ];

    internal static readonly string[] BedTypes =
    [
        "Apartamento", "Enfermaria", "UTI", "Isolamento", "Observação",
        "Recuperação Pós-Anestésica", "Berçário", "Semi Intensivo", "Emergência",
    ];

    internal static readonly (string Group, string[] Items)[] SupplierTypes =
    [
        ("Medicamentos", ["Distribuidor de Medicamentos", "Laboratório Farmacêutico", "Importador de Medicamentos"]),
        ("Materiais", ["Material Médico-Hospitalar", "Material de Limpeza", "Material de Escritório"]),
        ("Serviços", ["Serviços Gerais", "Manutenção Predial", "Terceirização de Limpeza", "Segurança Patrimonial"]),
        ("Tecnologia", ["Equipamentos Médicos", "Informática e TI", "Software Hospitalar"]),
        ("Diagnóstico", ["Reagentes Laboratoriais", "Contraste Radiológico", "Material de Patologia"]),
    ];

    internal static readonly (string Group, string[] Items)[] ProductTypes =
    [
        ("Medicamentos", ["Medicamento Genérico", "Medicamento Similar", "Medicamento Referência", "Medicamento Manipulado", "Vacina", "Soro e Solução"]),
        ("Materiais Hospitalares", ["Curativo", "Gaze e Compressa", "Sonda e Cateter", "Equipo e Extensor", "Luva e EPI", "Material Cirúrgico"]),
        ("OPME", ["Prótese Ortopédica", "Stent e Dispositivo Cardíaco", "Marca-passo", "Implante Oftalmológico", "Material de Síntese"]),
        ("Equipamentos", ["Equipamento Médico", "Equipamento de Laboratório", "Mobiliário Hospitalar", "Instrumental Cirúrgico"]),
        ("Consumo", ["Material de Limpeza", "Material de Escritório", "Alimentação", "Roupa Hospitalar"]),
    ];

    internal static readonly (string Group, string[] Items)[] ServiceTypes =
    [
        ("Atendimento", ["Consulta Ambulatorial", "Atendimento de Urgência", "Teleconsulta", "Visita Hospitalar"]),
        ("Internação", ["Diária de Enfermaria", "Diária de Apartamento", "Diária UTI", "Taxa de Observação"]),
        ("Diagnóstico", ["Exame Laboratorial", "Exame de Imagem", "Procedimento Endoscópico", "Biópsia"]),
        ("Terapias", ["Fisioterapia", "Nutrição Clínica", "Psicoterapia", "Hemodiálise", "Quimioterapia", "Radioterapia"]),
        ("Procedimentos", ["Procedimento Cirúrgico", "Procedimento Ambulatorial", "Anestesia", "Centro Cirúrgico"]),
    ];

    internal static readonly (string Name, string Cbo)[] Specialties =
    [
        ("Clínica Geral", "225125"), ("Cardiologia", "225120"), ("Pediatria", "225124"),
        ("Ortopedia e Traumatologia", "225270"), ("Ginecologia", "225250"), ("Obstetrícia", "225265"),
        ("Neurologia", "225112"), ("Neurocirurgia", "225260"), ("Endocrinologia", "225155"),
        ("Dermatologia", "225135"), ("Urologia", "225285"), ("Oftalmologia", "225265"),
        ("Otorrinolaringologia", "225275"), ("Psiquiatria", "225130"), ("Anestesiologia", "225151"),
        ("Cirurgia Geral", "225225"), ("Cirurgia Cardiovascular", "225210"), ("Oncologia", "225121"),
        ("Infectologia", "225103"), ("Nefrologia", "225109"), ("Gastroenterologia", "225165"),
        ("Pneumologia", "225127"), ("Reumatologia", "225136"), ("Geriatria", "225280"),
    ];

    internal static readonly string[] PermissionActions =
    [
        "Visualizar", "Incluir", "Alterar", "Excluir", "Imprimir",
        "Exportar PDF", "Exportar Excel", "Assinar Digitalmente", "Cancelar",
        "Aprovar", "Rejeitar", "Auditar", "Administrar",
    ];

    internal static readonly (string Name, string? Description)[] ReadyProfiles =
    [
        ("Administrador Master", "Acesso total ao sistema e configurações."),
        ("Diretor Hospitalar", "Visão executiva, relatórios e aprovações."),
        ("Coordenador Administrativo", "Gestão administrativa e cadastros."),
        ("Coordenador Assistencial", "Supervisão de equipes assistenciais."),
        ("Coordenador de Enfermagem", "Enfermagem, SAE e dispensação."),
        ("Médico Assistente", "PEP, prescrição e solicitação de exames."),
        ("Médico Especialista", "PEP especializado e procedimentos."),
        ("Enfermeiro", "Evolução de enfermagem e administração de medicamentos."),
        ("Técnico de Enfermagem", "Sinais vitais, curativos e cuidados."),
        ("Recepcionista", "Cadastro de pacientes, agendamento e check-in."),
        ("Faturista TISS", "Guias, lotes e faturamento convênio."),
        ("Farmacêutico", "Dispensação, estoque e validação farmacêutica."),
        ("Laboratório", "Coleta, processamento e laudos laboratoriais."),
        ("Radiologia", "Agendamento, execução e laudos de imagem."),
        ("Financeiro", "Contas a pagar/receber e tesouraria."),
        ("Recursos Humanos", "Colaboradores, escalas e folha."),
        ("Auditor", "Auditoria clínica e administrativa."),
        ("TI / Sistemas", "Usuários, integrações e parâmetros."),
        ("Portal do Paciente", "Acesso limitado ao próprio prontuário."),
    ];

    internal static readonly (string Name, string? Description)[] RegulatoryBases =
    [
        ("SIGTAP", "Tabela Unificada de Procedimentos do SUS — DATASUS."),
        ("TUSS", "Terminologia Unificada da Saúde Suplementar — ANS."),
        ("TISS", "Troca de Informação em Saúde Suplementar — ANS."),
        ("CID-10", "Classificação Internacional de Doenças, 10ª revisão."),
        ("CID-11", "Classificação Internacional de Doenças, 11ª revisão."),
        ("CIAP", "Classificação Internacional de Atenção Primária."),
        ("CBHPM", "Classificação Brasileira Hierarquizada de Procedimentos Médicos."),
        ("ANS", "Agência Nacional de Saúde Suplementar — normativas e rol."),
    ];

    internal static readonly string[] RecommendedModules =
    [
        "Dashboard Executivo", "Recepção e Cadastro", "Agendamento", "Pronto-Socorro",
        "Ambulatório", "Internação e Leitos", "UTI", "Centro Cirúrgico",
        "Prontuário Eletrônico (PEP)", "Enfermagem e SAE", "CCIH", "Laboratório",
        "Diagnóstico por Imagem", "Farmácia e Dispensação", "Hemoterapia", "Nutrição",
        "Oncologia", "Faturamento SUS", "Faturamento TISS / Convênio", "Financeiro",
        "Estoque e Almoxarifado", "Compras", "Recursos Humanos", "Engenharia Clínica",
        "Segurança e Portaria", "LGPD e Auditoria", "Integrações Governamentais",
        "Integrações HL7/FHIR", "Automação Hospitalar", "Connect / Comunicação",
        "Business Intelligence", "Qualidade e Acreditação", "Regulação de Leitos",
        "Telemedicina", "Transporte Interno", "Hotelaria Hospitalar",
    ];

    internal static readonly string[] TissGuideTypes =
    [
        "Guia de Consulta", "Guia SP/SADT", "Guia de Resumo de Internação",
        "Guia de Honorários Individuais", "Guia de OPME", "Guia de Quimioterapia",
        "Guia de Radioterapia", "Guia de Prorrogação de Internação",
        "Guia de Tratamento Odontológico", "Anexo de Situacao Inicial",
        "Anexo de Quimioterapia", "Anexo de Radioterapia", "Anexo de OPME",
        "Demonstrativo de Análise de Conta", "Demonstrativo de Pagamento",
    ];
}
