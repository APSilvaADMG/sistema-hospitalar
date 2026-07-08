namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>Corpo dos artigos da Central de Ajuda — conteúdo completo para operadores hospitalares.</summary>
public static class HelpArticleTexts
{
    // ── Pacientes ──
    public const string PacCadastrarFaq =
        """
        O cadastro de pacientes é o ponto de partida de todo atendimento. Acesse o menu superior Pacientes → Inserir ou use o atalho na sidebar.

        Campos essenciais:
        • Nome civil completo (obrigatório) e nome social, quando informado pelo paciente.
        • Data de nascimento, sexo ao nascer e nome da mãe (identificação em conformidade com RN-001 do sistema).
        • CPF e/ou CNS — ao menos um documento de identificação reduz duplicidades.
        • Telefone celular e e-mail para confirmações de agenda e comunicação via Connect.

        Após salvar, o sistema gera um identificador único (UUID) visível na busca rápida da barra superior. Recomenda-se vincular convênio ainda na recepção, antes do encaminhamento ao consultório.

        Dica: pacientes sem CPF (recém-nascidos, estrangeiros) podem ser cadastrados com documento alternativo; registre observação no campo de notas para auditoria.
        """;

    public const string PacBuscarFaq =
        """
        Existem três formas principais de localizar um paciente:

        1. Busca rápida na barra superior Feegow — digite nome parcial ou os primeiros caracteres do ID. Resultados aparecem em dropdown; clique para abrir o prontuário.
        2. Pacientes → Listar — filtros por nome, CPF, data de nascimento e status ativo/inativo.
        3. Durante agendamento ou faturamento — campos de autocomplete buscam a mesma base unificada.

        Para homônimos, confirme data de nascimento e últimos dígitos do CPF antes de abrir atendimento. Em caso de cadastro duplicado, solicite ao administrador a unificação (módulo Configurações → Cadastros), nunca crie um segundo prontuário para o mesmo indivíduo.
        """;

    public const string PacConvenioFaq =
        """
        Na ficha do paciente, abra a seção Planos / Convênios e clique em Adicionar.

        Informe: operadora (ANS), número da carteirinha, validade, acomodação (enfermaria/apartamento quando exigido) e titularidade (titular ou dependente). O campo plano deve corresponder ao contrato vigente na operadora.

        Quando a integração de elegibilidade estiver ativa (Configurações → APIs), use o botão Consultar elegibilidade antes do atendimento. Registros negados devem orientar o paciente à recepção para particular ou correção de dados.

        Convênios múltiplos: é possível cadastrar mais de um plano; defina qual é o principal para faturamento automático de novas guias TISS.
        """;

    public const string PacFluxoArtigo =
        """
        Fluxo completo — atendimento ambulatorial

        1. Identificação e cadastro
        Confirme identidade na recepção. Atualize telefone e endereço a cada visita. Verifique alergias e comorbidades no PEP se já houver histórico.

        2. Agendamento ou encaixe
        Consultas eletivas passam pela Agenda (diária/semanal). Encaixes exigem permissão e devem registrar motivo clínico quando aplicável.

        3. Check-in
        Registre chegada do paciente para alimentar fila de espera e indicadores de pontualidade. Check-in pode disparar notificação ao profissional via Connect.

        4. Atendimento clínico (PEP)
        Anamnese, evolução, prescrição e solicitação de exames ficam no prontuário eletrônico. Todos os registros são auditáveis.

        5. Procedimentos e exames
        Laboratório, imagem e procedimentos ambulatoriais geram pedidos vinculados ao atendimento do dia.

        6. Faturamento e alta administrativa
        Gere guia TISS ou registre produção SUS conforme o convênio. Particulares: conta a receber ou recebimento no caixa.

        7. Retorno e continuidade
        Agende retorno na alta ou envie orientações por Connect (e-mail interno / WhatsApp quando habilitado).
        """;

    public const string PacTreino =
        """
        Treinamento prático — Recepção e cadastro (≈ 30 min)

        Objetivo: percorrer o ciclo recepção → agenda → check-in sem erros de cadastro.

        Passo 1 — Cadastro
        Crie paciente fictício "Maria Silva Teste" com CPF válido de homologação. Preencha todos os campos obrigatórios.

        Passo 2 — Convênio
        Vincule operadora de demonstração e carteirinha 000000000000.

        Passo 3 — Agendamento
        Agende consulta clínica para amanhã, 09:00, com profissional ativo.

        Passo 4 — Check-in
        No dia simulado (ou com data alterada), registre check-in e confirme aparição na fila.

        Passo 5 — Conferência
        Abra o PEP e verifique se o paciente está vinculado ao atendimento.

        Ao concluir, marque este treinamento como concluído. Em caso de dúvida, abra chamado em Ajuda → Suporte.
        """;

