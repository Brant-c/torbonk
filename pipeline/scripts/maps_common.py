"""Shared plumbing for the per-map (Hutta/Dromund Kaas/Balmorra) pipelines.

Generalizes the Korriban A4 scripts: same archive reader, same terrain decoder,
same conversion adapters; only the area/room/patch selection comes from a recipe.
"""
import json, os, re, sys
from pathlib import Path
import character_source
from source_pipeline import ROOT, SOURCE

def map_id():
    return os.environ['TORBONK_MAP']
RECIPE_PATH = lambda: ROOT/'maps'/f'{map_id()}.json'
def recipe(): return json.loads(RECIPE_PATH().read_text())
def work(): d = ROOT/'work/maps'/map_id(); d.mkdir(parents=True, exist_ok=True); return d
def asset_dir(): d = ROOT/'godot/Assets/Maps'/map_id(); d.mkdir(parents=True, exist_ok=True); return d
def manifest(name): return ROOT/'manifests'/f'map-{map_id()}-{name}.json'
def local(path): return SOURCE/path.lstrip('/')

class RetryPath(type(Path())):
    """Manifest path whose writes retry briefly (Windows sync/AV handles can reject an open)."""
    def write_text(self, data, *a, **k):
        import time
        for attempt in range(20):
            try: return super().write_text(data, *a, **k)
            except OSError:
                if attempt == 19: raise
                time.sleep(0.25)

def source():
    r = recipe()
    character_source.MANIFEST = RetryPath(str(manifest('source')))
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',
            maximumFiles=600, maximumDecodedBytes=300*1024*1024, resources=[], dependencies=[], unresolved=[],
            selection=f"{r['name']}: {r['section']}")))
    return character_source.SelectedSource()

def sections(text):
    result = {}; section = None
    for line in text.splitlines():
        if line.startswith('['): section = line.strip('[]'); result[section] = []
        elif section: result[section].append(line)
    return result

def records(text):
    result = {}; current = None
    for i, line in enumerate(text.splitlines(), 1):
        m = re.match(r'^  (\d+)=(.*)', line)
        if m:
            current = dict(id=m[1], asset=m[2], line=i, fields={}); result[m[1]] = current
        elif current and (m := re.match(r'^\s+\.(\w+)=(.*)', line)):
            current['fields'][m[1]] = m[2]
    return result
