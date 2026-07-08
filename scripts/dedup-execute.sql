-- Post-migration dedup (idempotent)
DELETE FROM tuss_catalogs
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "Code", "TableType"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         LENGTH("Description") DESC,
                         "Id"
            ) AS rn
        FROM tuss_catalogs
    ) ranked WHERE rn > 1);

DELETE FROM sigtap_procedures
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "Code", "Competence"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         LENGTH("Description") DESC,
                         "Id"
            ) AS rn
        FROM sigtap_procedures
    ) ranked WHERE rn > 1);

DELETE FROM cbhpm_procedures
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "Code"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         LENGTH("Description") DESC,
                         "Id"
            ) AS rn
        FROM cbhpm_procedures
    ) ranked WHERE rn > 1);

DELETE FROM brasindice_items
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "Code"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         LENGTH("Description") DESC,
                         "Id"
            ) AS rn
        FROM brasindice_items
    ) ranked WHERE rn > 1);

DELETE FROM simpro_items
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "Code"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         LENGTH("Description") DESC,
                         "Id"
            ) AS rn
        FROM simpro_items
    ) ranked WHERE rn > 1);

DELETE FROM sus_guides
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "GuideNumber"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         "Id"
            ) AS rn
        FROM sus_guides
    ) ranked WHERE rn > 1);

DELETE FROM service_units
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "Code"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         "Id"
            ) AS rn
        FROM service_units
    ) ranked WHERE rn > 1);

DELETE FROM patient_reference_catalog
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "CatalogType", "Code"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         "Id"
            ) AS rn
        FROM patient_reference_catalog
    ) ranked WHERE rn > 1);

DELETE FROM medication_catalogs
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "ExternalBulaSlug"
                ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC,
                         LENGTH("Name") DESC,
                         "Id"
            ) AS rn
        FROM medication_catalogs
        WHERE "ExternalBulaSlug" IS NOT NULL
    ) ranked WHERE rn > 1);
