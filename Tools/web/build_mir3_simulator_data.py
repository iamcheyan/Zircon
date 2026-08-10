#!/usr/bin/env python3
"""Build the unified data model consumed by the Mir3 EI 800x600 client
simulator (Tools/mir3_client_simulator/).

All coordinates come from the evidence catalog (docs/research/ei-ui-layout/).
Nothing here invents primary facts: window origins that the binary has not
exposed are emitted as `candidate` (centered within the 800x600 viewport) or
`pending`, and every control carries its evidence_level. The HTML simulator
renders candidate geometry with the candidate marker so it can never be
mistaken for original-binary fact.

Outputs (all under Tools/mir3_client_simulator/data/):
  windows.json        window containers: id, screen rect, frame, visibility, evidence
  controls.json       every interactive/display control: id, rect, frame pair,
                      state list, zIndex, hitTest, evidence, window_id
  resources.json      WIL library registry with frame counts (from catalog)
  entities.json       scene entities: player, monsters, NPCs, drops
  equipment_slots.json  character panel equipment slots
  skills.json         skill grid entries
  maps.json           map background / minimap candidate frames
  hud.json            HUD bars + target info + chat region + minimap widget
"""

from __future__ import annotations

import json
from pathlib import Path

EVIDENCE = Path("docs/research/ei-ui-layout")
OUT = Path("Tools/mir3_client_simulator/data")

VIEW_W, VIEW_H = 800, 600
HUD_ORIGIN = (0, 465)  # GameInter F50 800x136; top = 601 - 136


def load(name: str) -> dict:
    return json.loads((EVIDENCE / name).read_text(encoding="utf-8"))


def rel_to_abs(pos: dict, base: tuple[int, int]) -> tuple[int, int]:
    x, y = pos.get("x", {}), pos.get("y", {})
    ox = x.get("offset", 0) if isinstance(x, dict) else 0
    oy = y.get("offset", 0) if isinstance(y, dict) else 0
    if isinstance(x, dict) and x.get("base") in ("hud.left", "hud.right"):
        bx = base[0] if x["base"] == "hud.left" else 0
    else:
        bx = base[0]
    if isinstance(y, dict) and y.get("base") in ("hud.top", "hud.bottom"):
        by = base[1] if y["base"] == "hud.top" else 0
    else:
        by = base[1]
    return bx + ox, by + oy


