"""Run existing focused release regressions in one guarded disposable native lab."""
import subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
assert profile.is_relative_to((root/'private').resolve())
assert tag and all(c.isalnum() or c in '-_' for c in tag)
for script in ['sts2-tactical-check.py','sts2-delayed-reward-check.py','sts2-reward-intervention-check.py','sts2-vigor-check.py']:
    subprocess.run([sys.executable,str(root/'scripts'/script),str(profile),tag],check=True)
    print(tag,script,'passed',flush=True)
