"""Collect every professor line from the Runtime sources: string literals inside Say(...) / Prof.Say(...) /
WhatNext(...) calls (all branches of ternaries and both C(vr, desktop) variants) and the spoken `line` of
every codex entry. Output: lines.json [{text, src}]; interpolated strings are reported (must not exist)."""
import re, json, sys, os, glob
root = sys.argv[1]; out = sys.argv[2]
def literals(expr):
    res, i = [], 0
    while i < len(expr):
        if expr[i] == '"' and (i == 0 or expr[i-1] != '$'):
            j = i + 1; buf = ''
            while expr[j] != '"':
                if expr[j] == '\\': buf += {'n': '\n', 't': '\t', '"': '"', '\\': '\\'}.get(expr[j+1], expr[j+1]); j += 2
                else: buf += expr[j]; j += 1
            res.append(buf); i = j + 1
        elif expr[i] == '$' and i + 1 < len(expr) and expr[i+1] == '"':
            j = i + 2
            while expr[j] != '"': j += 2 if expr[j] == '\\' else 1
            res.append(('INTERP', expr[i:j+1])); i = j + 1
        else: i += 1
    return res
def call_args(src, start):
    depth, i, instr = 0, start, False
    while i < len(src):
        c = src[i]
        if instr:
            if c == '\\': i += 2; continue
            if c == '"': instr = False
        else:
            if c == '"': instr = True
            elif c == '(': depth += 1
            elif c == ')':
                depth -= 1
                if depth == 0: return src[start + 1:i]
        i += 1
    return ''
lines, interp = {}, []
cyr = re.compile('[А-Яа-яЁё]')
for path in sorted(glob.glob(os.path.join(root, '*.cs'))):
    src = open(path, encoding='utf-8').read(); name = os.path.basename(path)
    for m in re.finditer(r'\b(Say|WhatNext)\s*\(', src):
        args = call_args(src, m.end() - 1)
        if name == 'Bvr2Professor.cs' or 'string text' in args or 'string who' in args or 'string freeSwimLine' in args: continue
        # only the first argument (text) — drop the mood after the last top-level comma
        for lit in literals(args):
            if isinstance(lit, tuple): interp.append((name, lit[1][:80])); continue
            if cyr.search(lit) and len(lit) > 6: lines.setdefault(lit, name)
    if name == 'Bvr2Codex.cs':
        for m in re.finditer(r'\bAdd\s*\(', src):
            lits = [l for l in literals(call_args(src, m.end() - 1)) if not isinstance(l, tuple)]
            if len(lits) >= 5: lines.setdefault(lits[3], name + ':' + lits[0])
json.dump([{'text': t, 'src': s} for t, s in lines.items()], open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(len(lines), 'lines;', 'interpolated:', interp)