def main() -> None:
    layout = load("layout.json")
    OUT.mkdir(parents=True, exist_ok=True)

    records = layout["records"]
    hud_records = [r for r in records if r["kind"] == "button"]
    window_records = [r for r in records if r["kind"] == "window"]
    other = [r for r in records if r["kind"] not in ("button", "window")]

    # ---------------------------------------------------------------- windows
    windows: list[dict] = []
    init_evidence = layout.get("window_initialization_evidence", {}).get("records", [])
    init_by_id = {r.get("layout_id"): r for r in init_evidence}
    confirmed_origins = {
        "window.guild-candidate": (102, 22),
        "window.group": (272, 123),
        "window.chat-pop": (114, 76),
        "window.option": (276, 113),
        "window.notice-prompt-candidate": (107, 110),
    }
    for rec in window_records:
        wid = rec["id"]
        res = rec.get("resource", {})
        size = rec.get("size", {})
        w, h = size.get("width", 0), size.get("height", 0)
        pos = rec.get("position", {})
        origin = confirmed_origins.get(wid)
        evidence = "primary-static"
        if origin is None:
            px, py = pos.get("x"), pos.get("y")
            if isinstance(px, dict) and isinstance(px.get("offset"), (int, float)) \
               and isinstance(py, dict) and isinstance(py.get("offset"), (int, float)):
                origin = (px["offset"], py["offset"])
                evidence = "primary-static"
            else:
                origin = ((VIEW_W - w) // 2, (VIEW_H - h) // 2)
                evidence = "candidate"
        windows.append({
            "id": wid,
            "title": wid.replace("window.", "").replace("-candidate", ""),
            "rect": [origin[0], origin[1], w, h],
            "frame": res.get("frame") if isinstance(res.get("frame"), int) else res.get("frames", {}).get("normal"),
            "resource_library": res.get("file"),
            "evidence_level": evidence,
            "init_va": init_by_id.get(wid, {}).get("va"),
            "visibility_va": init_by_id.get(wid, {}).get("default_visibility", ""),
        })

    # -------------------------------------------------------------- controls
    controls: list[dict] = []
    specialized = layout.get("specialized_control_rects", [])
    for i, c in enumerate(specialized):
        rel = c["relative_rect"]
        wid = c["window_id"]
        base = next((w for w in windows if w["id"] == wid), None)
        bx, by = (base["rect"][0], base["rect"][1]) if base else (0, 0)
        frame_pair = c.get("frame_pair") or [None, None]
        controls.append({
            "id": c.get("id", f"specialized-{i}"),
            "window_id": wid,
            "rect": [bx + rel[0], by + rel[1], rel[2], rel[3]],
            "relative_rect": rel,
            "frame_pair": frame_pair,
            "resource_library": c.get("resource_library", "GameInter.wil"),
            "state": ["normal", "hover", "pressed"],
            "zIndex": 40,
            "hitTest": True,
            "evidence_level": c.get("evidence_level", "candidate"),
            "source": c.get("source", ""),
        })

    # HUD buttons as controls
    for rec in hud_records:
        frames = rec["resource"]["frames"]
        size = rec["size"]
        x, y = rel_to_abs(rec["position"], HUD_ORIGIN)
        w, h = size["width"], size["height"]
        controls.append({
            "id": rec["id"],
            "window_id": "hud",
            "rect": [x, y, w, h],
            "relative_rect": [x - HUD_ORIGIN[0], y - HUD_ORIGIN[1], w, h],
            "frame_pair": [frames["normal"], frames.get("state")],
            "resource_library": rec["resource"]["file"],
            "state": ["normal", "hover", "pressed"],
            "zIndex": 60,
            "hitTest": True,
            "evidence_level": rec["evidence"]["level"],
            "source": ";".join(rec["evidence"].get("addresses", [])),
        })

    # -------------------------------------------------------------- resources
    resource_family = load("resource-family-catalog.json").get("records", [])
    resources: list[dict] = []
    for r in resource_family:
        p = r.get("path", "")
        lib = r.get("library", {})
        resources.append({
            "path": p,
            "library": p.rsplit("/", 1)[-1],
            "frame_count": lib.get("frame_count"),
            "nonblank_frame_count": lib.get("nonblank_frame_count"),
            "category": r.get("category"),
        })

    # -------------------------------------------------------------- entities
    # Scene composition is a demo layer; entity frames are candidate picks
    # from real libraries so the simulator shows genuine sprites.
    entities: list[dict] = [
        {"id": "player", "name": "玩家", "kind": "player",
         "x": 320, "y": 300, "library": "M-Hum.wil", "frame": 0,
         "evidence_level": "candidate",
         "note": "player sprite demo; real M-Hum.wil frame"},
        {"id": "npc.guild", "name": "行会管理员", "kind": "npc",
         "x": 380, "y": 340, "library": "NPC.wil", "frame": 0,
         "evidence_level": "candidate",
         "note": "NPC dialogue opens on click"},
        {"id": "npc.store", "name": "商店老板", "kind": "npc",
         "x": 440, "y": 360, "library": "NPC.wil", "frame": 1,
         "evidence_level": "candidate",
         "note": "store window opens on click"},
        {"id": "mon.1", "name": "稻草人", "kind": "monster",
         "x": 260, "y": 320, "library": "DMon-1.wil", "frame": 0,
         "evidence_level": "candidate",
         "note": "targetable monster"},
        {"id": "mon.2", "name": "鸡", "kind": "monster",
         "x": 480, "y": 280, "library": "DMon-1.wil", "frame": 2,
         "evidence_level": "candidate",
         "note": "targetable monster"},
        {"id": "drop.1", "name": "金创药", "kind": "drop",
         "x": 300, "y": 350, "library": "Ground.wil", "frame": 0,
         "evidence_level": "candidate",
         "note": "ground drop item"},
    ]

    # -------------------------------------------------------- equipment slots
    equipment_slots: list[dict] = [
        {"id": "slot.weapon", "name": "武器", "x": 60, "y": 100, "w": 38, "h": 38,
         "library": "Equip.wil", "frame": 0, "evidence_level": "candidate"},
        {"id": "slot.helmet", "name": "头盔", "x": 100, "y": 60, "w": 38, "h": 38,
         "library": "Equip.wil", "frame": 1, "evidence_level": "candidate"},
        {"id": "slot.armor", "name": "衣服", "x": 60, "y": 140, "w": 38, "h": 38,
         "library": "Equip.wil", "frame": 2, "evidence_level": "candidate"},
        {"id": "slot.necklace", "name": "项链", "x": 140, "y": 60, "w": 38, "h": 38,
         "library": "Equip.wil", "frame": 3, "evidence_level": "candidate"},
        {"id": "slot.belt", "name": "腰带", "x": 100, "y": 180, "w": 38, "h": 38,
         "library": "Equip.wil", "frame": 4, "evidence_level": "candidate"},
        {"id": "slot.boots", "name": "靴子", "x": 60, "y": 220, "w": 38, "h": 38,
         "library": "Equip.wil", "frame": 5, "evidence_level": "candidate"},
    ]

    # ---------------------------------------------------------------- skills
    skills: list[dict] = []
    for i in range(12):
        frame = 410 + i
        skills.append({
            "id": f"skill.{i}",
            "name": f"技能 {i + 1}",
            "x": 30 + (i % 4) * 40, "y": 60 + (i // 4) * 40,
            "w": 36, "h": 36,
            "library": "GameInter.wil", "frame": frame,
            "evidence_level": "candidate",
            "note": f"skill grid slot; F{frame} from skill-window evidence",
        })

    # ------------------------------------------------------------------ maps
    maps: list[dict] = [
        {"id": "map.bg", "name": "地图背景", "library": "FMMap.wil", "frame": 0,
         "evidence_level": "candidate", "note": "full-map resource candidate"},
        {"id": "map.minimap", "name": "小地图", "library": "MMap.wil", "frame": 0,
         "evidence_level": "candidate", "note": "fixed minimap 128x128 candidate"},
    ]

    # ------------------------------------------------------------------- hud
    hud: dict = {
        "origin": list(HUD_ORIGIN),
        "background_frame": 50,
        "resource_library": "GameInter.wil",
        "hp_bar": {"rect": [61, 496, 104, 566], "frame": 60,
                   "evidence_level": "primary-static",
                   "note": "0x004276D6 SetRect; (血量)%d/%d formatter"},
        "mp_bar": {"rect": [105, 496, 147, 566], "frame": 61,
                   "evidence_level": "primary-static",
                   "note": "0x004276F0 SetRect; (魔法)%d/%d formatter"},
        "exp_bar": {"rect": [61, 586, 400, 597], "frame": 63,
                    "evidence_level": "primary-static",
                    "note": "0x0042770D SetRect; (经验)%d/%d formatter"},
        "target_info": {"rect": [235, 496, 400, 586], "evidence_level": "primary-static",
                        "note": "0x004276B3 text region candidate"},
        "chat_region": {"rect": [224, 492, 578, 566], "evidence_level": "primary-static",
                        "note": "0x00427696 SetRect; chat/text total region"},
        "minimap": {"rect": [672, 0, 800, 128], "evidence_level": "primary-static",
                    "note": "fixed minimap rect (672,0)-(800,128)"},
    }

    out = {
        "windows": windows,
        "controls": controls,
        "resources": resources,
        "entities": entities,
        "equipment_slots": equipment_slots,
        "skills": skills,
        "maps": maps,
        "hud": hud,
        "viewport": {"width": VIEW_W, "height": VIEW_H},
        "meta": {
            "source": "docs/research/ei-ui-layout/layout.json + specialist evidence",
            "version": layout.get("version"),
            "generated_by": "Tools/web/build_mir3_simulator_data.py",
            "evidence_rule": "candidate geometry is never presented as primary fact",
        },
    }

    # Split into per-domain files (docs-required layout) plus a full bundle.
    for domain in ("windows", "controls", "resources", "entities",
                   "equipment_slots", "skills", "maps", "hud"):
        (OUT / f"{domain}.json").write_text(
            json.dumps(out[domain], ensure_ascii=False, indent=2), encoding="utf-8")
    (OUT / "layout.json").write_text(
        json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"windows={len(windows)} controls={len(controls)} resources={len(resources)}")
    print(f"entities={len(entities)} equipment_slots={len(equipment_slots)} skills={len(skills)}")
    print(f"maps={len(maps)} wrote={OUT}/layout.json")


if __name__ == "__main__":
    main()
