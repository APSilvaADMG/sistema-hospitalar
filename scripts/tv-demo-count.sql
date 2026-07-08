SELECT 'midias' AS tipo, COUNT(*)::text AS qtd FROM tv_midias WHERE "IsActive"
UNION ALL SELECT 'avisos', COUNT(*)::text FROM tv_avisos WHERE "IsActive"
UNION ALL SELECT 'noticias', COUNT(*)::text FROM tv_noticias WHERE "IsActive"
UNION ALL SELECT 'chamadas', COUNT(*)::text FROM tv_chamadas WHERE "IsActive"
UNION ALL SELECT 'campanhas', COUNT(*)::text FROM tv_campanhas WHERE "IsActive";
