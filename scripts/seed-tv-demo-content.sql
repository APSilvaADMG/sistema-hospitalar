-- Conteúdo de demonstração TV Corporativa (hospital SGH) — idempotente
-- Executar: psql -U postgres -d sistema_hospitalar -f scripts/seed-tv-demo-content.sql

-- Layout SGH nas salas de espera
\ir apply-tv-sgh-layout.sql

-- TV Sala de Casa (192.168.3.33)
INSERT INTO tv_displays (
    "Id", "Name", "Slug", "Sector", "IpAddress", "Orientation", "Status",
    "PlayerToken", "LayoutId", "ShowPatientName", "EnableSound", "CallDisplaySeconds",
    "WeatherCity", "CreatedAt", "IsActive"
)
SELECT
    gen_random_uuid(),
    'TV Sala de Casa',
    'sala-de-casa',
    'Sala de Casa',
    '192.168.3.33',
    1,
    2,
    encode(gen_random_bytes(16), 'hex'),
    l."Id",
    false,
    true,
    30,
    'Arapiraca - AL',
    NOW() AT TIME ZONE 'UTC',
    true
FROM tv_layouts l
WHERE l."Name" = 'Sala de Espera — SGH'
  AND NOT EXISTS (SELECT 1 FROM tv_displays WHERE "Slug" = 'sala-de-casa');

UPDATE tv_displays d
SET "IpAddress" = '192.168.3.33',
    "Sector" = COALESCE(d."Sector", 'Sala de Casa'),
    "LayoutId" = l."Id"
FROM tv_layouts l
WHERE l."Name" = 'Sala de Espera — SGH'
  AND d."Slug" = 'sala-de-casa';

UPDATE tv_displays d
SET "LayoutId" = l."Id",
    "Sector" = COALESCE(d."Sector", 'Sala de Casa'),
    "IpAddress" = COALESCE(d."IpAddress", '192.168.3.33')
FROM tv_layouts l
WHERE l."Name" = 'Sala de Espera — SGH'
  AND (d."Slug" = '192.168.3.33' OR d."IpAddress" = '192.168.3.33');

-- Mídias / publicidade
INSERT INTO tv_midias ("Id", "Title", "MediaType", "StoragePath", "MimeType", "Sector", "StartsAt", "EndsAt", "Priority", "DurationSeconds", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), v."Title", v."MediaType", v."StoragePath", v."MimeType", v."Sector",
       NOW() AT TIME ZONE 'UTC' - INTERVAL '30 days',
       NOW() AT TIME ZONE 'UTC' + INTERVAL '180 days',
       v."Priority", v."DurationSeconds", NOW() AT TIME ZONE 'UTC', true
FROM (VALUES
    ('Campanha Outubro Rosa', 1, 'tv-demo/outubro-rosa.svg', 'image/svg+xml', NULL::varchar, 20, 12),
    ('Vacinação Influenza 2026', 1, 'tv-demo/vacinacao.svg', 'image/svg+xml', 'Recepção', 18, 12),
    ('Check-up Executivo SGH', 1, 'tv-demo/checkup.svg', 'image/svg+xml', NULL::varchar, 15, 10),
    ('Humanização no Atendimento', 1, 'tv-demo/humanizacao.svg', 'image/svg+xml', NULL::varchar, 12, 10),
    ('Laboratório — orientações de jejum', 1, 'tv-demo/laboratorio-jejum.svg', 'image/svg+xml', 'Laboratório', 16, 12)
) AS v("Title", "MediaType", "StoragePath", "MimeType", "Sector", "Priority", "DurationSeconds")
WHERE NOT EXISTS (SELECT 1 FROM tv_midias m WHERE m."Title" = v."Title");

UPDATE tv_midias m
SET "StoragePath" = v."StoragePath", "MimeType" = v."MimeType", "Sector" = v."Sector",
    "Priority" = v."Priority", "DurationSeconds" = v."DurationSeconds", "IsActive" = true
FROM (VALUES
    ('Campanha Outubro Rosa', 'tv-demo/outubro-rosa.svg', 'image/svg+xml', NULL::varchar, 20, 12),
    ('Vacinação Influenza 2026', 'tv-demo/vacinacao.svg', 'image/svg+xml', 'Recepção', 18, 12),
    ('Check-up Executivo SGH', 'tv-demo/checkup.svg', 'image/svg+xml', NULL::varchar, 15, 10),
    ('Humanização no Atendimento', 'tv-demo/humanizacao.svg', 'image/svg+xml', NULL::varchar, 12, 10),
    ('Laboratório — orientações de jejum', 'tv-demo/laboratorio-jejum.svg', 'image/svg+xml', 'Laboratório', 16, 12)
) AS v("Title", "StoragePath", "MimeType", "Sector", "Priority", "DurationSeconds")
WHERE m."Title" = v."Title";