    // ── TISS / Guias ──
    public const string TissOQueEFaq =
        """
        TISS (Troca de Informação em Saúde Suplementar) é o padrão da ANS para troca de dados entre prestadores e operadoras de planos de saúde.

        No IASGH, cada guia representa um atendimento faturável e contém: dados do beneficiário, contratado executante, procedimentos TUSS, valores e indicadores clínicos exigidos pelo tipo de guia (consulta, SP/SADT, honorário individual, resumo de internação etc.).

        Ciclo de vida típico: Rascunho → Validada → Enviada em lote XML → Processada pela operadora → Paga ou Glosada. Guias em rascunho não impactam produção até validação.

        Mantenha a versão TISS configurada em Configurações alinhada ao contrato com cada operadora (ex.: 3.05.00).
        """;

    public const string TissTussFaq =
        """
        O catálogo TUSS (Terminologia Unificada da Saúde Suplementar) padroniza códigos de procedimentos, materiais, medicamentos e taxas.

        Ao incluir item na guia: pesquise por código numérico ou trecho da descrição. O sistema aplica formatação de descrição e ordenação por código.

        Atualização de tabelas:
        • Importação XLSX/CSV em Faturamento TISS → Catálogos.
        • Upsert inteligente: registros idênticos são ignorados; alterações atualizam descrição; novos códigos são inseridos.
        • Evite duplicar importações da mesma versão — o sistema impõe unicidade por código.

        Para OPME e medicamentos, verifique também tabelas Brasíndice/Simpro quando o contrato exigir codificação paralela.
        """;

    public const string TissLoteFaq =
        """
        Fechamento de lote agrupa guias validadas para envio único à operadora.

        Procedimento recomendado:
        1. Filtre guias por operadora, competência e status Validada.
        2. Execute validação de schema (campos obrigatórios, totais, CID quando exigido).
        3. Gere XML conforme versão TISS ativa e registre número do lote.
        4. Transmita pelo canal da operadora (portal, SFTP ou integração).
        5. Arquive XML enviado e protocolo de recebimento.

        Lotes rejeitados: corrija guias indicadas no retorno, não reenvie o mesmo arquivo sem alteração. Use o histórico de auditoria para rastrear quem alterou cada guia.
        """;

    public const string TissGuiaConsultaArtigo =
        """
        Guia de consulta — passo a passo

        Pré-requisitos: paciente com convênio ativo, profissional com CRM/CBO cadastrado, procedimento TUSS de consulta compatível com especialidade.

        Preenchimento:
        • Cabeçalho: operadora, número da guia (automático ou manual conforme operadora), dados do beneficiário espelhados do cadastro.
        • Executante: profissional, conselho, UF, CBO.
        • Procedimento: código TUSS, quantidade (geralmente 1), valor unitário conforme tabela contratada.
        • Indicação clínica / CID quando exigido pelo rol.

        Validações do sistema bloqueiam envio se faltar carteirinha, procedimento incompatível ou data de atendimento futura. Após salvar como Validada, a guia entra na fila de lotes.

        Consultas de retorno na mesma especialidade dentro do prazo contratual podem exigir procedimento TUSS específico — consulte tabela da operadora.
        """;

    public const string TissGlosaArtigo =
        """
        Gestão de glosas e demonstrativos

        O demonstrativo de pagamento (retorno da operadora) lista guias pagas, pagas parcialmente ou glosadas com motivo (código de glosa TISS).

        Fluxo no sistema:
        1. Importe ou registre demonstrativo vinculado à competência.
        2. Associe itens às guias originais — divergências de valor aparecem destacadas.
        3. Para glosa procedente: ajuste guia ou registre perda conforme política financeira.
        4. Para glosa indevida: monte recurso com documentação anexa (laudo, relatório, autorização).

        Métricas de glosa por operadora e motivo estão em Relatórios → Faturamento. Meta assistencial: reduzir glosas evitáveis com elegibilidade prévia e conferência de codificação TUSS antes do lote.
        """;

    public const string TissTreino =
        """
        Treinamento — Primeira guia TISS (≈ 45 min)

        1. Selecione paciente com convênio de teste.
        2. Crie guia de consulta em status Rascunho.
        3. Adicione procedimento TUSS 10101012 (consulta em consultório — exemplo).
        4. Revise totais e salve como Validada.
        5. Simule inclusão em lote (sem transmitir XML real se ambiente de homologação).
        6. Altere status para Enviada e registre observação de teste.

        Critério de conclusão: guia visível no hub de guias com histórico de alterações na auditoria.
        """;

