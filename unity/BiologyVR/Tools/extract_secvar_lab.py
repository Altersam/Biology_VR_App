"""Selective, safe extraction of SecVarLab_Full.unitypackage into the project.
Only the lab models, their materials, the capsule prefab and the reference scene; never project
settings, editor scripts, URP configs, XRI samples or Quibli files. GUIDs are kept (.meta copied)."""
import sys, tarfile, os, json
pkg, project = sys.argv[1], sys.argv[2]
SRC, DST = 'Assets/#Project/', 'Assets/ThirdParty/SecVarLab/'
INCLUDE = ('Assets/#Project/LevelDesign/', 'Assets/#Project/Scenes/')
entries = {}
with tarfile.open(pkg, 'r:gz') as tar:
    for m in tar.getmembers():
        parts = m.name.strip('./').split('/')
        if len(parts) != 2 or not m.isfile(): continue
        entries.setdefault(parts[0], {})[parts[1]] = m
    written, skipped = [], []
    for guid, files in entries.items():
        if 'pathname' not in files: continue
        path = tar.extractfile(files['pathname']).read().decode('utf-8').splitlines()[0].strip()
        if not path.startswith(INCLUDE) or '..' in path:
            skipped.append(path); continue
        target = os.path.join(project, DST + path[len(SRC):])
        os.makedirs(os.path.dirname(target), exist_ok=True)
        for name, suffix in (('asset', ''), ('asset.meta', '.meta')):
            if name in files:
                with open(target + suffix, 'wb') as f: f.write(tar.extractfile(files[name]).read())
        written.append(DST + path[len(SRC):])
    # folder metas are not in the package: Unity creates them
print(json.dumps({'written': sorted(written), 'skipped': sorted(skipped)}, ensure_ascii=False, indent=1))