-- Campanha com mídias (sempre ativa para demo)
INSERT INTO tv_campanhas ("Id", "Name", "Sector", "StartsAt", "EndsAt", "DailyStart", "DailyEnd", "DaysOfWeek", "Priority", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'Campanha institucional — horário comercial', NULL,
       (NOW() AT TIME ZONE 'UTC')::date - 30,
       (NOW() AT TIME ZONE 'UTC')::date + 180,
       TIME '00:00', TIME '23:59', '0,1,2,3,4,5,6', 10,
       NOW() AT TIME ZONE 'UTC', true
WHERE NOT EXISTS (
    SELECT 1 FROM tv_campanhas WHERE "Name" = 'Campanha institucional — horário comercial'
);

UPDATE tv_campanhas
SET "EndsAt" = (NOW() AT TIME ZONE 'UTC')::date + 180,
    "DailyStart" = TIME '00:00',
    "DailyEnd" = TIME '23:59',
    "DaysOfWeek" = '0,1,2,3,4,5,6'
WHERE "Name" = 'Campanha institucional — horário comercial';

INSERT INTO tv_campanha_midias ("Id", "CampaignId", "MediaId", "SortOrder", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), c."Id", m."Id", row_number() OVER (ORDER BY m."Priority" DESC) - 1,
       NOW() AT TIME ZONE 'UTC', true
FROM tv_campanhas c
CROSS JOIN tv_midias m
WHERE c."Name" = 'Campanha institucional — horário comercial'
  AND m."Title" IN (
    'Campanha Outubro Rosa', 'Vacinação Influenza 2026', 'Check-up Executivo SGH',
    'Humanização no Atendimento', 'Laboratório — orientações de jejum'
  )
  AND NOT EXISTS (
    SELECT 1 FROM tv_campanha_midias cm
    WHERE cm."CampaignId" = c."Id" AND cm."MediaId" = m."Id"
  );

-- Comunicados
INSERT INTO tv_avisos ("Id", "Title", "Body", "Sector", "StartsAt", "EndsAt", "Priority", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), v."Title", v."Body", v."Sector",
       NOW() AT TIME ZONE 'UTC' - INTERVAL '7 days',
       NOW() AT TIME ZONE 'UTC' + INTERVAL '90 days',
       v."Priority", NOW() AT TIME ZONE 'UTC', true
FROM (VALUES
    ('Documentos para atendimento', 'Apresente RG, CPF, carteirinha do convênio e pedidos médicos na recepção.', 'Recepção', 10),
    ('Uso obrigatório de máscara', 'Em áreas de maior fluxo, utilize máscara facial. Dispenser na entrada principal.', NULL::varchar, 8),
    ('Ambulatório — retirada de exames', 'Resultados disponíveis no portal do paciente ou balcão do laboratório após 48h.', 'Ambulatório', 7),
    ('Laboratório — coleta por ordem de chegada', 'Senhas liberadas às 6h30. Prioridade conforme legislação vigente.', 'Laboratório', 9),
    ('Sala de Casa — visitas', 'Horário de visitas: 14h às 17h. Máximo 2 acompanhantes por paciente.', 'Sala de Casa', 6),
    ('Wi-Fi para acompanhantes', 'Rede SGH-Visitantes disponível na recepção. Senha no balcão de informações.', NULL::varchar, 5)
) AS v("Title", "Body", "Sector", "Priority")
WHERE NOT EXISTS (SELECT 1 FROM tv_avisos a WHERE a."Title" = v."Title");

-- Notícias / ticker
INSERT INTO tv_noticias ("Id", "Title", "Summary", "Sector", "PublishedAt", "ExpiresAt", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), v."Title", v."Summary", v."Sector",
       NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
       NOW() AT TIME ZONE 'UTC' + INTERVAL '90 days',
       NOW() AT TIME ZONE 'UTC', true
FROM (VALUES
    ('Horário ampliado no Ambulatório', 'Atendimento estendido até 19h de segunda a quinta.', 'Ambulatório'),
    ('Novo tomógrafo em operação', 'Exames de imagem com menor tempo de espera.', NULL::varchar),
    ('Campanha de doação de sangue', 'Doe sangue — estoque em alerta. Hemocentro no 2º andar.', 'Recepção'),
    ('Higienização das mãos', 'Lembrete: higienize as mãos ao entrar nas áreas assistenciais.', NULL::varchar),
    ('Programa de humanização', 'Equipe SGH reforça acolhimento e escuta ativa.', NULL::varchar)
) AS v("Title", "Summary", "Sector")
WHERE NOT EXISTS (SELECT 1 FROM tv_noticias n WHERE n."Title" = v."Title");

-- Chamadas de senha (atualiza timestamps a cada execução)
INSERT INTO tv_chamadas ("Id", "DisplayId", "TicketNumber", "PatientName", "Destination", "Sector", "CalledAt", "DisplaySeconds", "ShowPatientName", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), d."Id", v."Ticket", v."Patient", v."Destination", v."Sector",
       NOW() AT TIME ZONE 'UTC' - (v."SecondsAgo" || ' seconds')::interval,
       30, v."ShowName", NOW() AT TIME ZONE 'UTC', true
FROM (VALUES
    ('R048', NULL::varchar, 'Guichê 3', 'Recepção', 'recepcao', 8, false),
    ('R047', NULL, 'Guichê 2', 'Recepção', 'recepcao', 180, false),
    ('R046', 'Maria S.', 'Guichê 1', 'Recepção', 'recepcao', 480, true),
    ('R045', NULL, 'Guichê 4', 'Recepção', NULL, 900, false),
    ('A023', NULL, 'Sala Consultório 102', 'Ambulatório', 'ambulatorio', 300, false),
    ('A022', 'João P.', 'Sala Consultório 101', 'Ambulatório', 'ambulatorio', 720, true),
    ('A021', NULL, 'Guichê Ambulatório', 'Ambulatório', 'ambulatorio', 1200, false),
    ('L015', NULL, 'Guichê Laboratório', 'Laboratório', 'laboratorio', 240, false),
    ('L014', 'Ana C.', 'Sala Coleta 2', 'Laboratório', 'laboratorio', 600, true),
    ('C003', NULL, 'Sala de Casa — Enfermaria', 'Sala de Casa', 'sala-de-casa', 420, false),
    ('C002', 'Pedro M.', 'Guichê Recepção Casa', 'Sala de Casa', 'sala-de-casa', 960, true),
    ('C004', NULL, 'Sala Enfermaria 12', 'Sala de Casa', '192.168.3.33', 360, false)
) AS v("Ticket", "Patient", "Destination", "Sector", "DisplaySlug", "SecondsAgo", "ShowName")
LEFT JOIN tv_displays d ON d."Slug" = v."DisplaySlug"
WHERE NOT EXISTS (
    SELECT 1 FROM tv_chamadas c
    WHERE c."TicketNumber" = v."Ticket" AND c."Destination" = v."Destination"
);

UPDATE tv_chamadas c
SET "CalledAt" = NOW() AT TIME ZONE 'UTC' - (v."SecondsAgo" || ' seconds')::interval,
    "DisplayId" = d."Id",
    "PatientName" = v."Patient",
    "Sector" = v."Sector",
    "ShowPatientName" = v."ShowName",
    "IsActive" = true
FROM (VALUES
    ('R048', NULL::varchar, 'Guichê 3', 'Recepção', 'recepcao', 8, false),
    ('R047', NULL, 'Guichê 2', 'Recepção', 'recepcao', 180, false),
    ('R046', 'Maria S.', 'Guichê 1', 'Recepção', 'recepcao', 480, true),
    ('R045', NULL, 'Guichê 4', 'Recepção', NULL, 900, false),
    ('A023', NULL, 'Sala Consultório 102', 'Ambulatório', 'ambulatorio', 300, false),
    ('A022', 'João P.', 'Sala Consultório 101', 'Ambulatório', 'ambulatorio', 720, true),
    ('A021', NULL, 'Guichê Ambulatório', 'Ambulatório', 'ambulatorio', 1200, false),
    ('L015', NULL, 'Guichê Laboratório', 'Laboratório', 'laboratorio', 240, false),
    ('L014', 'Ana C.', 'Sala Coleta 2', 'Laboratório', 'laboratorio', 600, true),
    ('C003', NULL, 'Sala de Casa — Enfermaria', 'Sala de Casa', 'sala-de-casa', 420, false),
    ('C002', 'Pedro M.', 'Guichê Recepção Casa', 'Sala de Casa', 'sala-de-casa', 960, true),
    ('C004', NULL, 'Sala Enfermaria 12', 'Sala de Casa', '192.168.3.33', 360, false)
) AS v("Ticket", "Patient", "Destination", "Sector", "DisplaySlug", "SecondsAgo", "ShowName")
LEFT JOIN tv_displays d ON d."Slug" = v."DisplaySlug"
WHERE c."TicketNumber" = v."Ticket" AND c."Destination" = v."Destination";
