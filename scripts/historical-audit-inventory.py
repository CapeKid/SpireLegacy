"""Read-only classification of #63 tracked historical and shared-tool boundaries."""
import csv
import hashlib
from pathlib import Path
import subprocess
from historical_tools import RETIRED_WRITERS, RETIRED_CLI, RETIRED_POWERSHELL

ROOT=Path(__file__).resolve().parents[1]
files=set(subprocess.check_output(['git','ls-files'],cwd=ROOT,text=True).splitlines())
files.update(subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=ROOT,text=True).splitlines())
rows=[]
for path in sorted(files):
    name=Path(path).name
    if path.startswith('src/main/java/heir/cards/'):
        category='historical generated card identity'
        source=(ROOT/path).read_text()
        assert source.startswith('package heir.cards; public final class ') and 'extends heir.HeirCard' in source
    elif path.startswith('src/main/java/'):
        category='historical Java runtime/check; excluded from native compilation'
    elif path.startswith('historical/sts1/'):
        category='archived configuration; not root-discovered'
    elif path in {'Play.cmd','Play.sh'}:
        category='retired launcher; early refusal'
    elif path.startswith('scripts/') and not name.startswith('sts2-'):
        if name in RETIRED_WRITERS:category='retired writer/tool; execution and import refusal'
        elif name in RETIRED_CLI:category='retired CLI; execution and import refusal'
        elif name in RETIRED_POWERSHELL:category='retired PowerShell; early refusal'
        elif name.endswith('.java') or name in {'LinuxLauncherChecks.py','ReaderDiscoveryChecks.py'}:
            category='historical disposable fixture; no active release invocation'
        else:category='live shared authoring, validation or audit helper; reviewed separately'
    elif path in {'src/main/resources/ModTheSpire.json','src/main/resources/heir/assets.json','src/main/resources/heir/host_assets.json'}:
        category='historical metadata/reader manifest; excluded from native release'
    elif path.startswith('licenses/python-build-standalone/'):
        category='historical reader license record; not executable'
    else:continue
    rows.append(dict(path=path,classification=category,sha256LF=hashlib.sha256((ROOT/path).read_bytes().replace(b'\r\n',b'\n')).hexdigest()))
assert sum(r['path'].startswith('src/main/java/') for r in rows)==389
assert sum(r['path'].startswith('src/main/java/heir/cards/') for r in rows)==360
output=ROOT/'private/historical-sts1-coverage.csv'
with output.open('w',encoding='utf-8',newline='') as stream:
    writer=csv.DictWriter(stream,fieldnames=['path','classification','sha256LF']);writer.writeheader();writer.writerows(rows)
print(f'{len(rows)} source/config/tool records classified; 389 Java files, including 360 card identities')
