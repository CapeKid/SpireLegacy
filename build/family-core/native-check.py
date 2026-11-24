import hashlib
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
channel, profile_arg, dll = sys.argv[1:]
profile = Path(profile_arg)
labroot = root / "build/verify-170"
protocol = root / "scripts/oracle_protocol.py"
scope = {"__file__": str(protocol), "labroot": labroot}
exec(protocol.read_text().replace("(Path(__file__).resolve().parents[1]/'private').resolve()", "labroot.resolve()"), scope)
sha = hashlib.sha256(Path(dll).read_bytes()).hexdigest()
game = "v0.111.0" if channel == "regular" else "v0.113.1"
evidence = []


def send(command, **args):
    response = scope["call"](profile, dict(command=command, **args), timeout=60, expected_hash=sha)["response"]
    assert response["ok"], (command, args, response.get("error"))
    assert response["metadata"]["game"] == game, response["metadata"]
    return response["result"]


cards = {r["id"]: r for r in json.loads((root / "sheets/cards.json").read_text())}
reactions = {r["id"]: r for r in json.loads((root / "sheets/class_reactions.json").read_text())}
pools = json.loads((root / "sheets/card_pools.json").read_text())


def dmg(card, up):
    return cards[card]["damage"] + (cards[card]["upgradeDamage"] if up else 0)


def rx(card, cls, up=False):
    r = reactions[card]
    return r[cls + "Amount"] + (r.get(cls + "Upgrade", 0) if up else 0)


def step(label, **args):
    result = send("family-core", **args)
    evidence.append({"label": label, "args": args, "result": result})
    return result["before"], result["after"], result


def check(label, ok, detail):
    assert ok, (label, detail)


def hit(b, a, i=0):
    return b["enemies"][i]["hp"] - a["enemies"][i]["hp"]


