"""Compare native class sizes and capture comparable trait-free Workshop images.

Run against an already launched disposable owned-game oracle profile.
These are controlled visual fixtures, not natural campaign evidence.
"""
import hashlib
import json
import re
import shutil
import sys
import time
from pathlib import Path

from PIL import Image
from oracle_protocol import call

root = Path(__file__).resolve().parents[1]
profile = Path(sys.argv[1]).resolve()
expected = hashlib.sha256((root / "sts2/bin/Release/net9.0/SpireLegacy.dll").read_bytes()).hexdigest()
records = []
measurements = []
art = {row["id"]: row for row in json.loads((root / "sheets/character_art.json").read_text())}
output = root / "media/workshop-gallery"


def send(command, **args):
    record = call(profile, dict(command=command, **args), timeout=90, expected_hash=expected)
    records.append(record)
    (profile / "class-size-receipts.json").write_text(json.dumps(records, indent=2))
    reply = record["response"]
    assert reply["ok"], reply
    assert {m["id"] for m in reply["metadata"]["mods"]} == {"BaseLib", "SpireLegacy"}
    return reply["result"]


def ready():
    for _ in range(150):
        state = send("state")
        if state["phase"] == "Play":
            return state
        time.sleep(.1)
    raise TimeoutError("Native combat did not reach Play")


for cls in ["knight", "mage", "ranger"]:
    for traits in [[], ["small"]]:
        send("start", **{"class": cls, "traits": traits, "quick": True,
                         "manorTier": 0, "ascension": 0, "seed": "CLASS-SIZE-COMPARISON"})
        state = ready()
        assert state["heir"]["classId"] == cls and state["heir"]["traits"] == traits, state
        time.sleep(2)
        visual = send("visual")
        assert len(visual) == 1 and visual[0]["classId"] == cls and visual[0]["visible"], visual
        scale = [float(n) for n in re.findall(r"-?\d+(?:\.\d+)?", visual[0]["scale"])]
        assert len(scale) == 2, visual
        normalized = scale[1] * art[cls]["height"]
        target = 230 * (.8 if traits else 1)
        assert abs(normalized - target) < .01, (cls, traits, normalized, target)
        atlas = Image.open(root / "src/main/resources" / art[cls]["path"])
        bounds = []
        for x, y, w, h in art[cls]["regions"][:4]:
            alpha = atlas.crop((x, y, x+w, y+h)).getchannel("A").point(lambda a: 255 if a >= 16 else 0)
            box = alpha.getbbox()
            assert box
            bounds.append(round((box[3] - box[1]) * scale[1], 3))
        measurements.append(dict(cls=cls, traits=traits, normalizedHeight=normalized,
                                 idleAlphaHeights=bounds, visual=visual[0]))
        if not traits:
            shot = send("screenshot")
            assert shot["error"] == "Ok", shot
            ordinal = {"knight": "01", "mage": "02", "ranger": "03"}[cls]
            shutil.copy2(shot["path"], output / f"{ordinal}-{cls}-combat.png")
        print(cls, traits, "normalized height", round(normalized, 3), "idle bounds", bounds, flush=True)

send("abandon")
(profile / "class-size-measurements.json").write_text(json.dumps(measurements, indent=2))
print("All classes normalize to 230 units; Dwarfism scales each to 184. Three trait-free screenshots captured.")
