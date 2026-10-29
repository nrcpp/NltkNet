#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
output="$root/.artifacts/nltk-data"
mkdir -p "$output"

fetch() {
    local category="$1" name="$2" checksum="$3"
    mkdir -p "$output/$category"
    local archive="$output/$category/$name.zip"
    curl --fail --location --retry 3 \
        "https://raw.githubusercontent.com/nltk/nltk_data/gh-pages/packages/$category/$name.zip" \
        --output "$archive"
    if command -v sha256sum >/dev/null 2>&1; then
        actual="$(sha256sum "$archive" | awk '{print $1}')"
    else
        actual="$(shasum -a 256 "$archive" | awk '{print $1}')"
    fi
    if [[ "$actual" != "$checksum" ]]; then
        echo "SHA-256 mismatch: $archive" >&2
        exit 1
    fi
    unzip -oq "$archive" -d "$output/$category"
    rm "$archive"
    chmod -R go-w "$output/$category/$name"
}

# Development/test data only. Review each dataset's redistribution terms before packaging it.
fetch tokenizers punkt_tab e57f64187974277726a3417ca6f181ec5403676c717672eef6a748a7b20e0106
fetch taggers averaged_perceptron_tagger_eng 6025f530624335c67d6547d44757b357b4e79bae030a0383e9887a92c1718f0b
fetch chunkers maxent_ne_chunker_tab 1370234c7770045d0c50f41e08bc627ec92450324a946de14b93cd7d5e362a86
fetch corpora words 54ed02917d6771dcc3e8141218960d020947f7f2ccfd9ac9b320979349746015
fetch corpora wordnet cbda5ea6eef7f36a97a43d4a75f85e07fccbb4f23657d27b4ccbc93e2646ab59
echo "Development data prepared in $output"