for cls in ("knight", "mage", "ranger"):
    send("start", **{"class": cls, "quick": True, "traits": [], "manorTier": 0, "seed": "FAMILYCORE"})
    deck = send("family-core-deck")
    check("deck", deck.count("family_motto") == 1 and deck.count(cls + "_strike") == 4 and deck.count(cls + "_guard") == 4 and len(deck) == 10, deck)
    for card in reactions:
        text = send("family-core-text", card=card)
        evidence.append({"label": f"{cls} text {card}", "result": text})
        for other in ("knight", "mage", "ranger"):
            piece = max(reactions[card][other].split("{n}"), key=len).strip()
            check(f"{cls} text {card} {other}", (piece in text["description"]) == (other == cls), (piece, text))
    for up in (False, True):
        tag = f"{cls}{'+' if up else ''}"
        # Family Motto
        b, a, _ = step(tag + " motto", reset=True, upgrade=up, charges=0, attacks=0, play=["family_motto"])
        draw = 2 if up else 1
        check(tag + " motto draw", a["hand"] - b["hand"] == draw, (b, a))
        check(tag + " motto cost", a["energy"] == b["energy"], (b, a))
        if cls == "knight": check(tag + " motto block", a["block"] - b["block"] == rx("family_motto", cls), (b, a))
        else: check(tag + " motto no block", a["block"] == b["block"], (b, a))
        if cls == "mage": check(tag + " motto charges", a["charges"] == 1 + rx("family_motto", cls), (b, a))
        if cls == "ranger":
            check(tag + " motto rhythm", a["attacks"] == 1, (b, a))
            b, a, _ = step(tag + " motto rhythm reward", reset=True, upgrade=up, attacks=2, play=["family_motto"])
            check(tag + " motto rhythm draw", a["attacks"] == 3 and a["hand"] - b["hand"] == draw + 1, (b, a))
        # Heirloom Blade
        b, a, _ = step(tag + " heirloom", reset=True, upgrade=up, charges=0, attacks=0, play=["heirloom_blade"])
        check(tag + " heirloom damage", hit(b, a) == dmg("heirloom_blade", up), (b, a))
        check(tag + " heirloom block", a["block"] - b["block"] == (rx("heirloom_blade", cls) if cls == "knight" else 0), (b, a))
        check(tag + " heirloom draw", a["hand"] - b["hand"] == (rx("heirloom_blade", cls) if cls == "mage" else 0), (b, a))
        if cls == "ranger":
            b, a, _ = step(tag + " heirloom third", reset=True, upgrade=up, attacks=2, play=["heirloom_blade"])
            check(tag + " heirloom third damage", hit(b, a) == dmg("heirloom_blade", up) + rx("heirloom_blade", cls), (b, a))
        # Clean Armor
        b, a, _ = step(tag + " clean armor", reset=True, upgrade=up, charges=0, playerWeak=1, play=["knight_clean_armor"])
        base = cards["knight_clean_armor"]["block"] + (cards["knight_clean_armor"]["upgradeBlock"] if up else 0)
        check(tag + " clean cleanse", b["playerWeak"] == 1 and a["playerWeak"] == 0, (b, a))
        check(tag + " clean block", a["block"] - b["block"] == base + (rx("knight_clean_armor", cls) if cls == "knight" else 0), (b, a))
        check(tag + " clean draw", a["hand"] - b["hand"] == (rx("knight_clean_armor", cls) if cls == "ranger" else 0), (b, a))
        if cls == "mage": check(tag + " clean charges", a["charges"] == 1 + rx("knight_clean_armor", cls), (b, a))
        # Banner Volley
        charges = 3 if cls == "mage" else 0
        attacks = 1 if cls == "ranger" else 0
        b, a, r = step(tag + " banner", reset=True, upgrade=up, charges=charges, attacks=attacks, play=["banner_volley"])
        expected = dmg("banner_volley", up) + (min(8, charges * rx("banner_volley", cls)) if cls == "mage" else 0)
        check(tag + " banner aoe", all(hit(b, a, i) == expected for i in range(len(b["enemies"]))), (b, a, expected))
        check(tag + " banner block", a["block"] - b["block"] == (rx("banner_volley", cls) if cls == "knight" else 0), (b, a))
        if cls == "mage": check(tag + " banner consumes", a["charges"] == 0 and a["hand"] - b["hand"] == charges, (b, a))
        if cls == "ranger": check(tag + " banner rhythm", a["attacks"] == 3 and a["hand"] - b["hand"] == 1 and all(e["vulnerable"] >= 1 for e in a["enemies"]), (b, a))
        # Birthright
        b, a, _ = step(tag + " birthright", reset=True, upgrade=up, charges=charges, attacks=0, play=["birthright"])
        expected = dmg("birthright", up) + (min(10, charges * rx("birthright", cls)) if cls == "mage" else 0)
        check(tag + " birthright damage", hit(b, a) == expected, (b, a, expected))
        check(tag + " birthright block", a["block"] - b["block"] == (rx("birthright", cls, up) if cls == "knight" else 0), (b, a))
        if cls == "mage": check(tag + " birthright keeps charges", a["charges"] == charges and a["hand"] == b["hand"], (b, a))
        if cls == "ranger": check(tag + " birthright all", all(hit(b, a, i) == expected for i in range(len(b["enemies"]))), (b, a))
        else: check(tag + " birthright single", all(hit(b, a, i) == 0 for i in range(1, len(b["enemies"]))), (b, a))
        # Inherited Grudge
        b, a, _ = step(tag + " grudge plain", reset=True, upgrade=up, charges=0, play=["inherited_grudge"])
        check(tag + " grudge plain", hit(b, a) == dmg("inherited_grudge", up), (b, a))
        b, a, _ = step(tag + " grudge weak", reset=True, upgrade=up, charges=0, enemyWeak=1, play=["inherited_grudge"])
        check(tag + " grudge repeat", hit(b, a) == 2 * dmg("inherited_grudge", up), (b, a))
        # Lineage Strike
        _, _, r = step(tag + " lineage cost", reset=True, upgrade=up, costOf="lineage_strike")
        check(tag + " lineage base cost", r["cost"] == 2, r)
        step(tag + " lineage power", upgrade=up, play=["hearth_memory"])
        _, _, r = step(tag + " lineage discount", upgrade=up, costOf="lineage_strike")
        check(tag + " lineage discount", r["cost"] == 1 and r["after"]["powersPlayed"] == 1, r)
        b, a, _ = step(tag + " lineage play", upgrade=up, charges=0, attacks=0, play=["lineage_strike"])
        check(tag + " lineage paid", hit(b, a) == dmg("lineage_strike", up) and b["energy"] - a["energy"] == 1, (b, a))
        # Crown Breaker
        b, a, r = step(tag + " crown miss", reset=True, upgrade=up, charges=0, attacks=0, play=["crown_breaker"])
        check(tag + " crown miss", hit(b, a) == dmg("crown_breaker", up) and b["energy"] - a["energy"] == 2 and a["hand"] == b["hand"], (b, a))
        b, a, r = step(tag + " crown kill", reset=True, extraEnemy=True, upgrade=up, charges=0, attacks=0, enemyHp=1, play=["crown_breaker"])
        check(tag + " crown kill", r["enemyCount"] >= 2 and r["targetKilled"] and b["energy"] - a["energy"] == 1 and a["hand"] - b["hand"] == 2, (b, a, r))
        # Heraldry
        _, base_turn, _ = step(tag + " heraldry baseline", reset=True, upgrade=up, charges=0, attacks=0, endTurn=True)
        _, a, _ = step(tag + " heraldry", reset=True, upgrade=up, charges=0, attacks=0, play=["heraldry"], endTurn=True)
        if cls == "knight": check(tag + " heraldry primed", base_turn["charges"] == 0 and a["charges"] == 1, (base_turn, a))
        if cls == "mage": check(tag + " heraldry charges", a["charges"] == base_turn["charges"] + rx("heraldry", cls), (base_turn, a))
        if cls == "ranger": check(tag + " heraldry rhythm", base_turn["attacks"] == 0 and a["attacks"] == rx("heraldry", cls), (base_turn, a))
        # Hearth Memory
        b, a, _ = step(tag + " hearth", reset=True, upgrade=up, play=["hearth_memory"], retain=2, flush=True)
        magic = cards["hearth_memory"]["magic"] + (cards["hearth_memory"]["upgradeMagic"] if up else 0)
        check(tag + " hearth block", a["block"] - b["block"] == 2 * magic, (b, a))
        # Old Grudges
        b, a, _ = step(tag + " old grudges", reset=True, upgrade=up, charges=0, attacks=0, play=["old_grudges", "knight_long_reach"])
        magic = cards["old_grudges"]["magic"] + (cards["old_grudges"]["upgradeMagic"] if up else 0)
        check(tag + " old grudges", hit(b, a) == dmg("knight_long_reach", up) + magic and a["enemies"][0]["weak"] >= 1, (b, a))
        # Generations
        _, _, r = step(tag + " generations cost", reset=True, upgrade=up, costOf="generations")
        check(tag + " generations cost", r["cost"] == (2 if up else 3), r)
        _, a, _ = step(tag + " generations", reset=True, upgrade=up, play=["generations"], endTurn=True)
        unique = {p["cardId"] for p in pools if p["classId"] == cls and p["scope"] == "unique" and cards[p["cardId"]]["rarity"] != "BASIC"}
        made = [c for c in a["cards"] if c["key"] in unique]
        check(tag + " generations card", len(made) >= 1 and all(c["cost"] == 0 and c["exhaust"] for c in made), a["cards"])
    send("abandon")

(profile / "family-core.json").write_text(json.dumps(evidence, indent=2))
print("verified", channel, game, len(evidence), "steps", "skipped:", [e["label"] for e in evidence if "skipped" in e])
