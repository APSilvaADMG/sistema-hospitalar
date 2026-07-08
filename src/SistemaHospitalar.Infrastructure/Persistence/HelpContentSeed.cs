using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class HelpContentSeed
{
    /// <summary>Incrementar ao alterar corpo dos artigos para forçar upsert em bases existentes.</summary>
    private const int ContentRevision = 2;

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var categoryDefs = new[]
        {
            Cat("pacientes", "Pacientes", "👤", 1),
            Cat("guias-tiss", "Guias / TISS / TUSS", "📋", 2),
            Cat("sus-sigtap", "SUS / SIGTAP", "🏥", 3),
            Cat("financeiro", "Financeiro", "💰", 4),
            Cat("agenda", "Agenda", "📅", 5),
            Cat("internacao", "Internação", "🛏️", 6),
            Cat("estoque", "Estoque", "📦", 7),
            Cat("relatorios", "Relatórios", "📊", 8),
            Cat("configuracoes", "Configurações", "⚙️", 9),
            Cat("connect", "Connect / Comunicação", "💬", 10),
            Cat("geral", "Sobre o sistema", "ℹ️", 11),
        };

        var existingCats = await db.HelpCategories.ToDictionaryAsync(c => c.Code, cancellationToken);
        foreach (var def in categoryDefs)
        {
            if (existingCats.TryGetValue(def.Code, out var cat))
            {
                cat.Name = def.Name;
                cat.Icon = def.Icon;
                cat.SortOrder = def.SortOrder;
                cat.IsActive = true;
            }
            else
            {
                db.HelpCategories.Add(def);
                existingCats[def.Code] = def;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var byCode = await db.HelpCategories.AsNoTracking().ToDictionaryAsync(c => c.Code, c => c.Id, cancellationToken);
        var incoming = BuildAllArticles(byCode).ToList();
        var existingArticles = await db.HelpArticles.ToDictionaryAsync(a => a.Slug, cancellationToken);

        foreach (var article in incoming)
        {
            article.Keywords = $"{article.Keywords},rev{ContentRevision}";
            if (existingArticles.TryGetValue(article.Slug, out var current))
            {
                current.Title = article.Title;
                current.Summary = article.Summary;
                current.Content = article.Content;
                current.Type = article.Type;
                current.VideoUrl = article.VideoUrl;
                current.DownloadUrl = article.DownloadUrl;
                current.Keywords = article.Keywords;
                current.ContextRoutes = article.ContextRoutes;
                current.SortOrder = article.SortOrder;
                current.CategoryId = article.CategoryId;
                current.IsActive = true;
                current.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.HelpArticles.Add(article);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<HelpArticle> BuildAllArticles(Dictionary<string, Guid> byCode)
    {
        var articles = new List<HelpArticle>();
        articles.AddRange(PacientesContent(byCode["pacientes"]));
        articles.AddRange(GuiasTissContent(byCode["guias-tiss"]));
        articles.AddRange(SusSigtapContent(byCode["sus-sigtap"]));
        articles.AddRange(FinanceiroContent(byCode["financeiro"]));
        articles.AddRange(AgendaContent(byCode["agenda"]));
        articles.AddRange(InternacaoContent(byCode["internacao"]));
        articles.AddRange(EstoqueContent(byCode["estoque"]));
        articles.AddRange(RelatoriosContent(byCode["relatorios"]));
        articles.AddRange(ConfiguracoesContent(byCode["configuracoes"]));
        articles.AddRange(ConnectContent(byCode["connect"]));
        articles.AddRange(GeralContent(byCode["geral"]));
        return articles;
    }

    private static HelpCategory Cat(string code, string name, string icon, int order) =>
        new() { Code = code, Name = name, Icon = icon, SortOrder = order };

    private static HelpArticle Faq(Guid catId, string slug, string title, string content, string routes, int order, string? keywords = null) =>
        new()
        {
            CategoryId = catId,
            Slug = slug,
            Title = title,
            Summary = Summarize(content, 280),
            Content = content,
            Type = HelpArticleType.Faq,
            ContextRoutes = routes,
            SortOrder = order,
            Keywords = keywords,
        };

    private static HelpArticle Article(Guid catId, string slug, string title, string content, string routes, int order, string? keywords = null) =>
        new()
        {
            CategoryId = catId,
            Slug = slug,
            Title = title,
            Summary = Summarize(content, 300),
            Content = content,
            Type = HelpArticleType.Article,
            ContextRoutes = routes,
            SortOrder = order,
            Keywords = keywords,
        };

    private static HelpArticle Video(Guid catId, string slug, string title, string summary, string content, string videoUrl, string routes, int order) =>
        new()
        {
            CategoryId = catId,
            Slug = slug,
            Title = title,
            Summary = summary,
            Content = content,
            Type = HelpArticleType.Video,
            VideoUrl = videoUrl,
            ContextRoutes = routes,
            SortOrder = order,
        };

    private static HelpArticle Manual(Guid catId, string slug, string title, string content, string downloadUrl, string routes, int order) =>
        new()
        {
            CategoryId = catId,
            Slug = slug,
            Title = title,
            Summary = Summarize(content, 200),
            Content = content,
            Type = HelpArticleType.Manual,
            DownloadUrl = downloadUrl,
            ContextRoutes = routes,
            SortOrder = order,
        };

    private static HelpArticle Training(Guid catId, string slug, string title, string content, string routes, int order) =>
        new()
        {
            CategoryId = catId,
            Slug = slug,
            Title = title,
            Summary = Summarize(content, 220),
            Content = content,
            Type = HelpArticleType.Training,
            ContextRoutes = routes,
            SortOrder = order,
        };

    private static string Summarize(string content, int max)
    {
        var flat = content.Replace("\n", " ").Trim();
        while (flat.Contains("  ")) flat = flat.Replace("  ", " ");
        return flat.Length <= max ? flat : flat[..max].TrimEnd() + "…";
    }

    private static IEnumerable<HelpArticle> PacientesContent(Guid catId) =>
    [
        Faq(catId, "pac-como-cadastrar", "Como cadastrar um novo paciente?", HelpArticleTexts.PacCadastrarFaq,
            "/recepcao/pacientes,/pacientes", 1, "cadastro paciente inserir novo"),
        Faq(catId, "pac-buscar-paciente", "Como localizar um paciente rapidamente?", HelpArticleTexts.PacBuscarFaq,
            "/recepcao/pacientes,/pacientes", 2, "buscar localizar paciente homônimo"),
        Faq(catId, "pac-convenio", "Como vincular convênio ao paciente?", HelpArticleTexts.PacConvenioFaq,
            "/recepcao/pacientes", 3, "convênio plano carteirinha elegibilidade"),
        Faq(catId, "pac-lgpd", "Quais dados do paciente são obrigatórios (LGPD)?",
            """
            O cadastro mínimo para atendimento inclui identificação do titular, base legal de tratamento (tutela da saúde) e informações necessárias ao cuidado.

            Dados obrigatórios operacionais: nome, data de nascimento, sexo, filiação (nome da mãe) e ao menos um contato. CPF/CNS fortemente recomendados para evitar duplicidade e habilitar faturamento.

            Consentimentos específicos (pesquisa, telemedicina, compartilhamento com operadora) são registrados em módulo apartado. O paciente pode exercer direitos pelo canal LGPD — não delete cadastros manualmente; use fluxo formal de anonimização solicitado ao DPO.

            Acesso ao prontuário é registrado em auditoria. Consultas sem necessidade de serviço configuram violação de política interna.
            """,
            "/recepcao/pacientes", 4, "lgpd consentimento dados pessoais"),
        Article(catId, "pac-fluxo-atendimento", "Fluxo completo de atendimento ambulatorial", HelpArticleTexts.PacFluxoArtigo,
            "/recepcao/pacientes", 10, "fluxo recepção pep faturamento"),
        Article(catId, "pac-responsavel", "Responsáveis legais e contatos de emergência",
            """
            Para pacientes menores ou incapazes, cadastre responsável legal com CPF, grau de parentesco e telefone prioritário.

            Contatos de emergência adicionais são opcionais mas recomendados em pronto-socorro. Em procedimentos eletivos, confirme autorização do responsável para menores.

            Atualize responsáveis a cada internação pediátrica — divergências entre cadastro e documento apresentado devem ser corrigidas antes de procedimentos invasivos.
            """,
            "/recepcao/pacientes/responsaveis,/recepcao/pacientes", 11, "responsável menor emergência"),
        Video(catId, "pac-video-cadastro", "Vídeo: tour pelo cadastro de pacientes",
            "Demonstração em vídeo dos campos do formulário Feegow, validações e vínculo de convênio.",
            "Este vídeo complementa o artigo de cadastro. Assista para conhecer atalhos de teclado, busca de CEP e salvamento parcial.\n\nDuração estimada: 8 minutos.",
            "https://www.youtube.com/embed/dQw4w9WgXcQ",
            "/recepcao/pacientes", 20),
        Training(catId, "pac-treino-recepcao", "Treinamento: recepção e cadastro (nível básico)", HelpArticleTexts.PacTreino,
            "/recepcao/pacientes", 30),
        Manual(catId, "pac-manual-cadastro", "Manual PDF: cadastro e identificação de pacientes",
            """
            Manual institucional de referência (download):

            • Campos obrigatórios e opcionais por tipo de atendimento.
            • RN-001 — identificação única do paciente.
            • Procedimento para unificação de cadastros duplicados.
            • Checklist de conferência na recepção (documento com foto, carteirinha, pedido médico).
            • Fluxograma de encaminhamento PS vs. ambulatorial.

            Versão digital atualizada mensalmente na Central de Downloads.
            """,
            "/relatorios/downloads?doc=pacientes-cadastro",
            "/recepcao/pacientes", 40),
    ];

    private static IEnumerable<HelpArticle> GuiasTissContent(Guid catId) =>
    [
        Faq(catId, "tiss-o-que-e", "O que é uma guia TISS e quando usar cada tipo?", HelpArticleTexts.TissOQueEFaq,
            "/guias,/faturamento-tiss", 1, "tiss guia ans consulta sp/sadt"),
        Faq(catId, "tiss-tuss", "Como pesquisar e manter o catálogo TUSS atualizado?", HelpArticleTexts.TissTussFaq,
            "/faturamento-tiss,/guias", 2, "tuss código procedimento importação"),
        Faq(catId, "tiss-lote", "Como fechar, validar e enviar lote TISS?", HelpArticleTexts.TissLoteFaq,
            "/faturamento-tiss,/guias", 3, "lote xml envio protocolo"),
        Faq(catId, "tiss-elegibilidade", "Elegibilidade e autorização prévia",
            """
            Antes de executar procedimentos de alto custo, confirme autorização da operadora (senha, GUID ou número de protocolo).

            A consulta de elegibilidade no cadastro do paciente valida se o plano está ativo na data do atendimento. Elegibilidade negativa não impede atendimento de urgência, mas impede faturamento convênio até regularização.

            Registre número de autorização na guia — glosas por falta de autorização são entre as mais frequentes em SP/SADT e internação.
            """,
            "/faturamento-tiss,/guias", 4, "autorização elegibilidade senha guia"),
        Article(catId, "tiss-guia-consulta", "Guia de consulta — preenchimento e validação", HelpArticleTexts.TissGuiaConsultaArtigo,
            "/faturamento-tiss", 10),
        Article(catId, "tiss-glosa", "Gestão de glosas e demonstrativos de pagamento", HelpArticleTexts.TissGlosaArtigo,
            "/guias,/faturamento-tiss", 11),
        Article(catId, "tiss-sp-sadt", "Guia SP/SADT — particularidades",
            """
            A guia SP/SADT cobre procedimentos ambulatoriais, exames de apoio diagnóstico e terapias serializadas.

            Pode conter múltiplos procedimentos TUSS na mesma guia, cada um com quantidade e valor. Anexos de OPME e quimioterapia seguem layouts específicos TISS.

            Atenção à compatibilidade entre procedimento solicitante e executante (CBO), caráter do atendimento (eletivo/urgência) e indicação clínica obrigatória para ressonância e tomografia em alguns contratos.
            """,
            "/faturamento-tiss,/guias", 12, "spsadt exame terapia"),
        Video(catId, "tiss-video-lote", "Vídeo: fechamento de lote e geração de XML",
            "Demonstração do assistente de lote: seleção de guias, validação de schema e exportação XML.",
            "Conteúdo: filtros por operadora, pré-validação, geração de hash do arquivo, registro de protocolo de envio e arquivamento para auditoria.",
            "https://www.youtube.com/embed/dQw4w9WgXcQ",
            "/faturamento-tiss", 20),
        Training(catId, "tiss-treino-basico", "Treinamento: sua primeira guia TISS", HelpArticleTexts.TissTreino,
            "/faturamento-tiss", 30),
        Manual(catId, "tiss-manual-tiss-3", "Referência TISS 3.05 — campos e layouts XML",
            """
            Documento de apoio com:
            • Tabela de tipos de guia e transação XML.
            • Campos obrigatórios por versão 3.05.00.
            • Códigos de glosa mais frequentes e ação recomendada.
            • Exemplo de XML anonimizado para testes de schema.

            Consulte também atualizações oficiais ANS em Configurações → Atualizações Oficiais.
            """,
            "/relatorios/downloads?doc=tiss-manual",
            "/faturamento-tiss", 40),
    ];

    private static IEnumerable<HelpArticle> SusSigtapContent(Guid catId) =>
    [
        Faq(catId, "sus-apac-bpa", "BPA, APAC ou AIH — qual instrumento usar?", HelpArticleTexts.SusApacBpaFaq,
            "/faturamento/sus,/guias,/faturamento", 1, "apac bpa aih sus produção"),
        Faq(catId, "sus-sigtap-sync", "Sincronização SIGTAP e competência ativa", HelpArticleTexts.SusSigtapSyncFaq,
            "/faturamento/sus,/guias", 2, "sigtap datasus competência importação"),
        Faq(catId, "sus-unidade", "Unidade de atendimento e CNES",
            """
            Todo lançamento SUS deve estar vinculado a estabelecimento com CNES válido e habilitado para o procedimento.

            Cadastre unidades em Configurações → Unidades de Atendimento. Campos: CNES, nome, tipo (executante/solicitante), município e gestão (municipal/estadual).

            Procedimentos com restrição de habilitação no SIGTAP serão bloqueados se o CNES não possuir a habilitação correspondente na competência.
            """,
            "/faturamento/sus,/configuracoes", 3, "cnes unidade habilitação"),
        Article(catId, "sus-fluxo-producao", "Fluxo de faturamento SUS no hospital", HelpArticleTexts.SusFluxoArtigo,
            "/faturamento/sus,/faturamento", 10),
        Article(catId, "sus-criticas", "Críticas comuns na exportação SUS",
            """
            Antes de exportar produção, o sistema executa críticas:

            • Procedimento incompatível com idade ou sexo do paciente.
            • CID obrigatório ausente ou incompatível.
            • Quantidade acima do máximo permitido na competência.
            • Profissional sem CNS ou CBO inválido.
            • Competência fechada ou data fora do período.

            Corrija críticas na tela de revisão; exportações com erro são rejeitadas pelo sistema destino e atrasam repasse do gestor.
            """,
            "/faturamento/sus", 11, "crítica exportação validação"),
        Video(catId, "sus-video-sigtap", "Vídeo: importação oficial SIGTAP (DATASUS)",
            "Passo a passo da sincronização one-click e conferência da competência importada.",
            "Inclui verificação do número de procedimentos, data da competência e rollback em caso de pacote corrompido.",
            "https://www.youtube.com/embed/dQw4w9WgXcQ",
            "/faturamento/sus", 20),
        Training(catId, "sus-treino-bpa", "Treinamento: consolidar BPA de demonstração",
            """
            1. Selecione unidade CNES de teste.
            2. Lance três procedimentos ambulatoriais distintos em pacientes diferentes.
            3. Execute críticas e corrija alertas.
            4. Gere prévia do BPA consolidado.
            5. Exporte arquivo de homologação (não envie a produção real).

            Conclusão: arquivo gerado sem críticas pendentes.
            """,
            "/faturamento/sus", 30),
        Manual(catId, "sus-manual-export", "Manual de exportação e layouts SIA/SUS",
            """
            Layouts de exportação BPA, APAC e AIH conforme versão do gestor municipal/estadual.

            Inclui mapeamento de campos do IASGH para colunas do arquivo texto, charset (ISO-8859-1), sequencial de linhas e procedimentos de contingência quando o webservice do gestor estiver indisponível.
            """,
            "/relatorios/downloads?doc=sus-export",
            "/faturamento/sus", 40),
    ];

    private static IEnumerable<HelpArticle> FinanceiroContent(Guid catId) =>
    [
        Faq(catId, "fin-contas-pagar", "Lançamento e aprovação de contas a pagar", HelpArticleTexts.FinContasPagarFaq,
            "/financeiro", 1, "pagar fornecedor nf aprovação"),
        Faq(catId, "fin-caixa", "Operação de caixa — abertura, movimentos e fechamento", HelpArticleTexts.FinCaixaFaq,
            "/financeiro,/financeiro/caixas", 2, "caixa fechamento sangria"),
        Faq(catId, "fin-pix", "Cobrança PIX e conciliação",
            """
            Gere cobrança PIX a partir de conta a receber ou atendimento particular. O QR Code dinâmico tem validade configurável.

            Status: pendente, pago, expirado ou cancelado. Pagamento confirmado baixa automaticamente a conta vinculada e registra movimento de caixa/banco conforme configuração.

            Para conciliação em lote, exporte extrato PIX e confronte com relatório de transações em Financeiro → Extratos.
            """,
            "/financeiro", 3, "pix cobrança qr code"),
        Article(catId, "fin-fluxo-receber", "Contas a receber, convênios e repasses", HelpArticleTexts.FinFluxoReceberArtigo,
            "/financeiro", 10),
        Article(catId, "fin-tef", "Transações TEF e cartões",
            """
            Integração TEF registra vendas no cartão com NSU, bandeira e parcelamento. Estornos exigem perfil autorizado e ficam na trilha de auditoria.

            Concilie o fechamento do adquirente (credenciadora) com movimentos do sistema antes de reconhecer receita na competência correta.
            """,
            "/financeiro/tef,/financeiro/cartoes", 11, "tef cartão adquirente"),
        Training(catId, "fin-treino-caixa", "Treinamento: turno completo de caixa",
            """
            Simule: abertura com R$ 200,00 → recebimento particular R$ 150,00 → sangria R$ 100,00 → fechamento com conferência.

            Documente qualquer diferença no campo de observação do fechamento. Anexe comprovante de sangria se política exigir.
            """,
            "/financeiro/caixas", 30),
        Manual(catId, "fin-manual-financeiro", "Manual do módulo financeiro",
            """
            Visão geral: plano de contas, centros de custo, contas a pagar/receber, caixas, bancos, TEF, PIX, honorários, fechamento contábil operacional e relatórios gerenciais (DRE operacional simplificada, fluxo de caixa projetado).
            """,
            "/relatorios/downloads?doc=financeiro-manual",
            "/financeiro", 40),
    ];

    private static IEnumerable<HelpArticle> AgendaContent(Guid catId) =>
    [
        Faq(catId, "agenda-encaixe", "Encaixes e política de sobreposição", HelpArticleTexts.AgendaEncaixeFaq,
            "/recepcao/agendamentos,/agenda", 1, "encaixe horário sobreposição"),
        Faq(catId, "agenda-confirmar", "Confirmação de agendamentos e redução de no-show",
            """
            O módulo Confirmar agendamentos lista consultas dos próximos dias com status pendente de confirmação.

            Envie confirmação por Connect (e-mail interno ao paciente cadastrado) ou WhatsApp quando integrado. Registre resposta: confirmado, reagendar ou cancelado.

            Métricas de no-show alimentam relatório de produtividade e podem disparar política de bloqueio de agenda para faltas recorrentes (configurável).
            """,
            "/recepcao/agendamentos/confirmar,/recepcao/agendamentos", 2, "confirmar lembrete whatsapp"),
        Faq(catId, "agenda-checkin", "Check-in e fila de espera",
            """
            Check-in registra horário real de chegada do paciente. A sala de espera (Espera no menu) ordena por horário de check-in ou prioridade clínica.

            Atrasos superiores ao tolerado pela unidade podem reclassificar o slot ou encaminhar para reagendamento — defina política local.
            """,
            "/recepcao/agendamentos/check-in,/emergencia", 3, "check-in espera fila"),
        Article(catId, "agenda-visoes", "Visões e filtros da agenda", HelpArticleTexts.AgendaVisoesArtigo,
            "/recepcao/agendamentos", 10),
        Training(catId, "agenda-treino", "Treinamento: ciclo agendar → confirmar → check-in",
            """
            1. Crie agendamento para paciente teste.
            2. Altere horário (reagendamento).
            3. Registre confirmação.
            4. No dia, execute check-in e verifique fila.

            Tempo estimado: 20 minutos.
            """,
            "/recepcao/agendamentos", 30),
    ];

    private static IEnumerable<HelpArticle> InternacaoContent(Guid catId) =>
    [
        Faq(catId, "int-solicitacao", "Solicitação e regulação de leitos", HelpArticleTexts.IntSolicitacaoFaq,
            "/internacao", 1, "solicitação leito regulação"),
        Faq(catId, "int-transferencia", "Transferência entre leitos e alas",
            """
            Com paciente internado, acesse Internação → Transferências. Selecione leito destino disponível (livre, higienizado).

            O sistema impede transferência para leito em manutenção ou ocupado. Motivo da transferência (clínico/administrativo) deve ser registrado.

            Histórico completo fica no prontuário de internação para cálculo de permanência e auditoria de infecção hospitalar (CCIH).
            """,
            "/internacao/transferencias,/internacao", 2, "transferência leito ala"),
        Article(catId, "int-fluxo", "Ciclo completo de internação", HelpArticleTexts.IntFluxoArtigo,
            "/internacao", 10),
        Article(catId, "int-alta", "Alta hospitalar e liberação de leito",
            """
            Alta médica precede alta administrativa. Registre CID de alta, resumo de internação e orientações.

            Alta administrativa dispara higienização do leito no mapa. Pendências de materiais ou conta aberta podem gerar alerta — resolva antes do fechamento AIH/guia de internação.

            Óbito segue fluxo específico com documentação obrigatória e comunicação à auditoria.
            """,
            "/internacao/altas,/internacao", 11, "alta hospitalar óbito"),
        Training(catId, "int-treino", "Treinamento: alta e liberação de leito",
            "Simule internação de teste → evolução mínima → alta médica → alta administrativa → confirme leito livre no mapa.",
            "/internacao", 30),
    ];

    private static IEnumerable<HelpArticle> EstoqueContent(Guid catId) =>
    [
        Faq(catId, "est-requisicao", "Requisições entre almoxarifados e setores", HelpArticleTexts.EstRequisicaoFaq,
            "/estoque,/estoque/requisicoes", 1, "requisição transferência aprovação"),
        Faq(catId, "est-kit", "Kits de produtos e dispensação cirúrgica",
            """
            Kits agrupam itens consumidos juntos (ex.: kit parto, kit laparoscopia). Ao dispensar kit, o sistema baixa cada componente na quantidade definida na composição.

            Alterações em kit exigem versionamento — cirurgias agendadas podem referenciar versão anterior até a data de corte configurada.

            Integração com centro cirúrgico: requisição automática a partir da agenda de cirurgias (quando módulo habilitado).
            """,
            "/estoque/kits,/estoque", 2, "kit produto cirurgia"),
        Faq(catId, "est-minimo", "Estoque mínimo e alertas",
            """
            Configure estoque mínimo e ponto de reposição por produto. Alertas aparecem no dashboard principal (itens abaixo do mínimo).

            Produtos próximos ao vencimento (90/60/30 dias) geram relatório em Estoque → Listar com filtro de validade. Priorize saída FEFO.

            Inventário cíclico: contagem parcial rotativa reduz divergência anual.
            """,
            "/estoque", 3, "mínimo vencimento inventário"),
        Article(catId, "est-farmacia-ala", "Farmácia por ala — operação detalhada", HelpArticleTexts.EstFarmaciaAlaArtigo,
            "/estoque/farmacia-ala,/estoque", 10),
        Training(catId, "est-treino", "Treinamento: entrada de NF e requisição",
            "Registre entrada por nota fiscal de fornecedor teste → confira saldo → crie requisição para farmácia de ala → aprove e confirme recebimento.",
            "/estoque", 30),
    ];

    private static IEnumerable<HelpArticle> RelatoriosContent(Guid catId) =>
    [
        Faq(catId, "rel-export", "Exportação e filtros de relatórios", HelpArticleTexts.RelExportFaq,
            "/relatorios", 1, "exportar csv pdf filtro"),
        Faq(catId, "rel-downloads", "Central de downloads e manuais",
            """
            Manuais oficiais, layouts de exportação SUS/TISS e modelos de documentos ficam em Relatórios → Downloads (também acessível por Configurações → Downloads).

            Arquivos são versionados — confira data de publicação antes de usar em produção. Sugestões de novos documentos: Ajuda → Sugestões.
            """,
            "/relatorios,/relatorios/downloads", 2, "downloads manual layout"),
        Article(catId, "rel-producao", "Produção assistencial e faturamento — relatórios", HelpArticleTexts.RelProducaoArtigo,
            "/relatorios", 10),
        Article(catId, "rel-bi", "BI e indicadores avançados",
            """
            O módulo BI (/bi) oferece visões analíticas: tendências de ocupação, receita por especialidade, tempo médio de permanência, glosa por operadora.

            Dados do dashboard operacional alimentam KPIs em tempo real; BI é indicado para análise histórica e reuniões de diretoria.
            """,
            "/relatorios,/bi", 11, "bi indicadores analytics"),
    ];

    private static IEnumerable<HelpArticle> ConfiguracoesContent(Guid catId) =>
    [
        Faq(catId, "cfg-parametros", "Parâmetros gerais da instituição", HelpArticleTexts.CfgParametrosFaq,
            "/configuracoes", 1, "parâmetros sistema unidade"),
        Faq(catId, "cfg-usuarios", "Usuários, perfis e permissões (RBAC)", HelpArticleTexts.CfgUsuariosFaq,
            "/configuracoes,/usuarios", 2, "usuário permissão perfil acesso"),
        Article(catId, "cfg-integracoes", "Integrações oficiais e APIs", HelpArticleTexts.CfgIntegracoesArtigo,
            "/configuracoes", 10),
        Article(catId, "cfg-backup", "Backup, atualização e ambiente",
            """
            Ambiente Docker: serviços api, web e postgres com healthchecks. Atualizações de versão devem incluir backup do banco antes de migration.

            Teste migrations em homologação. Rollback de migration não automático — restaure backup se necessário.

            Variáveis sensíveis (JWT, webhook Meta, connection string) via secrets, nunca commitadas no repositório.
            """,
            "/configuracoes", 11, "backup docker migration"),
        Manual(catId, "cfg-manual-admin", "Manual do administrador do sistema",
            """
            Checklist de implantação: cadastros base, unidades CNES, operadoras, profissionais, perfis de acesso, tabelas TUSS/SIGTAP, teste de lote TISS homologação, treinamento por área.

            Manutenção mensal: revisão de usuários, atualização SIGTAP, conferência de integrações, restore test de backup.
            """,
            "/relatorios/downloads?doc=admin-manual",
            "/configuracoes", 40),
    ];

    private static IEnumerable<HelpArticle> ConnectContent(Guid catId) =>
    [
        Faq(catId, "conn-chamado", "Chamados de suporte — abertura e acompanhamento", HelpArticleTexts.ConnChamadoFaq,
            "/connect,/ajuda", 1, "chamado ticket suporte protocolo"),
        Faq(catId, "conn-chat", "Chat interno e salas de conversa",
            """
            Connect → Chat: mensagens em tempo real entre equipes, com histórico e anexos (conforme permissão).

            Crie salas por projeto ou setor. Menções (@usuário) geram notificação. Não utilize chat para dados clínicos detalhados de paciente — use PEP e referencie apenas ID interno quando necessário.
            """,
            "/connect/chat,/connect", 2, "chat mensagem equipe"),
        Faq(catId, "conn-mural", "Mural de comunicados institucionais",
            """
            Comunicados oficiais (políticas, feriados, manutenção programada) são publicados no Mural.

            Leitura pode ser rastreada para comunicados obrigatórios. Administradores publicam via Connect → Mural com perfil adequado.
            """,
            "/connect", 3, "mural comunicado aviso"),
        Article(catId, "conn-whatsapp", "WhatsApp oficial — configuração e operação", HelpArticleTexts.ConnWhatsappArtigo,
            "/connect/whatsapp,/connect", 10),
        Article(catId, "conn-email", "E-mail interno (Connect Mail)",
            """
            Caixa de entrada, enviadas e rascunhos. Anexos com limite de tamanho configurável. Integração com pendências e aprovações de workflow.

            Use assunto padronizado para rastreio: [SETOR] Assunto. Mensagens não lidas aparecem no assistente Connect AI e no dashboard de notificações.
            """,
            "/connect", 11, "email interno caixa"),
    ];

    private static IEnumerable<HelpArticle> GeralContent(Guid catId) =>
    [
        Faq(catId, "geral-sobre", "Sobre o IASGH / APSMed Hospitalar", HelpArticleTexts.GeralSobreFaq,
            "/,/dashboard,/ajuda", 1, "sobre sistema apsmed iasgh"),
        Faq(catId, "geral-lgpd", "Privacidade, LGPD e auditoria", HelpArticleTexts.GeralLgpdFaq,
            "/ajuda,/seguranca-lgpd", 2, "lgpd privacidade dpo auditoria"),
        Faq(catId, "geral-atalhos", "Atalhos e busca rápida",
            """
            • Busca de paciente na barra superior (qualquer tela).
            • Ctrl+K / busca de módulos (quando habilitado no layout).
            • ? Ajuda contextual na página atual.
            • Assistente flutuante (canto inferior direito) para dúvidas na base de ajuda.

            Central de Ajuda completa: menu ? ou /ajuda.
            """,
            "/dashboard,/ajuda", 3, "atalho busca ajuda"),
        Article(catId, "geral-modulos", "Mapa de módulos e navegação Feegow", HelpArticleTexts.GeralModulosArtigo,
            "/dashboard", 10),
        Video(catId, "geral-video-tour", "Tour guiado pelo sistema (vídeo)",
            "Visão geral dos módulos principais em linguagem acessível para novos usuários.",
            "Roteiro: login → dashboard → paciente → agenda → guia TISS → financeiro → Connect → ajuda. Duração: ~12 min.",
            "https://www.youtube.com/embed/dQw4w9WgXcQ",
            "/dashboard,/ajuda", 20),
    ];
}
