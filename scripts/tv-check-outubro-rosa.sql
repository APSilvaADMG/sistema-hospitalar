SELECT m."Title", m."MediaType", m."StoragePath", m."Sector", m."IsActive", m."Priority"
FROM tv_midias m
WHERE m."Title" LIKE '%Outubro%' OR m."IsActive" = true
ORDER BY m."Priority" DESC;

SELECT c."Name", cm."SortOrder", m."Title"
FROM tv_campanhas c
JOIN tv_campanha_midias cm ON cm."CampaignId" = c."Id"
JOIN tv_midias m ON m."Id" = cm."MediaId"
WHERE c."IsActive" = true
ORDER BY cm."SortOrder";
