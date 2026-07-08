-- Pivot filtered CHARTEVENTS vitals into wide snapshots (mimic_staging only).
-- ITEMIDs from table-mapping.json (MIMIC-III v1.4).

-- Parameters: replace :etl_run_id with the active run id before execution.

TRUNCATE mimic_staging.vital_sign_snapshot;

INSERT INTO mimic_staging.vital_sign_snapshot (
    subject_id,
    hadm_id,
    icustay_id,
    recorded_at,
    heart_rate,
    systolic_bp,
    diastolic_bp,
    spo2,
    respiratory_rate,
    temperature_c,
    source,
    etl_run_id
)
SELECT
    subject_id,
    MAX(hadm_id) AS hadm_id,
    icustay_id,
    charttime AS recorded_at,
    MAX(CASE WHEN itemid = 220045 THEN valuenum::smallint END) AS heart_rate,
    MAX(CASE WHEN itemid = 220179 THEN valuenum::smallint END) AS systolic_bp,
    MAX(CASE WHEN itemid = 220180 THEN valuenum::smallint END) AS diastolic_bp,
    MAX(CASE WHEN itemid = 220277 THEN valuenum::smallint END) AS spo2,
    MAX(CASE WHEN itemid = 220210 THEN valuenum::smallint END) AS respiratory_rate,
    MAX(CASE WHEN itemid = 223761 THEN ROUND(valuenum, 1) END) AS temperature_c,
    'MIMIC_CHARTEVENTS',
    :etl_run_id
FROM mimic_staging.chartevents_vitals_raw
WHERE valuenum IS NOT NULL
GROUP BY subject_id, icustay_id, charttime;
