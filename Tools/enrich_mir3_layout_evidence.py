#!/usr/bin/env python3
"""Merge primary WIL dimensions into the evidence-backed layout catalog.

Coordinates remain expressions when the EI binary has not yet exposed the
HUD origin.  This is intentional: a relative rectangle is more truthful than
an invented absolute coordinate.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path


SKILL_REDRAW_POSITIONS = {
    "0x00439334": {"x": 5, "y": 21, "offset": "0x2f4"},
    "0x0043935d": {"x": 3, "y": 56, "offset": "0x3a8"},
    "0x00439386": {"x": 4, "y": 91, "offset": "0x45c"},
    "0x004393b3": {"x": 2, "y": 126, "offset": "0x510"},
    "0x004393e0": {"x": 2, "y": 161, "offset": "0x5c4"},
    "0x0043940d": {"x": 2, "y": 196, "offset": "0x678"},
    "0x00439437": {"x": 1, "y": 231, "offset": "0x72c"},
    "0x00439464": {"x": 2, "y": 266, "offset": "0x7e0"},
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--layout", type=Path, default=Path("docs/research/ei-ui-layout/layout.json"))
    parser.add_argument("--frames", type=Path, default=Path("docs/research/ei-ui-layout/gameinter-frame-metadata.json"))
    parser.add_argument("--windows", type=Path, default=Path("docs/research/ei-ui-layout/window_layout.json"))
    parser.add_argument("--controls", type=Path, default=Path("docs/research/ei-ui-layout/window-control-calls.json"))
    parser.add_argument("--positions", type=Path, default=Path("docs/research/ei-ui-layout/window-control-position-analysis.json"))
    parser.add_argument("--vtable-bindings", type=Path, default=Path("docs/research/ei-ui-layout/window-vtable-bindings.json"))
    parser.add_argument("--npc-paint", type=Path, default=Path("docs/research/ei-ui-layout/npc-paint-evidence.json"))
    parser.add_argument("--resource-handles", type=Path, default=Path("docs/research/ei-ui-layout/window-resource-handle-bindings.json"))
    parser.add_argument("--resource-paths", type=Path, default=Path("docs/research/ei-ui-layout/resource-path-table.json"))
    parser.add_argument("--resource-families", type=Path, default=Path("docs/research/ei-ui-layout/resource-family-catalog.json"))
    parser.add_argument("--resource-cluster-crossmatch", type=Path, default=Path("docs/research/ei-ui-layout/resource-cluster-crossmatch.json"))
    parser.add_argument("--interface1c-parent", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-parent-context.json"))
    parser.add_argument("--interface1c-select", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-select-screen-context.json"))
    parser.add_argument("--window-visual-semantics", type=Path, default=Path("docs/research/ei-ui-layout/window-frame-visual-semantics.json"))
    parser.add_argument("--control-semantics", type=Path, default=Path("docs/research/ei-ui-layout/control-semantic-catalog.json"))
    parser.add_argument("--skill-window-context", type=Path, default=Path("docs/research/ei-ui-layout/skill-window-context.json"))
    parser.add_argument("--magic-exp-records", type=Path, default=Path("docs/research/ei-ui-layout/magic-exp-records.json"))
    parser.add_argument("--inventory-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/inventory-window-render-evidence.json"))
    parser.add_argument("--status-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/status-window-render-evidence.json"))
    parser.add_argument("--quest-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/quest-window-render-evidence.json"))
    parser.add_argument("--store-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/store-window-render-evidence.json"))
    parser.add_argument("--store-state-graph", type=Path, default=Path("docs/research/ei-ui-layout/store-state-graph.json"))
    parser.add_argument("--map-ui-evidence", type=Path, default=Path("docs/research/ei-ui-layout/map-ui-resource-evidence.json"))
    parser.add_argument("--map-crossref", type=Path, default=Path("docs/research/ei-ui-layout/minimap-server-crossref.json"))
    parser.add_argument("--chat-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/chat-window-render-evidence.json"))
    parser.add_argument("--npc-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/npc-window-render-evidence.json"))
    parser.add_argument("--social-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/social-window-render-evidence.json"))
    parser.add_argument("--system-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/system-window-render-evidence.json"))
    parser.add_argument("--draw-order-evidence", type=Path, default=Path("docs/research/ei-ui-layout/draw-order-evidence.json"))
    parser.add_argument("--window-position-dispatch", type=Path, default=Path("docs/research/ei-ui-layout/window-position-dispatch-evidence.json"))
    parser.add_argument("--window-visibility-dispatch", type=Path, default=Path("docs/research/ei-ui-layout/window-visibility-dispatch-evidence.json"))
    parser.add_argument("--window-initialization", type=Path, default=Path("docs/research/ei-ui-layout/window-initialization-evidence.json"))
    parser.add_argument("--window-traversal", type=Path, default=Path("docs/research/ei-ui-layout/window-traversal-evidence.json"))
    parser.add_argument("--horse-window-evidence", type=Path, default=Path("docs/research/ei-ui-layout/horse-window-render-evidence.json"))
    parser.add_argument("--hud-label-evidence", type=Path, default=Path("docs/research/ei-ui-layout/hud-label-evidence.json"))
    parser.add_argument("--hud-bars-evidence", type=Path, default=Path("docs/research/ei-ui-layout/hud-bars-render-evidence.json"))
    parser.add_argument("--notice-prompt-evidence", type=Path, default=Path("docs/research/ei-ui-layout/notice-prompt-window-evidence.json"))
    parser.add_argument("--confirmation-prompt-evidence", type=Path, default=Path("docs/research/ei-ui-layout/confirmation-prompt-evidence.json"))
    parser.add_argument("--control-resources", type=Path, default=Path("docs/research/ei-ui-layout/window-control-resource-analysis.json"))
    parser.add_argument("--secondary-controls", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-cluster-4027.json"))
    parser.add_argument("--secondary-controls-extra", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-cluster-456d.json"))
    parser.add_argument("--secondary-controls-gameinter", type=Path, default=Path("docs/research/ei-ui-layout/gameinter-cluster-43e260.json"))
    args = parser.parse_args()

    layout = json.loads(args.layout.read_text(encoding="utf-8"))
    frames = json.loads(args.frames.read_text(encoding="utf-8"))["frames"]
    windows = json.loads(args.windows.read_text(encoding="utf-8"))["records"]
    controls = json.loads(args.controls.read_text(encoding="utf-8"))["records"] if args.controls.exists() else []
    positions = {}
    if args.positions.exists():
        positions = {r["call_va"]: r for r in json.loads(args.positions.read_text(encoding="utf-8"))["records"]}
    control_resources = {}
    if args.control_resources.exists():
        control_resources = {r["call_va"]: r for r in json.loads(args.control_resources.read_text(encoding="utf-8"))["records"]}
    secondary_controls = []
    secondary_window_candidates = []
    if args.secondary_controls.exists():
        secondary_controls = json.loads(args.secondary_controls.read_text(encoding="utf-8")).get("records", [])
    if args.secondary_controls_extra.exists():
        secondary_controls.extend(json.loads(args.secondary_controls_extra.read_text(encoding="utf-8")).get("records", []))
    if args.secondary_controls_gameinter.exists():
        gameinter_secondary = json.loads(args.secondary_controls_gameinter.read_text(encoding="utf-8"))
        secondary_controls.extend(gameinter_secondary.get("records", []))
        if gameinter_secondary.get("window_candidate"):
            secondary_window_candidates.append(gameinter_secondary["window_candidate"])
    bindings = {}
    if args.vtable_bindings.exists():
        bindings = {r["window_id"]: r for r in json.loads(args.vtable_bindings.read_text(encoding="utf-8"))["records"]}
    npc_paint = None
    if args.npc_paint.exists():
        npc_paint = json.loads(args.npc_paint.read_text(encoding="utf-8"))
    resource_handles = {}
    if args.resource_handles.exists():
        resource_handles = {r["window_id"]: r for r in json.loads(args.resource_handles.read_text(encoding="utf-8"))["records"]}
    resource_paths = json.loads(args.resource_paths.read_text(encoding="utf-8")) if args.resource_paths.exists() else {"records": []}
    resource_families = json.loads(args.resource_families.read_text(encoding="utf-8")) if args.resource_families.exists() else {"records": [], "counts": {}}
    resource_cluster_crossmatch = json.loads(args.resource_cluster_crossmatch.read_text(encoding="utf-8")) if args.resource_cluster_crossmatch.exists() else {"records": [], "counts": {}}
    interface1c_parent = json.loads(args.interface1c_parent.read_text(encoding="utf-8")) if args.interface1c_parent.exists() else {}
    interface1c_select = json.loads(args.interface1c_select.read_text(encoding="utf-8")) if args.interface1c_select.exists() else {}
    window_visual_semantics = {}
    if args.window_visual_semantics.exists():
        window_visual_semantics = {r["window_id"]: r for r in json.loads(args.window_visual_semantics.read_text(encoding="utf-8")).get("records", [])}
    control_semantics = {}
    if args.control_semantics.exists():
        control_semantics = {r["control_id"]: r for r in json.loads(args.control_semantics.read_text(encoding="utf-8")).get("records", [])}
    skill_window_context = json.loads(args.skill_window_context.read_text(encoding="utf-8")) if args.skill_window_context.exists() else {}
    magic_exp_records = json.loads(args.magic_exp_records.read_text(encoding="utf-8")) if args.magic_exp_records.exists() else {}
    inventory_window_evidence = json.loads(args.inventory_window_evidence.read_text(encoding="utf-8")) if args.inventory_window_evidence.exists() else {}
    status_window_evidence = json.loads(args.status_window_evidence.read_text(encoding="utf-8")) if args.status_window_evidence.exists() else {}
    quest_window_evidence = json.loads(args.quest_window_evidence.read_text(encoding="utf-8")) if args.quest_window_evidence.exists() else {}
    store_window_evidence = json.loads(args.store_window_evidence.read_text(encoding="utf-8")) if args.store_window_evidence.exists() else {}
    store_state_graph = json.loads(args.store_state_graph.read_text(encoding="utf-8")) if args.store_state_graph.exists() else {}
    map_ui_evidence = json.loads(args.map_ui_evidence.read_text(encoding="utf-8")) if args.map_ui_evidence.exists() else {}
    map_crossref = json.loads(args.map_crossref.read_text(encoding="utf-8")) if args.map_crossref.exists() else {}
    chat_window_evidence = json.loads(args.chat_window_evidence.read_text(encoding="utf-8")) if args.chat_window_evidence.exists() else {}
    npc_window_evidence = json.loads(args.npc_window_evidence.read_text(encoding="utf-8")) if args.npc_window_evidence.exists() else {}
    social_window_evidence = json.loads(args.social_window_evidence.read_text(encoding="utf-8")) if args.social_window_evidence.exists() else {}
    system_window_evidence = json.loads(args.system_window_evidence.read_text(encoding="utf-8")) if args.system_window_evidence.exists() else {}
    draw_order_evidence = json.loads(args.draw_order_evidence.read_text(encoding="utf-8")) if args.draw_order_evidence.exists() else {}
    window_position_dispatch = json.loads(args.window_position_dispatch.read_text(encoding="utf-8")) if args.window_position_dispatch.exists() else {}
    window_visibility_dispatch = json.loads(args.window_visibility_dispatch.read_text(encoding="utf-8")) if args.window_visibility_dispatch.exists() else {}
    window_initialization = json.loads(args.window_initialization.read_text(encoding="utf-8")) if args.window_initialization.exists() else {}
    window_traversal = json.loads(args.window_traversal.read_text(encoding="utf-8")) if args.window_traversal.exists() else {}
    horse_window_evidence = json.loads(args.horse_window_evidence.read_text(encoding="utf-8")) if args.horse_window_evidence.exists() else {}
    hud_label_evidence = json.loads(args.hud_label_evidence.read_text(encoding="utf-8")) if args.hud_label_evidence.exists() else {}
    hud_bars_evidence = json.loads(args.hud_bars_evidence.read_text(encoding="utf-8")) if args.hud_bars_evidence.exists() else {}
    notice_prompt_evidence = json.loads(args.notice_prompt_evidence.read_text(encoding="utf-8")) if args.notice_prompt_evidence.exists() else {}
    confirmation_prompt_evidence = json.loads(args.confirmation_prompt_evidence.read_text(encoding="utf-8")) if args.confirmation_prompt_evidence.exists() else {}

    for record in layout["records"]:
        if record.get("kind") != "button" or "frames" not in record.get("resource", {}):
            continue
        normal = str(record["resource"]["frames"].get("normal"))
        meta = frames.get(normal)
        if not meta or meta["width"] is None or meta["height"] is None:
            continue
        width, height = meta["width"], meta["height"]
        record["size"] = {"width": width, "height": height, "source": f"GameInter.wil frame {normal}"}
        record["hit_rect"] = {
            "x": record["position"]["x"],
            "y": record["position"]["y"],
            "width": width,
            "height": height,
            "basis": "0x00417550 SetRect; relative to hud.left/hud.top",
            "evidence_level": "primary-static",
        }

    existing_ids = {r["id"] for r in layout["records"]}
    for window in windows:
        if window["id"] in existing_ids:
            continue
        w = window["window"]
        layout["records"].append({
            "id": window["id"],
            "kind": "window",
            "layer": "windows",
            "resource": window["resource"],
            "position": {"x": w["x"], "y": w["y"]},
            "size": {"width": w["width"], "height": w["height"]},
            "hit_rect": None,
            "window": {"id": w["id"], "can_move": w["can_move"]},
            "evidence": window["evidence"],
        })

    for record in layout["records"]:
        if record.get("kind") != "window":
            continue
        binding = bindings.get(record["id"])
        if binding:
            record["vtable"] = {
                "derived_vtable": binding.get("derived_vtable_va"),
                "assignment_va": binding.get("derived_assignment_va"),
                "paint_slot_plus_0xc": binding.get("paint_slot_candidate_plus_0xc"),
                "paint_slot_matches_shared_base": binding.get("paint_slot_matches_shared_base"),
                "evidence_level": binding.get("binding_status"),
                "warning": binding.get("warning"),
            }
        handle = resource_handles.get(record["id"])
        if handle:
            record["resource_handle"] = handle["window_resource_argument"] | {
                "library": handle["resource_library"],
                "evidence": handle["evidence"],
            }
        if record["id"] == "window.npc-candidate" and npc_paint:
            record["special_paint"] = {
                "method_va": "0x0043f040",
                "evidence_level": npc_paint.get("evidence_level"),
                "frames": npc_paint.get("confirmed_frame_selects"),
                "calls": npc_paint.get("calls"),
                "interpretation": npc_paint.get("interpretation"),
            }
        if record["id"] in window_visual_semantics:
            record["visual_semantics"] = window_visual_semantics[record["id"]]

    # Keep unresolved window-internal controls in the same catalog without
    # pretending that their relative positions are known yet.
    layout["control_constructors"] = []
    for control in controls:
        pairs = control.get("frame_pair_candidates", [])
        pair = pairs[-1] if pairs else []
        position_analysis = positions.get(control["call_va"])
        coordinate_status = "unresolved"
        absolute_position = None
        if position_analysis:
            px = position_analysis.get("x", {}).get("absolute_candidate")
            py = position_analysis.get("y", {}).get("absolute_candidate")
            if px is not None and py is not None:
                absolute_position = {"x": px, "y": py}
                coordinate_status = position_analysis.get("geometric_status", "resolved-candidate")
        resource_analysis = control_resources.get(control["call_va"], {})
        dimensions = None
        resource_library = None
        frames_meta = resource_analysis.get("frames", [])
        if frames_meta:
            libraries = frames_meta[0].get("libraries") or {}
            candidates = [(name, meta) for name, meta in libraries.items()
                          if meta and meta.get("width") is not None and meta.get("height") is not None]
            if candidates:
                resource_library, selected = next(
                    ((name, meta) for name, meta in candidates if name == "GameInter.wil"),
                    candidates[0],
                )
                dimensions = {"width": selected.get("width"), "height": selected.get("height"),
                              "source": f"Data/{resource_library} frame {pair[0] if pair else 'unknown'}"}
        item = {
            "id": f"{control['window_id']}.control.{control['call_va']}",
            "kind": "control-constructor",
            "window_id": control["window_id"],
            "wrapper_va": control["wrapper_va"],
            "call_va": control["call_va"],
            "resource": {"file": "unresolved-resource-handle", "frame_pair": pair},
            "object_lea": control.get("object_lea"),
            "constructor_args": control.get("constructor_args"),
            "position_analysis": position_analysis,
            "coordinate_status": coordinate_status,
            "position": absolute_position,
            "size": dimensions,
            "hit_rect": ({"x": absolute_position["x"], "y": absolute_position["y"],
                          "width": dimensions["width"], "height": dimensions["height"],
                          "basis": f"0x00417550 SetRect; {resource_library or 'selected WIL'} Frame dimensions",
                          "evidence_level": "primary-static-expression-plus-primary-resource"}
                         if absolute_position and dimensions and dimensions["width"] and dimensions["height"] else None),
            "evidence": control["evidence"],
            "notes": (f"Frame pair, {resource_library or 'unresolved'} resource candidate, and push-time x/y expression are primary static evidence; "
                       "geometric status is retained as a separate validation field."),
        }
        redraw = SKILL_REDRAW_POSITIONS.get(control["call_va"].lower())
        if redraw:
            item["position"] = {"x": redraw["x"], "y": redraw["y"], "coordinate_space": "window-relative"}
            item["redraw_position"] = {
                **redraw,
                "source_va": "0x00439500",
                "evidence_level": "primary-static-redraw-position",
            }
            item["coordinate_status"] = "resolved-primary-redraw"
            if dimensions and dimensions.get("width") and dimensions.get("height"):
                item["hit_rect"] = {
                    "x": redraw["x"], "y": redraw["y"],
                    "width": dimensions["width"], "height": dimensions["height"],
                    "basis": "0x00439500 redraw SetPos + 0x00417550 SetRect; window-relative",
                    "evidence_level": "primary-static-redraw-plus-primary-resource",
                }
        if resource_handles.get(control["window_id"]):
            item["resource_handle"] = {
                "expression": "wrapper_entry_resource_arg1 (observed in edi)",
                "library": f"Data/{resource_library}" if resource_library else "unresolved-resource-handle",
                "evidence_level": "primary-static-handle-flow-plus-primary-resource-crosscheck" if resource_library else "primary-static-handle-flow",
                "warning": "Static register-flow record; selected WIL library is cross-checked from decoded frame headers and remains separate from the generic constructor handle expression.",
            }
        if item["id"] in control_semantics:
            item["semantic_candidate"] = control_semantics[item["id"]]
        layout["control_constructors"].append(item)

    layout["version"] = "0.3-primary-evidence-vtable-enriched"
    layout["resource_evidence"] = {
        "path_table": {"artifact": str(args.resource_paths), "record_count": len(resource_paths.get("records", []))},
        "family_catalog": {"artifact": str(args.resource_families), "counts": resource_families.get("counts", {})},
        "cluster_crossmatch": {"artifact": str(args.resource_cluster_crossmatch), "counts": resource_cluster_crossmatch.get("counts", {})},
        "evidence_level": "primary-static-resource-index",
        "warning": "Resource family labels do not prove a window's business name or draw order.",
    }
    layout["secondary_screen_candidates"] = [x for x in (interface1c_parent, interface1c_select) if x]
    layout["specialized_window_evidence"] = [x for x in (skill_window_context, inventory_window_evidence, status_window_evidence, quest_window_evidence, store_window_evidence, npc_window_evidence, social_window_evidence, system_window_evidence, notice_prompt_evidence, confirmation_prompt_evidence) if x]
    layout["magic_exp_records"] = magic_exp_records
    if map_crossref:
        map_ui_evidence = dict(map_ui_evidence)
        map_ui_evidence["server_cross_reference_rows"] = map_crossref.get("rows", [])
        map_ui_evidence["server_cross_reference_stats"] = map_crossref.get("stats", {})
    layout["map_ui_evidence"] = map_ui_evidence
    layout["chat_window_evidence"] = chat_window_evidence
    layout["npc_window_evidence"] = npc_window_evidence
    layout["social_window_evidence"] = social_window_evidence
    layout["system_window_evidence"] = system_window_evidence
    layout["store_state_graph"] = store_state_graph
    layout["draw_order_evidence"] = draw_order_evidence
    layout["window_position_dispatch_evidence"] = window_position_dispatch
    layout["window_visibility_dispatch_evidence"] = window_visibility_dispatch
    layout["window_initialization_evidence"] = window_initialization
    layout["window_traversal_evidence"] = window_traversal
    layout["horse_window_evidence"] = horse_window_evidence
    layout["hud_label_evidence"] = hud_label_evidence
    layout["hud_bars_render_evidence"] = hud_bars_evidence
    layout["notice_prompt_evidence"] = notice_prompt_evidence
    layout["confirmation_prompt_evidence"] = confirmation_prompt_evidence
    layout["draw_evidence"] = {
        "window_base_paint": "docs/research/ei-ui-layout/window-base-draw-evidence.json",
        "button_draw_chain": "docs/research/ei-ui-layout/button-draw-calls.json",
        "vtable_evidence": "docs/research/ei-ui-layout/window-vtable-evidence.json",
        "vtable_bindings": "docs/research/ei-ui-layout/window-vtable-bindings.json",
        "npc_special_paint": "docs/research/ei-ui-layout/npc-paint-evidence.json",
        "resource_handle_flow": "docs/research/ei-ui-layout/window-resource-handle-bindings.json",
        "hud_label_controls": "docs/research/ei-ui-layout/hud-label-evidence.json",
        "window_position_dispatch": "docs/research/ei-ui-layout/window-position-dispatch-evidence.json",
        "window_visibility_dispatch": "docs/research/ei-ui-layout/window-visibility-dispatch-evidence.json",
        "window_initialization": "docs/research/ei-ui-layout/window-initialization-evidence.json",
        "window_traversal": "docs/research/ei-ui-layout/window-traversal-evidence.json",
        "horse_window": "docs/research/ei-ui-layout/horse-window-render-evidence.json",
    }
    layout["secondary_control_constructors"] = secondary_controls
    layout["secondary_window_candidates"] = secondary_window_candidates
    args.layout.write_text(json.dumps(layout, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    buttons = sum(1 for r in layout["records"] if r["kind"] == "button")
    windows_count = sum(1 for r in layout["records"] if r["kind"] == "window")
    print(f"buttons={buttons} windows={windows_count} controls={len(layout['control_constructors'])} records={len(layout['records'])}")
    print(f"wrote={args.layout}")


if __name__ == "__main__":
    main()
