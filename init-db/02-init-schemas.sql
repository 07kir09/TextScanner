\c file_storage
CREATE SCHEMA IF NOT EXISTS storage;
CREATE TABLE IF NOT EXISTS storage.files (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    location TEXT NOT NULL
);

\c file_analysis
CREATE SCHEMA IF NOT EXISTS analysis;
CREATE TABLE IF NOT EXISTS analysis.analysis_results (
    id UUID PRIMARY KEY,
    file_id UUID NOT NULL,
    paragraph_count INT NOT NULL,
    word_count INT NOT NULL,
    character_count INT NOT NULL,
    word_cloud_location TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS analysis.plagiarism_checks (
    id UUID PRIMARY KEY,
    file_id UUID NOT NULL,
    hash TEXT NOT NULL,
    is_plagiarized BOOLEAN NOT NULL DEFAULT FALSE,
    similar_file_id UUID NULL
);
CREATE INDEX IF NOT EXISTS idx_plagiarism_checks_hash ON analysis.plagiarism_checks(hash);