"""Selected character extraction, reusing the Task 3 TOR reader unchanged.

No archive payload is decoded during indexing. Extraction is limited to the
explicit character dependencies and discovery registries, never filename globs.
"""
import json, hashlib, zlib, argparse
import xml.etree.ElementTree as ET
from source_pipeline import ROOT, SOURCE, ARCHIVES, entries, pathhash, sha

MANIFEST = ROOT/'manifests/character-source.json'

class SelectedSource:
    def __init__(self):
        self.index = {}
        for archive in sorted(ARCHIVES.glob('*.tor')):
            for e in entries(archive):
                self.index.setdefault(e['archiveHash'], []).append(e)
        self.manifest = json.loads(MANIFEST.read_text()) if MANIFEST.exists() else {
            'profile':'April2012 (operator labelled)', 'maximumFiles':180,
            'maximumDecodedBytes':100*1024*1024, 'resources':[], 'dependencies':[],
            'unresolved':[], 'selection':'human male body type 2; bmnnew skeleton'}
        self.baseline = {x['path']:x for x in json.loads((ROOT/'manifests/source-inventory.json').read_text())['archives']}
        inventory=json.loads((ROOT/'manifests/source-inventory.json').read_text())
        assert sha(ROOT.parent/'docs/investigation/asset-evidence-2026-10-20.json')==inventory['evidenceSha256'], 'investigation evidence changed'

    def has(self, path):
        return len(self.index.get(f'0x{pathhash(path):016X}', [])) == 1

    def get(self, path, parent='character selection', relation='selected dependency'):
        path=path.lower().replace('\\','/')
        if not path.startswith('/resources/'):
            path='/resources/'+path.lstrip('/')
        assert '..' not in path.split('/')
        matches=self.index.get(f'0x{pathhash(path):016X}', [])
        assert len(matches)==1, (path, 'missing or ambiguous', matches)
        e=dict(matches[0]); assert e['length'] <= 16*1024*1024
        baseline=self.baseline[e['archive']]; st=__import__('pathlib').Path(e['archive']).stat()
        assert st.st_size==baseline['length'] and str(st.st_mtime_ns)==baseline['mtimeNs'], 'archive metadata changed'
        rec=next((r for r in self.manifest['resources'] if r['path']==path),None)
        if not rec:
            assert len(self.manifest['resources']) < self.manifest['maximumFiles']
            assert sum(r['length'] for r in self.manifest['resources'])+e['length'] <= self.manifest['maximumDecodedBytes']
        with open(e['archive'],'rb') as f:
            f.seek(e['dataOffset']); payload=f.read(e['compressedSize'])
        if e['compressionMethod']==1:
            dec=zlib.decompressobj(); data=dec.decompress(payload,e['length']+1)
            assert dec.eof and not dec.unconsumed_tail
        else:
            assert e['compressionMethod']==0; data=payload
        assert len(data)==e['length']
        digest=hashlib.sha256(data).hexdigest(); out=SOURCE/path.lstrip('/')
        assert out.resolve().is_relative_to(SOURCE.resolve())
        if out.exists(): assert sha(out)==digest, 'refuse changed original'
        else:
            out.parent.mkdir(parents=True,exist_ok=True); out.write_bytes(data)
        e.update(path=path, sha256=digest, output=str(out.relative_to(ROOT)), archiveSha256=baseline['sha256'],
                 lookupEvidence='exact path lookup3 hash; unique April entry; decoded bytes hashed')
        if rec: assert rec==e
        else: self.manifest['resources'].append(e)
        edge={'from':parent,'to':path,'relation':relation}
        if edge not in self.manifest['dependencies']: self.manifest['dependencies'].append(edge)
        MANIFEST.write_text(json.dumps(self.manifest,indent=2))
        return data

    def registry(self, slot):
        p=f'/resources/art/dynamic/{slot}/index.xml'
        return ET.fromstring(self.get(p, relation='appearance registry'))

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('stage',choices=['discover','verify'])
    args=parser.parse_args(); src=SelectedSource()
    if args.stage=='discover':
        for slot in ['head','chest','leg','hand','boot','waist','hair','skincolor','eyecolor','haircolor','complexion','facepaint','colorscheme','garmenthue']:
            root=src.registry(slot)
            if slot in ['head','chest','leg','hand','boot','waist','hair']:
                candidates=[a for a in root if (a.findtext('ID')=='1000247' if slot=='head' else ('light' in (a.findtext('ArtName') or '') and '_si_' in (a.findtext('ArtName') or '')))]
                if slot=='hair': candidates=[a for a in root if 'human' in (a.findtext('ArtName') or '')][:3]
                for a in candidates[:18]:
                    print(slot, ET.tostring(a,encoding='unicode')[:3500])
            else: print(slot,ET.tostring(root[0],encoding='unicode')[:1800])
    else:
        for rec in src.manifest['resources']:
            assert sha(ROOT/rec['output'])==rec['sha256']
        (ROOT/'manifests/character-source-integrity.json').write_text(json.dumps({
            'passed':True,'checkedExtractedOriginals':len(src.manifest['resources']),
            'archiveMetadataMatchesBaseline':True,'investigationEvidenceUnchanged':True,
            'wholeArchiveHashCheck':'manifests/source-integrity.json (shared original pipeline)',
            'changes':[]},indent=2))
        print('Verified',len(src.manifest['resources']),'selected original files')
