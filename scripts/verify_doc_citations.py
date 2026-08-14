#!/usr/bin/env python3
"""Verify 路径:行号 citations in docs/codebase/*.md against actual source lines.

For each doc: sample N citations, print doc context + actual source line(s).
Heuristic check: an identifier token from the doc context line (or code fence
nearby) appearing in the cited source window counts as a match.
"""
import re, sys, os, random, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # repo root
DOCS = os.path.join(ROOT, "docs", "codebase")

CITE = re.compile(r"`?((?:ServerLibrary|Server|Client|LibraryCore|GodotClient|Tools|LibraryEditor|Patcher|Launcher|Components|ServerCore|PluginCore|ImageManager|Debug)/[A-Za-z0-9_/.-]+?\.(?:cs|json))[:：](\d+)(?:-(\d+))?`?")

def read_lines(path):
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            return f.read().splitlines()
    except OSError:
        return None

def identifiers(text):
    return set(re.findall(r"[A-Za-z_][A-Za-z0-9_]{3,}", text))

def main(sample_per_doc=3, seed=42):
    rng = random.Random(seed)
    report = []
    ok = bad = missing_file = 0
    md_files = []
    for dirpath, _, files in os.walk(DOCS):
        for fn in files:
            if fn.endswith(".md") and fn != "_index.md":
                md_files.append(os.path.join(dirpath, fn))
    md_files.sort()

    for md in md_files:
        lines = read_lines(md) or []
        # collect citations: (doc_line_no, src, line, end, context)
        cits = []
        for i, ln in enumerate(lines, 1):
            for m in CITE.finditer(ln):
                cits.append((i, m.group(1), int(m.group(2)),
                             int(m.group(3)) if m.group(3) else None, ln.strip()))
        rel = os.path.relpath(md, ROOT)
        if not cits:
            report.append(f"### {rel}: NO CITATIONS FOUND")
            continue
        sample = rng.sample(cits, min(sample_per_doc, len(cits)))
        report.append(f"### {rel} ({len(cits)} citations, sampled {len(sample)})")
        for doc_ln, src, sline, endline, ctx in sample:
            sp = os.path.join(ROOT, src)
            src_lines = read_lines(sp)
            if src_lines is None:
                report.append(f"  [MISSING FILE] {src}:{sline} (doc L{doc_ln})")
                missing_file += 1
                continue
            hi = endline or sline
            window = src_lines[max(0, sline - 4):min(len(src_lines), hi + 3)]
            window_ids = identifiers("\n".join(window))
            ctx_ids = identifiers(ctx) - {"cs", "the", "行", "见", "在"}
            hit = len(ctx_ids & window_ids)
            status = "OK" if hit >= 1 else "CHECK"
            if hit >= 1:
                ok += 1
            else:
                bad += 1
            report.append(f"  [{status}] {src}:{sline}{'' if not endline else '-' + str(endline)} (doc L{doc_ln})")
            report.append(f"      doc: {ctx[:150]}")
            actual = src_lines[sline - 1].strip() if 0 < sline <= len(src_lines) else "<<OUT OF RANGE>>"
            report.append(f"      src: {actual[:150]}")
        report.append("")
    print("\n".join(report))
    print(f"== summary: {ok} OK / {bad} CHECK / {missing_file} missing-file, {len(md_files)} docs")
    return 1 if (bad + missing_file) > 0 and os.environ.get("STRICT") else 0

if __name__ == "__main__":
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 3
    sys.exit(main(n))
