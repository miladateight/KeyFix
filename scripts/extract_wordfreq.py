"""
Extract word frequency lists from multiple sources and combine them.
Generates expanded word lists for KeyFix (English, German, Arabic, Persian).

Sources:
  - wordfreq (rspeer/wordfreq): large lists for en/de/ar, small for fa
  - hermitdave/FrequencyWords: 50k lists for all 4 languages
  - behnam/persian-words-frequency: 406k Persian Wikipedia words

Usage:
    python scripts/extract_wordfreq.py [--target 30000]
"""
import argparse
import os
import sys
import urllib.request
from pathlib import Path

try:
    from wordfreq import get_frequency_dict
except ImportError:
    print("wordfreq not installed. Run: pip install wordfreq")
    sys.exit(1)


def filter_english(word):
    if len(word) < 2:
        return False
    for ch in word:
        code = ord(ch)
        if not ((65 <= code <= 90) or (97 <= code <= 122) or code == 39):
            return False
    return True


def filter_german(word):
    if len(word) < 2:
        return False
    for ch in word:
        code = ord(ch)
        is_latin = (65 <= code <= 90) or (97 <= code <= 122)
        is_umlaut = code in (0x00E4, 0x00F6, 0x00FC, 0x00DF, 0x00C4, 0x00D6, 0x00DC)
        is_apos = code == 39
        if not (is_latin or is_umlaut or is_apos):
            return False
    return True


def filter_persian(word):
    if len(word) < 2:
        return False
    for ch in word:
        code = ord(ch)
        if code < 0x0600 or code > 0x06FF:
            if code != 0x200C:
                return False
    return True


def filter_arabic(word):
    if len(word) < 2:
        return False
    has_arabic = False
    for ch in word:
        code = ord(ch)
        if 0x0600 <= code <= 0x06FF or 0x0750 <= code <= 0x077F or 0x08A0 <= code <= 0x08FF:
            has_arabic = True
            break
    if not has_arabic:
        return False
    persian_only = {0x067E, 0x0686, 0x0698, 0x06AF, 0x06A9, 0x06CC}
    for ch in word:
        if ord(ch) in persian_only:
            return False
    return True


FILTERS = {"en": filter_english, "de": filter_german, "fa": filter_persian, "ar": filter_arabic}
WORDLIST_MODE = {"en": "large", "de": "large", "ar": "large", "fa": "small"}
OUTPUT_FILES = {"en": "words-en.txt", "de": "words-de.txt", "fa": "words-fa.txt", "ar": "words-ar.txt"}

HERMITDAVE_BASE = "https://raw.githubusercontent.com/hermitdave/FrequencyWords/master/content"
PERSIAN_WIKI_URL = "https://raw.githubusercontent.com/behnam/persian-words-frequency/master/persian-wikipedia.txt"


def load_existing_words(filepath):
    words = []
    seen = set()
    if os.path.exists(filepath):
        with open(filepath, "r", encoding="utf-8") as f:
            for line in f:
                w = line.strip()
                if w and w not in seen:
                    words.append(w)
                    seen.add(w)
    return words


def download_hermitdave(lang_code):
    """Download and parse hermitdave frequency lists (2018 + 2016)."""
    words = []
    for year in (2018, 2016):
        url = f"{HERMITDAVE_BASE}/{year}/{lang_code}/{lang_code}_50k.txt"
        try:
            data = urllib.request.urlopen(url, timeout=30).read().decode("utf-8")
            for line in data.strip().split("\n"):
                if line.strip():
                    word = line.split()[0].strip()
                    if FILTERS[lang_code](word):
                        words.append(word)
        except Exception as e:
            print(f"    Warning: could not download {url}: {e}")
    return words


def download_persian_wiki():
    """Download Persian Wikipedia frequency list."""
    words = []
    try:
        data = urllib.request.urlopen(PERSIAN_WIKI_URL, timeout=60).read().decode("utf-8")
        for line in data.strip().split("\n"):
            line = line.strip()
            if line and not line.startswith("#"):
                word = line.split()[0].strip()
                if filter_persian(word):
                    words.append(word)
    except Exception as e:
        print(f"    Warning: could not download Persian wiki: {e}")
    return words


def extract_wordfreq(lang_code):
    """Extract from wordfreq, sorted by frequency."""
    mode = WORDLIST_MODE[lang_code]
    filter_fn = FILTERS[lang_code]
    freq_dict = get_frequency_dict(lang_code, mode)
    filtered = [(w, f) for w, f in freq_dict.items() if filter_fn(w)]
    filtered.sort(key=lambda x: x[1], reverse=True)
    return [w for w, _ in filtered]


def main():
    parser = argparse.ArgumentParser(description="Extract word lists from multiple sources")
    parser.add_argument("--target", type=int, default=30000, help="Target word count per language")
    parser.add_argument("--output", type=str, default=None, help="Output directory")
    args = parser.parse_args()

    if args.output:
        output_dir = Path(args.output)
    else:
        script_dir = Path(__file__).parent
        output_dir = script_dir.parent / "src" / "KeyboardLanguageGuard.Core" / "Resources"

    output_dir.mkdir(parents=True, exist_ok=True)
    print(f"Output: {output_dir}  |  Target: {args.target}\n")

    for lang_code in ["en", "de", "ar", "fa"]:
        filename = OUTPUT_FILES[lang_code]
        output_path = output_dir / filename
        print(f"=== {lang_code} -> {filename} ===")

        existing = load_existing_words(output_path)
        print(f"  Existing: {len(existing)}")

        # Source 1: wordfreq (primary, frequency-sorted)
        wf_words = extract_wordfreq(lang_code)
        print(f"  wordfreq: {len(wf_words)}")

        # Source 2: hermitdave 50k
        hd_words = download_hermitdave(lang_code)
        print(f"  hermitdave: {len(hd_words)}")

        # Source 3: Persian Wikipedia (only for fa)
        pw_words = []
        if lang_code == "fa":
            pw_words = download_persian_wiki()
            print(f"  persian-wiki: {len(pw_words)}")

        # Combine: wordfreq first (frequency-sorted), then hermitdave, then persian-wiki, then existing
        combined = []
        seen = set()
        for w in wf_words:
            if w not in seen:
                combined.append(w)
                seen.add(w)
        for w in hd_words:
            if w not in seen:
                combined.append(w)
                seen.add(w)
        for w in pw_words:
            if w not in seen:
                combined.append(w)
                seen.add(w)
        for w in existing:
            if w not in seen:
                combined.append(w)
                seen.add(w)

        # Trim to target
        final = combined[:args.target]
        print(f"  Combined unique: {len(combined)} -> final: {len(final)}")

        with open(output_path, "w", encoding="utf-8", newline="\n") as f:
            for word in final:
                f.write(word + "\n")
        print(f"  Written: {output_path}\n")

    print("Done!")


if __name__ == "__main__":
    main()