    // ── SUS / SIGTAP ──
    public const string SusApacBpaFaq =
        """
        A produção SUS utiliza instrumentos distintos conforme o tipo de atendimento:

        BPA (Boletim de Produção Ambulatorial): consolida procedimentos ambulatoriais (consultas, pequenos procedimentos, exames) em produção mensal. Layout consolidado (BPA-C) ou individualizado (BPA-I) conforme exigência do gestor.

        APAC (Autorização de Procedimentos de Alta Complexidade): obrigatória para procedimentos de alta complexidade ambulatorial (oncologia, diálise, etc.) antes da execução. Exige CID, quadro clínico e validade.

        AIH (Autorização de Internação Hospitalar): para internações no SUS — número da AIH, procedimento principal, CID, permanência e alta.

        O hub Guias → SUS orienta qual instrumento usar. Nunca fature AIH em canal ambulatorial ou vice-versa — inconsistências geram rejeição na exportação municipal/estadual.
        """;

    public const string SusSigtapSyncFaq =
        """
        SIGTAP (Sistema de Gerenciamento da Tabela de Procedimentos do SUS) define procedimentos, compatibilidades, valores e regras por competência.

        Sincronização oficial no IASGH:
        1. Acesse Guias / Faturamento SUS.
        2. Clique em Sincronizar SIGTAP oficial — o sistema busca pacote DATASUS (FTP/RSS) da competência mais recente.
        3. Aguarde importação (~5 mil procedimentos por competência típica).
        4. Confira competência ativa exibida no painel antes de gerar produção.

        Procedimentos inativos na competência não devem ser lançados em produção nova. Atualize mensalmente ou quando o gestor publicar portaria de revisão.
        """;

    public const string SusFluxoArtigo =
        """
        Fluxo de faturamento SUS no hospital

        1. Cadastro: unidade de atendimento com CNES válido (Configurações → Unidades).
        2. Atendimento: registro clínico vinculado ao paciente SUS (CNS quando disponível).
        3. Lançamento: seleção de procedimento SIGTAP com quantidade e profissional/CBO.
        4. Consolidação: BPA mensal, APAC por autorização ou AIH por internação.
        5. Crítica: validações de compatibilidade idade/sexo, CID obrigatório e habilitação do estabelecimento.
        6. Exportação: arquivo no layout exigido pelo sistema municipal (SIA/SUS) ou estadual.
        7. Fechamento: competência fechada não deve receber lançamentos retroativos sem perfil de administrador.
        """;

    // ── Financeiro ──
    public const string FinContasPagarFaq =
        """
        Contas a pagar controlam obrigações da instituição (fornecedores, serviços, impostos).

        Para lançar: Financeiro → Contas a Pagar → Inserir. Informe fornecedor cadastrado, documento fiscal (NF), data de emissão e vencimento, valor bruto, retenções se houver, centro de custo e plano de contas.

        Fluxo de aprovação: conforme alçada configurada, títulos podem exigir aprovação de gestor antes de programação de pagamento. Títulos vencidos aparecem no dashboard e em relatórios de inadimplência interna.

        Conciliação: ao pagar, vincule ao movimento bancário ou sessão de caixa para rastreabilidade completa na auditoria.
        """;

    public const string FinCaixaFaq =
        """
        Sessões de caixa isolam movimentos por operador e turno.

        Abertura: informe saldo inicial conferido (dinheiro em espécie). O sistema bloqueia duas sessões abertas para o mesmo caixa.

        Durante o turno: registre recebimentos (dinheiro, cartão, PIX), sangrias e suprimentos. Particulares e coparticipações frequentemente passam pelo caixa da recepção.

        Fechamento: conte físico, informe saldo final; divergências exigem justificativa. Relatório de fechamento fica disponível para controladoria e pode ser exportado.

        Nunca compartilhe usuário de caixa — cada operador deve usar credencial própria (LGPD e SOX).
        """;

    public const string FinFluxoReceberArtigo =
        """
        Contas a receber e repasses médicos

        Receita de convênios: demonstrativos TISS pagos geram baixa em contas a receber vinculadas às guias. Divergências permanecem em aberto até recurso ou ajuste.

        Particulares: recebimento no caixa ou PIX baixa automaticamente a conta do atendimento.

        Repasses: módulo Honorários aplica percentuais por profissional, desconta taxas e gera repasse periódico. Conferência cruzada com produção (guias pagas) evita pagamento em duplicidade.

        Indicadores no dashboard: receita do dia, receita do mês e valores a receber em atraso.
        """;

