-- MIMIC-III sandbox staging schema (mimic_iii database only — NEVER sistema_hospitalar)
-- Run as owner of the isolated research database.

CREATE SCHEMA IF NOT EXISTS mimic_staging;

CREATE TABLE IF NOT EXISTS mimic_staging.etl_run (
    id              SERIAL PRIMARY KEY,
    started_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at    TIMESTAMPTZ,
    status          TEXT NOT NULL DEFAULT 'running',
    phase           TEXT,
    source_path     TEXT,
    rows_processed  BIGINT NOT NULL DEFAULT 0,
    error_message   TEXT
);

CREATE TABLE IF NOT EXISTS mimic_staging.chartevents_vitals_raw (
    subject_id   INTEGER NOT NULL,
    hadm_id      INTEGER,
    icustay_id   INTEGER,
    itemid       INTEGER NOT NULL,
    charttime    TIMESTAMPTZ NOT NULL,
    valuenum     NUMERIC,
    etl_run_id   INTEGER REFERENCES mimic_staging.etl_run(id)
);

CREATE INDEX IF NOT EXISTS idx_cevr_subject ON mimic_staging.chartevents_vitals_raw(subject_id);
CREATE INDEX IF NOT EXISTS idx_cevr_icustay ON mimic_staging.chartevents_vitals_raw(icustay_id);
CREATE INDEX IF NOT EXISTS idx_cevr_charttime ON mimic_staging.chartevents_vitals_raw(charttime);

-- Wide-format snapshots aligned with VitalSignRecord (research sandbox only).
CREATE TABLE IF NOT EXISTS mimic_staging.vital_sign_snapshot (
    id               BIGSERIAL PRIMARY KEY,
    subject_id       INTEGER NOT NULL,
    hadm_id          INTEGER,
    icustay_id       INTEGER,
    recorded_at      TIMESTAMPTZ NOT NULL,
    heart_rate       SMALLINT,
    systolic_bp      SMALLINT,
    diastolic_bp     SMALLINT,
    spo2             SMALLINT,
    respiratory_rate SMALLINT,
    temperature_c    NUMERIC(4, 1),
    source           TEXT NOT NULL DEFAULT 'MIMIC_CHARTEVENTS',
    etl_run_id       INTEGER REFERENCES mimic_staging.etl_run(id)
);

CREATE INDEX IF NOT EXISTS idx_vss_subject ON mimic_staging.vital_sign_snapshot(subject_id);
CREATE INDEX IF NOT EXISTS idx_vss_icustay ON mimic_staging.vital_sign_snapshot(icustay_id);
CREATE INDEX IF NOT EXISTS idx_vss_recorded ON mimic_staging.vital_sign_snapshot(recorded_at);
