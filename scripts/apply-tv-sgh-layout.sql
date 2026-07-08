-- Cria layout SGH e associa às salas de espera (idempotente)
INSERT INTO tv_layouts ("Id", "Name", "Description", "ZonesJson", "IsSystem", "CreatedAt", "IsActive")
SELECT
    gen_random_uuid(),
    'Sala de Espera — SGH',
    'Layout institucional SGH — chamadas, publicidade e comunicados.',
    '[{"id":"sgh-template","widget":2,"x":0,"y":0,"w":100,"h":100}]',
    true,
    NOW() AT TIME ZONE 'UTC',
    true
WHERE NOT EXISTS (
    SELECT 1 FROM tv_layouts WHERE "Name" = 'Sala de Espera — SGH'
);

UPDATE tv_layouts
SET "ZonesJson" = '[{"id":"sgh-template","widget":2,"x":0,"y":0,"w":100,"h":100}]',
    "Description" = 'Layout institucional SGH — chamadas, publicidade e comunicados.',
    "IsSystem" = true
WHERE "Name" = 'Sala de Espera — SGH';

UPDATE tv_displays d
SET "LayoutId" = l."Id"
FROM tv_layouts l
WHERE l."Name" = 'Sala de Espera — SGH'
  AND d."IsActive" = true
  AND (
    d."Slug" IN ('recepcao', 'ambulatorio', 'laboratorio', 'sala-espera', 'espera', 'sala-de-casa', 'sala-casa')
    OR LOWER(COALESCE(d."Name", '')) ~ '(recep|espera|ambulat|sala|casa)'
    OR LOWER(COALESCE(d."Sector", '')) ~ '(recep|espera|ambulat|sala|casa)'
    OR d."Slug" ~ '^\d{1,3}(\.\d{1,3}){3}$'
  );