    // ── Agenda ──
    public const string AgendaEncaixeFaq =
        """
        Encaixe é agendamento em horário não previsto na grade original — uso deve ser exceção e, em muitas unidades, exige permissão específica.

        Na agenda diária, clique em slot livre ou use a ação Encaixe sobre horário ocupado (dobra) quando a política permitir. Informe paciente, tipo de atendimento, profissional e motivo.

        Encaixes impactam tempo médio de consulta e indicadores de pontualidade — monitore em Relatórios → Agenda. Para demanda recorrente, prefira ampliar grade ou usar agenda múltipla.
        """;

    public const string AgendaVisoesArtigo =
        """
        Visões disponíveis no módulo Agenda (padrão Feegow)

        Diária: grade hora a hora por profissional ou sala — ideal para recepção e médicos.
        Semanal: visão de sete dias para planejamento de equipe.
        Múltipla: vários profissionais lado a lado no mesmo dia.
        Mapa de agenda: visão espacial por consultório/sala física.
        Check-in e Confirmar: fluxos em lote para reduzir no-show.

        Filtros: unidade de atendimento (local), especialidade, equipamento alocado. A sidebar contextual da agenda exibe bloco de local quando aplicável.
        """;

    // ── Internação ──
    public const string IntSolicitacaoFaq =
        """
        Solicitação de internação registra demanda de leito com prioridade clínica.

        Campos principais: paciente, CID principal, origem (PS, ambulatorial, transferência), tipo de leito (clínico, cirúrgico, UTI), isolamento se necessário e médico responsável.

        A fila de internação ordena por prioridade e tempo de espera. Reguladores podem aceitar, negar ou solicitar complementação. Integração com mapa de leitos evita alocação em leito indisponível ou em higienização.
        """;

    public const string IntFluxoArtigo =
        """
        Ciclo de internação hospitalar

        Solicitação → Regulação → Alocação de leito → Admissão na enfermaria → Evolução diária (médica e de enfermagem) → Prescrição e dispensação por ala → Procedimentos e exames → Alta hospitalar ou óbito → Limpeza e liberação do leito → Faturamento AIH (SUS) ou guia de internação (convênio).

        Transferências entre leitos/alas registram histórico para auditoria e cálculo de permanência. Altas administrativas pendentes bloqueiam ocupação fictícia do leito.
        """;

    // ── Estoque ──
    public const string EstRequisicaoFaq =
        """
        Requisições movimentam material entre almoxarifado central, farmácias de ala e setores consumidores.

        Crie requisição informando setor solicitante e itens com quantidade. Fluxo típico: Rascunho → Enviada → Aprovada → Separada → Entregue. Cada etapa pode exigir perfil diferente.

        Estoque negativo é bloqueado por padrão. Lotes e validades são rastreados quando o produto exige controle — priorize FEFO (primeiro a vencer) na separação.
        """;

    public const string EstFarmaciaAlaArtigo =
        """
        Farmácia por ala

        Cada ala/enfermaria pode manter saldo de medicamentos de uso frequente, separado do almoxarifado central.

        Movimentações: entrada por requisição aprovada, dispensação vinculada à prescrição do paciente, devolução de dose não administrada e ajuste de inventário cíclico.

        Integração com PEP: dispensação registra hora, profissional e paciente para rastreio em auditoria de enfermagem. Itens controlados exigem dupla conferência conforme política institucional.
        """;

    // ── Relatórios ──
    public const string RelExportFaq =
        """
        Relatórios operacionais e gerenciais estão em Relatórios no menu superior.

        Selecione o relatório desejado, defina período (data inicial/final), unidade, profissional ou operadora conforme filtros disponíveis. Use Exportar para CSV (análise em planilha) ou PDF (impressão/arquivo).

        Relatórios essenciais marcados no catálogo têm implementação completa; demais podem aparecer como placeholder em expansão contínua. BI avançado disponível em /bi para cruzamentos multidimensionais.
        """;

    public const string RelProducaoArtigo =
        """
        Produção assistencial e faturamento

        Relatórios de produção analítica cruzam atendimentos, procedimentos TUSS/SIGTAP, profissional e convênio. Use para:
        • Fechamento de produção médica antes de repasse.
        • Conferência de guias enviadas vs. atendimentos registrados.
        • Identificação de procedimentos sem guia (perda de receita).

        Filtros por competência alinham com ciclo de faturamento SUS e fechamento TISS mensal. Exporte CSV para conferência com planilhas legadas se necessário.
        """;

    // ── Configurações ──
    public const string CfgParametrosFaq =
        """
        Parâmetros institucionais definem comportamento global do sistema.

        Em Configurações → Parâmetros: nome da instituição (exibido no topo Feegow), módulos habilitados, versão TISS padrão, políticas de sessão e timeout.

        Alterações afetam todos os usuários após recarregar a página. Documente mudanças críticas (ex.: troca de versão TISS) em comunicado interno via Connect → Mural.
        """;

    public const string CfgUsuariosFaq =
        """
        Gestão de identidade e acesso (RBAC)

        Usuários: cadastro com e-mail institucional, perfil (role) e status ativo/bloqueado.
        Permissões: granulares por módulo (patients.read, tiss.write, connect.approve, etc.).
        Boas práticas: princípio do menor privilégio, revisão trimestral de acessos, desativação no desligamento no mesmo dia.

        Administradores não devem compartilhar senha. Autenticação suporta bloqueio após tentativas inválidas (ver logs em Auditoria).
        """;

    public const string CfgIntegracoesArtigo =
        """
        Integrações oficiais e de terceiros

        • SIGTAP / TUSS: atualização de tabelas nacionais.
        • WhatsApp Connect (Meta): webhook, templates, opt-out LGPD.
        • Laboratório / HL7: pedidos e resultados (módulo Integrações).
        • Atualizações oficiais: painel de versões de portarias e tabelas.

        Cada integração exibe status em Configurações → APIs e Webhooks. Falhas consecutivas aparecem no dashboard como alerta de integração.
        """;

    // ── Connect ──
    public const string ConnChamadoFaq =
        """
        Chamados de suporte substituem e-mail informal para incidentes e dúvidas técnicas.

        Abra em Connect → Chamados ou Ajuda → Suporte. Informe título objetivo, descrição com passos para reproduzir, categoria (TI, Engenharia Clínica, Compras, etc.) e prioridade.

        Você recebe número de protocolo. Acompanhe status: Aberto → Em andamento → Aguardando (sua resposta) → Resolvido. Comentários mantêm histórico auditável. SLA crítico dispara alerta no Connect.
        """;

    public const string ConnWhatsappArtigo =
        """
        WhatsApp oficial (Meta Business Platform)

        Configuração: token de longa duração, Phone Number ID, webhook com verificação de assinatura e secret configurado em variáveis de ambiente.

        Templates: apenas mensagens aprovadas pela Meta para início de conversa. Respostas dentro da janela de 24h podem ser livres conforme política.

        LGPD: registre opt-out; o sistema bloqueia envios a números que solicitaram cancelamento. Auditoria registra envios, entregas e falhas.

        Painel Connect → WhatsApp exibe saúde da integração e fila de mensagens.
        """;

    // ── Geral ──
    public const string GeralSobreFaq =
        """
        IASGH / APSMed Hospitalar é a plataforma integrada de gestão para hospitais e clínicas de médio e grande porte, com interface inspirada no Feegow Clinic.

        Módulos: recepção e pacientes, agenda, PEP, internação, centro cirúrgico, laboratório, imagem, estoque e farmácia, faturamento TISS e SUS, financeiro, RH, comunicação Connect, relatórios e configurações.

        Arquitetura web moderna (API .NET + PostgreSQL + frontend React), deploy via Docker, atualizações de tabelas oficiais (SIGTAP/TUSS) integradas.
        """;

    public const string GeralLgpdFaq =
        """
        Proteção de dados e LGPD

        Dados sensíveis de saúde são tratados conforme base legal de tutela da saúde e consentimentos registrados no módulo de consentimento.

        Controles: perfil de acesso, trilha de auditoria, mascaramento em logs, direito do titular via canal LGPD (módulo Segurança/LGPD).

        Proibido: exportar planilhas com PHI para uso pessoal, compartilhar credenciais, acessar prontuário sem necessidade de serviço. Violações devem ser reportadas ao DPO.
        """;

    public const string GeralModulosArtigo =
        """
        Mapa de módulos — navegação Feegow

        Menu superior: Agenda, Espera (PS), Pacientes, Estoque, Financeiro, Faturamento, Relatórios, Guias, Comunicação (Connect). Ícones ? (Ajuda) e ⚙ (Configurações) à direita.

        Sidebar contextual muda conforme módulo ativo (ex.: financeiro exibe contas a pagar/receber, caixas).

        Dashboard inicial: indicadores operacionais em tempo real. Use Ajuda contextual (? na página) para artigos ligados à tela atual.

        Portal do paciente: rota separada para usuários com perfil Patient.
        """;
}
