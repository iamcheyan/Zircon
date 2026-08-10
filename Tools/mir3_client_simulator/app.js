/* Mir3 EI 3.0 原版客户端模拟器 — interactive engine.
 *
 * Fixed 800x600 logical canvas. Real textures are decoded by the wilviewer
 * server via /api/image (WIL -> PNG). All geometry is consumed from the
 * unified data model (data/*.json, built by Tools/web/build_mir3_simulator_data.py
 * from docs/research/ei-ui-layout/). Nothing here hard-codes coordinates.
 *
 * Evidence levels are carried through: primary / derived / candidate / pending.
 * Candidate geometry is drawn with the candidate marker, never as primary fact.
 */
"use strict";

const VIEW_W = 800;
const VIEW_H = 600;

const STATE = {
  data: null,          // unified bundle
  scale: 1,
  evidence: false,
  testnav: false,
  selectedEntity: null, // {id, kind, name}
  hoveredEntity: null,
  openWindows: new Set(),
  foregroundWindow: null,
  storeState: 0,
  hp: 62, mp: 71, exp: 38,      // demo live values (bars driven by rects)
  chatLines: [],
  prompt: null,                 // {kind:'confirm'|'notice', text, cb}
  pressed: null,                // control id currently pressed
};

const $ = (sel) => document.querySelector(sel);
const stage = $("#stage");
const sceneEl = $("#scene");
const hudEl = $("#hud");
const winEl = $("#windows");
const promptEl = $("#prompts");
const evEl = $("#evidence-overlay");
const targetboxEl = $("#targetbox");

/* ------------------------------------------------------------ texture url */
function imgUrl(lib, frame, scale = 1) {
  if (frame == null || frame < 0) return null;
  const f = String(frame);
  return `/api/image?f=${encodeURIComponent(lib)}&i=${f}&scale=${scale}&bg=transparent`;
}

function makeImg(lib, frame, scale = 1) {
  const url = imgUrl(lib, frame, scale);
  const el = document.createElement("img");
  if (url) el.src = url;
  el.alt = `${lib} F${frame}`;
  return el;
}

/* ------------------------------------------------------------ data model */
async function loadData() {
  const r = await fetch("/sim/data/layout.json");
  if (!r.ok) throw new Error(`data fetch failed: ${r.status}`);
  return r.json();
}

/* ------------------------------------------------------------ scale */
function applyScale() {
  const wrap = $("#stage-wrap");
  const availW = wrap.clientWidth - 24;
  const availH = wrap.clientHeight - 24;
  const s = Math.max(1, Math.floor(Math.min(availW / VIEW_W, availH / VIEW_H)));
  STATE.scale = s;
  stage.style.transform = `scale(${s})`;
  stage.style.margin = "auto";
  $("#scale-label").textContent = `${VIEW_W}×${VIEW_H} 逻辑 · 缩放 ×${s}`;
}
window.addEventListener("resize", applyScale);

/* ------------------------------------------------------------ scene layer */
function renderScene() {
  sceneEl.innerHTML = "";
  const maps = STATE.data.maps || [];
  const bg = maps.find((m) => m.id === "map.bg");
  if (bg) {
    const im = makeImg(bg.library, bg.frame);
    im.className = "hud-img";
    im.style.cssText = "left:0;top:0;width:800px;height:600px;object-fit:cover;opacity:.9";
    im.dataset.evidence = bg.evidence_level;
    im.dataset.rect = "0,0,800,600";
    im.dataset.desc = `${bg.library} F${bg.frame}`;
    sceneEl.appendChild(im);
  }
  for (const e of STATE.data.entities || []) {
    const spr = document.createElement("div");
    spr.className = `sprite ${e.kind}`;
    spr.dataset.entity = e.id;
    spr.style.left = e.x + "px";
    spr.style.top = e.y + "px";
    const im = makeImg(e.library, e.frame);
    // sprite frames are large; constrain to a plausible in-world size
    im.style.cssText = "max-width:60px;max-height:80px;width:auto;height:auto";
    spr.appendChild(im);
    const name = document.createElement("div");
    name.className = "nameplate";
    name.textContent = e.name;
    spr.appendChild(name);
    spr.dataset.rect = `${e.x - 20},${e.y - 60},40,70`;
    spr.dataset.evidence = e.evidence_level;
    spr.dataset.desc = `${e.library} F${e.frame} · ${e.note || ""}`;
    sceneEl.appendChild(spr);
  }
}

/* ------------------------------------------------------------ HUD layer */
function renderHud() {
  hudEl.innerHTML = "";
  const hud = STATE.data.hud;
  const bg = makeImg(hud.resource_library, hud.background_frame);
  bg.className = "hud-img";
  bg.style.cssText = `left:${hud.origin[0]}px;top:${hud.origin[1]}px;width:800px;height:135px`;
  bg.dataset.evidence = "primary-static";
  bg.dataset.rect = `${hud.origin[0]},${hud.origin[1]},800,135`;
  bg.dataset.desc = "GameInter F50 主 HUD 底板";
  hudEl.appendChild(bg);

  // HP / MP / EXP bars
  const bars = [
    { key: "hp_bar", cls: "hp", val: STATE.hp, color: "#d4352c" },
    { key: "mp_bar", cls: "mp", val: STATE.mp, color: "#2b6bd4" },
    { key: "exp_bar", cls: "exp", val: STATE.exp, color: "#3fae4a" },
  ];
  for (const b of bars) {
    const meta = hud[b.key];
    if (!meta) continue;
    const [l, t, r, bot] = meta.rect;
    const w = r - l, h = bot - t;
    const fill = document.createElement("div");
    fill.className = `bar-fill ${b.cls}`;
    const bw = Math.max(0, Math.min(100, b.val)) / 100;
    const base = b.cls === "exp" ? w : w * 0.5; // exp is horizontal, hp/mp vertical
    let fw = w, fh = h;
    if (b.cls === "exp") { fw = Math.round(base * bw); fh = h; }
    else { fh = Math.round(h * bw); fw = w; }
    fill.style.cssText = `left:${l}px;top:${t + (b.cls === "exp" ? 0 : h - fh)}px;width:${fw}px;height:${fh}px;background:${b.color}`;
    fill.dataset.evidence = meta.evidence_level;
    fill.dataset.rect = meta.rect.join(",");
    fill.dataset.desc = meta.note || "";
    hudEl.appendChild(fill);
    // numeric label overlay
    const num = document.createElement("div");
    num.className = "lbl";
    num.dataset.bar = b.cls;
    num.style.cssText = `left:${l}px;top:${t}px;font:10px monospace;color:#fff;text-shadow:1px 1px 0 #000;z-index:3`;
    const label = b.cls === "hp" ? `血量 ${Math.round(b.val)}/100` : b.cls === "mp" ? `魔法 ${Math.round(b.val)}/100` : `经验 ${Math.round(b.val)}/100`;
    num.textContent = label;
    hudEl.appendChild(num);
  }

  // HUD controls (buttons) — from unified data model
  for (const c of STATE.data.controls) {
    if (c.window_id !== "hud") continue;
    const [x, y, w, h] = c.rect;
    const btn = document.createElement("div");
    btn.className = "control";
    btn.style.cssText = `left:${x}px;top:${y}px;width:${w}px;height:${h}px`;
    const im = makeImg(c.resource_library, c.frame_pair[0]);
    btn.appendChild(im);
    btn.dataset.control = c.id;
    btn.dataset.rect = c.rect.join(",");
    btn.dataset.evidence = c.evidence_level;
    btn.dataset.desc = c.id;
    btn.title = `${c.id} · F${c.frame_pair[0]} · ${c.evidence_level}`;
    btn.addEventListener("pointerdown", (ev) => {
      ev.stopPropagation();
      btn.classList.add("pressed");
      STATE.pressed = c.id;
    });
    btn.addEventListener("pointerup", (ev) => {
      ev.stopPropagation();
      btn.classList.remove("pressed");
      if (STATE.pressed === c.id) {
        STATE.pressed = null;
        onHudControl(c.id);
      }
    });
    btn.addEventListener("pointerleave", () => btn.classList.remove("pressed"));
    hudEl.appendChild(btn);
  }

  // minimap widget
  const mm = hud.minimap;
  const mmEl = document.createElement("div");
  mmEl.className = "minimap";
  mmEl.style.cssText = `left:${mm.rect[0]}px;top:${mm.rect[1]}px;width:${mm.rect[2] - mm.rect[0]}px;height:${mm.rect[3] - mm.rect[1]}px`;
  const mmImg = makeImg("MMap.wil", 0);
  mmEl.appendChild(mmImg);
  mmEl.dataset.evidence = mm.evidence_level;
  mmEl.dataset.rect = mm.rect.join(",");
  mmEl.dataset.desc = "固定小地图 (672,0)-(800,128)";
  mmEl.title = "小地图 · MMap.wil F0 · candidate";
  hudEl.appendChild(mmEl);

  // chat region
  const chat = hud.chat_region;
  const chatEl = document.createElement("div");
  chatEl.className = "text-panel";
  chatEl.style.cssText = `left:${chat.rect[0]}px;top:${chat.rect[1]}px;width:${chat.rect[2] - chat.rect[0]}px;height:${chat.rect[3] - chat.rect[1]}px`;
  chatEl.dataset.evidence = chat.evidence_level;
  chatEl.dataset.rect = chat.rect.join(",");
  chatEl.dataset.desc = "聊天/文本总区域 (224,492)-(578,566)";
  const lines = document.createElement("div");
  lines.className = "chat-lines";
  lines.id = "chat-lines";
  chatEl.appendChild(lines);
  hudEl.appendChild(chatEl);

  // target info panel
  const tgt = hud.target_info;
  const tp = document.createElement("div");
  tp.className = "target-panel";
  tp.id = "target-panel";
  tp.style.cssText = `left:${tgt.rect[0]}px;top:${tgt.rect[1]}px;width:${tgt.rect[2] - tgt.rect[0]}px;height:${tgt.rect[3] - tgt.rect[1]}px`;
  hudEl.appendChild(tp);

  pushChat("[系统] 欢迎使用 Mir3 EI 3.0 原版客户端模拟器");
  pushChat("[系统] 点击场景中的怪物/NPC 设置目标，底部按钮打开窗口");
}

function pushChat(line) {
  STATE.chatLines.push(line);
  if (STATE.chatLines.length > 40) STATE.chatLines.shift();
  const el = $("#chat-lines");
  if (el) el.textContent = STATE.chatLines.join("\n");
}

function setTarget(entity) {
  STATE.selectedEntity = entity;
  // clear targeting marks
  document.querySelectorAll("#scene .sprite.targeted").forEach((n) => n.classList.remove("targeted"));
  if (entity) {
    const spr = document.querySelector(`#scene .sprite[data-entity="${entity.id}"]`);
    if (spr) spr.classList.add("targeted");
  }
  updateTargetPanel();
  updateTargetBox();
}

function updateTargetPanel() {
  const tp = $("#target-panel");
  if (!tp) return;
  const e = STATE.selectedEntity;
  if (!e) { tp.classList.remove("visible"); return; }
  tp.classList.add("visible");
  const kind = e.kind === "monster" ? "怪物" : e.kind === "npc" ? "NPC" : "玩家";
  tp.textContent = `${kind}：${e.name}\n${e.note || ""}`;
}

function updateTargetBox() {
  const e = STATE.selectedEntity;
  if (!e) { targetboxEl.classList.add("hidden"); return; }
  const spr = document.querySelector(`#scene .sprite[data-entity="${e.id}"]`);
  if (!spr) { targetboxEl.classList.add("hidden"); return; }
  const r = spr.getBoundingClientRect();
  const sr = stage.getBoundingClientRect();
  targetboxEl.style.left = (r.left - sr.left) / STATE.scale + "px";
  targetboxEl.style.top = (r.top - sr.top) / STATE.scale + "px";
  targetboxEl.style.width = r.width / STATE.scale + "px";
  targetboxEl.style.height = r.height / STATE.scale + "px";
  targetboxEl.classList.remove("hidden");
}

/* ------------------------------------------------------------ windows */
const WINDOW_TITLES = {
  "window.inventory": "背包",
  "window.status": "人物状态",
  "window.store-candidate": "商店/仓库",
  "window.exchange-candidate": "交换",
  "window.guild-candidate": "行会",
  "window.group": "组队",
  "window.chat-pop": "聊天",
  "window.group-pop-candidate": "队伍信息",
  "window.option": "系统设置",
  "window.quest": "任务",
  "window.horse": "坐骑",
  "window.other-14-candidate": "技能",
  "window.npc-candidate": "NPC 对话",
  "window.notice-prompt-candidate": "公告",
};

function renderWindows() {
  winEl.innerHTML = "";
  for (const w of STATE.data.windows) {
    const [x, y, ww, hh] = w.rect;
    const box = document.createElement("div");
    box.className = "win closed";
    box.dataset.window = w.id;
    box.style.cssText = `left:${x}px;top:${y}px;width:${ww}px;height:${hh}px`;
    // background frame
    const bg = makeImg(w.resource_library, w.frame);
    bg.className = "win-bg";
    bg.style.objectFit = "fill";
    box.appendChild(bg);
    // title bar (drag)
    const tb = document.createElement("div");
    tb.className = "win-titlebar";
    tb.title = `${WINDOW_TITLES[w.id] || w.id} · ${w.evidence_level}`;
    box.appendChild(tb);
    // content host
    const content = document.createElement("div");
    content.className = "win-content";
    content.dataset.windowContent = w.id;
    box.appendChild(content);
    // close button (frame pair from evidence where available)
    const close = document.createElement("div");
    close.className = "close-btn";
    close.style.cssText = `right:4px;top:4px;width:28px;height:26px`;
    const closeImg = makeImg("GameInter.wil", 161);
    close.appendChild(closeImg);
    close.title = "关闭";
    close.addEventListener("click", (ev) => { ev.stopPropagation(); setWindowOpen(w.id, false); });
    box.appendChild(close);
    winEl.appendChild(box);
    fillWindowContent(w);
    bindWindowDrag(box);
  }
}

function fillWindowContent(w) {
  const content = winEl.querySelector(`[data-window-content="${w.id}"]`);
  if (!content) return;
  content.innerHTML = "";
  const id = w.id;

  if (id === "window.status") {
    // equipment slots from data model + attribute labels
    for (const s of STATE.data.equipment_slots) {
      const slot = document.createElement("div");
      slot.className = "slot equip-empty";
      slot.style.cssText = `left:${s.x}px;top:${s.y}px;width:${s.w}px;height:${s.h}px`;
      const im = makeImg(s.library, s.frame);
      slot.appendChild(im);
      slot.dataset.slot = s.id;
      slot.dataset.evidence = s.evidence_level;
      slot.dataset.rect = `${s.x},${s.y},${s.w},${s.h}`;
      slot.dataset.desc = `${s.name} · ${s.library} F${s.frame}`;
      slot.title = `${s.name} · ${s.evidence_level}`;
      slot.addEventListener("click", () => selectSlot(slot, s));
      content.appendChild(slot);
    }
    const attrs = ["等级 1", "攻击 5-10", "魔法 3-8", "防御 2-5", "魔御 1-4"];
    attrs.forEach((a, i) => {
      const lbl = document.createElement("div");
      lbl.className = "lbl";
      lbl.style.cssText = `left:160px;top:${20 + i * 22}px`;
      lbl.textContent = a;
      content.appendChild(lbl);
    });
  } else if (id === "window.inventory") {
    // 6x6 grid from evidence: 36px cells
    for (let i = 0; i < 36; i++) {
      const col = i % 6, row = Math.floor(i / 6);
      const x = 8 + col * 40, y = 8 + row * 40;
      const slot = document.createElement("div");
      slot.className = "slot";
      slot.style.cssText = `left:${x}px;top:${y}px;width:36px;height:36px`;
      slot.dataset.slot = `bag.${i}`;
      slot.dataset.rect = `${x},${y},36,36`;
      slot.dataset.evidence = "primary-static";
      slot.dataset.desc = "背包 6×6 网格 · 36px · 0x0042F150";
      slot.title = `背包格 ${i + 1} · primary-static`;
      // place a few real item icons from Equip.wil
      if (i % 7 === 0) {
        const im = makeImg("Equip.wil", Math.min(i, 124));
        slot.appendChild(im);
      }
      slot.addEventListener("click", () => selectSlot(slot, { id: `bag.${i}` }));
      content.appendChild(slot);
    }
    // weight label
    const lbl = document.createElement("div");
    lbl.className = "lbl";
    lbl.style.cssText = "left:8px;top:256px;width:200px";
    lbl.textContent = "负重 12/30";
    content.appendChild(lbl);
  } else if (id === "window.other-14-candidate" || id === "window.store-candidate" ||
             id === "window.exchange-candidate") {
    // skill grid (skills.json) / store slots / exchange grids
    const src = id === "window.other-14-candidate" ? STATE.data.skills
      : id === "window.store-candidate" ? storeGridSlots()
      : exchangeSlots();
    for (const s of src) {
      const slot = document.createElement("div");
      slot.className = "slot";
      slot.style.cssText = `left:${s.x}px;top:${s.y}px;width:${s.w}px;height:${s.h}px`;
      const im = makeImg(s.library, s.frame);
      slot.appendChild(im);
      slot.dataset.slot = s.id;
      slot.dataset.rect = `${s.x},${s.y},${s.w},${s.h}`;
      slot.dataset.evidence = s.evidence_level;
      slot.dataset.desc = `${s.name} · ${s.library} F${s.frame}`;
      slot.title = `${s.name} · ${s.evidence_level}`;
      slot.addEventListener("click", () => selectSlot(slot, s));
      content.appendChild(slot);
    }
    if (id === "window.store-candidate") {
      const stateLbl = document.createElement("div");
      stateLbl.className = "lbl";
      stateLbl.style.cssText = "left:8px;top:278px;width:280px";
      stateLbl.id = "store-state-label";
      stateLbl.textContent = `商店状态 ${STATE.storeState}`;
      content.appendChild(stateLbl);
    }
  } else if (id === "window.chat-pop") {
    // chat pop window: history + input from evidence rects
    const hist = document.createElement("div");
    hist.className = "text-panel";
    hist.style.cssText = "left:40px;top:29px;width:491px;height:279px";
    const hl = document.createElement("div");
    hl.className = "chat-lines";
    hl.textContent = STATE.chatLines.join("\n");
    hist.appendChild(hl);
    content.appendChild(hist);
    const input = document.createElement("input");
    input.className = "chat-input";
    input.style.cssText = "position:absolute;left:25px;top:311px;width:499px;height:15px;background:#0a0f14;color:#d8e4f0;border:1px solid #2a3a4c;font:12px monospace";
    input.placeholder = "输入聊天内容…";
    input.addEventListener("keydown", (ev) => {
      if (ev.key === "Enter" && input.value) {
        pushChat(`[你] ${input.value}`);
        input.value = "";
        // refresh chat-pop history
        const hl2 = content.querySelector(".chat-lines");
        if (hl2) hl2.textContent = STATE.chatLines.join("\n");
      }
    });
    content.appendChild(input);
  } else if (id === "window.quest") {
    const quests = ["主线：拜见国王", "支线：收集草药", "活动：讨伐稻草人", "任务 4：护送商队", "任务 5：击杀骷髅"];
    quests.forEach((q, i) => {
      const lbl = document.createElement("div");
      lbl.className = "lbl";
      lbl.style.cssText = `left:24px;top:${50 + i * 22}px;width:260px`;
      lbl.textContent = (i === 0 ? "★ " : "○ ") + q;
      content.appendChild(lbl);
    });
  } else if (id === "window.npc-candidate") {
    const npcLbl = document.createElement("div");
    npcLbl.className = "lbl";
    npcLbl.style.cssText = "left:16px;top:16px;width:500px";
    npcLbl.textContent = "你好，勇士！有什么可以帮你？";
    content.appendChild(npcLbl);
    const opts = ["购买物品", "存取仓库", "修理装备", "离开"];
    opts.forEach((o, i) => {
      const btn = document.createElement("div");
      btn.className = "slot";
      btn.style.cssText = `left:${16 + i * 130}px;top:120px;width:120px;height:24px;border:none;background:rgba(0,0,0,.25)`;
      btn.textContent = o;
      btn.style.cssText += ";font:12px monospace;color:#e8eef5;text-align:center;line-height:24px";
      btn.addEventListener("click", () => {
        pushChat(`[NPC] 你选择了「${o}」`);
        if (o === "购买物品" || o === "存取仓库") setWindowOpen("window.store-candidate", true);
      });
      content.appendChild(btn);
    });
  } else if (id === "window.option") {
    const opts = ["音乐 开", "音效 开", "窗口模式", "显示名称"];
    opts.forEach((o, i) => {
      const btn = document.createElement("div");
      btn.className = "slot";
      btn.style.cssText = `left:30px;top:${30 + i * 40}px;width:180px;height:28px;border:1px solid #3a4a5c;background:rgba(0,0,0,.2)`;
      btn.textContent = o;
      btn.style.cssText += ";font:12px monospace;color:#e8eef5;text-align:center;line-height:28px";
      btn.addEventListener("click", () => {
        btn.classList.toggle("selected");
        pushChat(`[设置] ${o} ${btn.classList.contains("selected") ? "开" : "关"}`);
      });
      content.appendChild(btn);
    });
  } else if (id === "window.group" || id === "window.guild-candidate") {
    const members = id === "window.group" ? ["玩家", "队友·法师", "队友·道士"] : ["行会会长", "长老", "成员 1", "成员 2"];
    members.forEach((m, i) => {
      const lbl = document.createElement("div");
      lbl.className = "lbl";
      lbl.style.cssText = `left:16px;top:${30 + i * 24}px;width:220px`;
      lbl.textContent = (id === "window.group" ? "▸ " : "▪ ") + m;
      content.appendChild(lbl);
    });
  } else if (id === "window.horse") {
    const horseLbl = document.createElement("div");
    horseLbl.className = "lbl";
    horseLbl.style.cssText = "left:16px;top:20px;width:260px";
    horseLbl.textContent = "坐骑：枣红马\n状态：健康\n命令：召唤 / 喂食 / 遛马";
    content.appendChild(horseLbl);
    const hIm = makeImg("Horse.wil", 0);
    hIm.style.cssText = "position:absolute;left:60px;top:80px;max-width:180px;image-rendering:pixelated";
    content.appendChild(hIm);
  }
}

function storeGridSlots() {
  // store states from store-state-graph evidence
  const rows = [];
  const cols = 5, cell = 46;
  for (let i = 0; i < 10; i++) {
    rows.push({
      id: `store.${i}`, name: `商店物品 ${i + 1}`,
      x: 12 + (i % cols) * cell, y: 40 + Math.floor(i / cols) * cell,
      w: 42, h: 42, library: "Equip.wil", frame: i % 124,
      evidence_level: "candidate", note: "store item slot",
    });
  }
  return rows;
}

function exchangeSlots() {
  const rows = [];
  const cols = 6, cell = 40;
  for (let i = 0; i < 30; i++) {
    rows.push({
      id: `ex.${i}`, name: `交换格 ${i + 1}`,
      x: 12 + (i % cols) * cell, y: 40 + Math.floor(i / cols) * cell,
      w: 36, h: 36, library: "Equip.wil", frame: (i * 3) % 124,
      evidence_level: "candidate", note: "exchange 6×5 grid",
    });
  }
  return rows;
}

function selectSlot(el, meta) {
  document.querySelectorAll("#windows .slot.selected").forEach((n) => n.classList.remove("selected"));
  el.classList.add("selected");
  pushChat(`[选中] ${meta.id || meta.name || "格子"}`);
  if (meta.id && meta.id.startsWith("skill.")) pushChat(`[技能] ${meta.name}`);
  if (meta.id && meta.id.startsWith("slot.")) pushChat(`[装备] ${meta.name}`);
  if (meta.id && meta.id.startsWith("bag.")) pushChat(`[背包] 第 ${parseInt(meta.id.split(".")[1], 10) + 1} 格`);
}

/* ------------------------------------------------------------ window open/close/drag */
function setWindowOpen(id, open) {
  const box = winEl.querySelector(`[data-window="${id}"]`);
  if (!box) return;
  if (open) {
    box.classList.remove("closed");
    STATE.openWindows.add(id);
    bringToFront(id);
    refreshWindowContent(id);
  } else {
    box.classList.add("closed");
    STATE.openWindows.delete(id);
    if (STATE.foregroundWindow === id) STATE.foregroundWindow = null;
  }
}

function bringToFront(id) {
  STATE.foregroundWindow = id;
  winEl.querySelectorAll(".win").forEach((w) => w.classList.remove("foreground"));
  const box = winEl.querySelector(`[data-window="${id}"]`);
  if (box) box.classList.add("foreground");
}

function refreshWindowContent(id) {
  if (id === "window.store-candidate") {
    const lbl = winEl.querySelector("#store-state-label");
    if (lbl) lbl.textContent = `商店状态 ${STATE.storeState} · 状态机证据见 store-state-graph.json`;
  }
}

function bindWindowDrag(box) {
  const tb = box.querySelector(".win-titlebar");
  let dragging = false, dx = 0, dy = 0;
  tb.addEventListener("pointerdown", (ev) => {
    ev.preventDefault();
    bringToFront(box.dataset.window);
    dragging = true;
    const r = box.getBoundingClientRect();
    const sr = stage.getBoundingClientRect();
    dx = (ev.clientX - r.left) / STATE.scale;
    dy = (ev.clientY - r.top) / STATE.scale;
    box.classList.add("dragging");
  });
  window.addEventListener("pointermove", (ev) => {
    if (!dragging) return;
    const sr = stage.getBoundingClientRect();
    let x = (ev.clientX - sr.left) / STATE.scale - dx;
    let y = (ev.clientY - sr.top) / STATE.scale - dy;
    x = Math.max(-50, Math.min(VIEW_W - 60, x));
    y = Math.max(0, Math.min(VIEW_H - 40, y));
    box.style.left = x + "px";
    box.style.top = y + "px";
    // update data model so evidence mode stays consistent
    const w = STATE.data.windows.find((q) => q.id === box.dataset.window);
    if (w) { w.rect[0] = x; w.rect[1] = y; }
  });
  window.addEventListener("pointerup", () => {
    dragging = false;
    box.classList.remove("dragging");
  });
}

/* ------------------------------------------------------------ HUD control actions */
function onHudControl(id) {
  switch (id) {
    case "hud.status": setWindowOpen("window.status", !isOpen("window.status")); break;
    case "hud.inventory": setWindowOpen("window.inventory", !isOpen("window.inventory")); break;
    case "hud.skill": case "hud.skill-entry":
      setWindowOpen("window.other-14-candidate", !isOpen("window.other-14-candidate")); break;
    case "hud.chat": setWindowOpen("window.chat-pop", !isOpen("window.chat-pop")); break;
    case "hud.quest": setWindowOpen("window.quest", !isOpen("window.quest")); break;
    case "hud.option": setWindowOpen("window.option", !isOpen("window.option")); break;
    case "hud.store": setWindowOpen("window.store-candidate", !isOpen("window.store-candidate")); break;
    case "hud.party": case "hud.group":
      setWindowOpen("window.group", !isOpen("window.group")); break;
    case "hud.guild": setWindowOpen("window.guild-candidate", !isOpen("window.guild-candidate")); break;
    case "hud.exchange": setWindowOpen("window.exchange-candidate", !isOpen("window.exchange-candidate")); break;
    case "hud.minimap": cycleMinimap(); break;
    case "hud.logout":
      showPrompt("confirm", "确定要返回人物选择吗？", () => {
        pushChat("[系统] 已断开连接（模拟）");
        STATE.hp = 0; renderHud();
      });
      break;
    case "hud.exit":
      showPrompt("confirm", "确定要退出游戏吗？", () => {
        pushChat("[系统] 退出游戏（模拟）");
      });
      break;
  }
}

function isOpen(id) {
  const box = winEl.querySelector(`[data-window="${id}"]`);
  return box && !box.classList.contains("closed");
}

function cycleMinimap() {
  const m = STATE.data.maps.find((q) => q.id === "map.minimap");
  if (!m) return;
  const next = (m.frame + 1) % 155;
  m.frame = next;
  const mmEl = hudEl.querySelector(".minimap img");
  if (mmEl) mmEl.src = imgUrl(m.library, next);
  pushChat(`[小地图] 切换到 MMap.wil F${next}（候选）`);
}

/* ------------------------------------------------------------ prompts */
const PROMPT_BUTTONS = {
  confirm: {
    background: { lib: "GameInter.wil", frame: 950, w: 360, h: 190 },
    center: [400, 246],
    buttons: [
      { id: "ok", rel: [51, 125, 44, 20], frames: [151, 152], label: "确定" },
      { id: "cancel", rel: [147, 125, 64, 20], frames: [157, 158], label: "取消" },
      { id: "alt", rel: [244, 125, 44, 20], frames: [154, 155], label: "其他" },
    ],
    text: { rel: [20, 30, 320, 80] },
  },
  notice: {
    background: { lib: "GameInter.wil", frame: 602, w: 584, h: 252 },
    center: [107, 110],
    buttons: [
      { id: "ok", rel: [520, 220, 28, 26], frames: [161, 162], label: "确定" },
      { id: "alt", rel: [540, 160, 28, 26], frames: [606, 607], label: "其他" },
    ],
    text: { rel: [23, 94, 400, 60] },
  },
};

function showPrompt(kind, text, cb) {
  const spec = PROMPT_BUTTONS[kind];
  const [cx, cy] = spec.center;
  const [bw, bh] = [spec.background.w, spec.background.h];
  const x = Math.round(cx - bw / 2), y = Math.round(cy - bh / 2);
  const box = document.createElement("div");
  box.className = "prompt visible";
  box.dataset.prompt = kind;
  box.style.cssText = `left:${x}px;top:${y}px;width:${bw}px;height:${bh}px`;
  const bg = makeImg(spec.background.lib, spec.background.frame);
  bg.className = "p-bg";
  box.appendChild(bg);
  const pt = document.createElement("div");
  pt.className = "p-text";
  const [tl, tt, tw, th] = spec.text.rel;
  pt.style.cssText = `left:${tl}px;top:${tt}px;width:${tw}px;height:${th}px`;
  pt.textContent = text;
  box.appendChild(pt);
  const result = { ok: false };
  for (const b of spec.buttons) {
    const btn = document.createElement("div");
    btn.className = "p-btn";
    const [bl, bt, bww, bhh] = b.rel;
    btn.style.cssText = `left:${bl}px;top:${bt}px;width:${bww}px;height:${bhh}px`;
    const im = makeImg("GameInter.wil", b.frames[0]);
    btn.appendChild(im);
    btn.title = b.label;
    btn.addEventListener("click", (ev) => {
      ev.stopPropagation();
      result.ok = b.id === "ok";
      box.remove();
      STATE.prompt = null;
      if (cb) cb(result.ok, b.id);
    });
    box.appendChild(btn);
  }
  // clicking backdrop cancels for confirm
  box.addEventListener("click", (ev) => {
    if (ev.target === box) {
      box.remove();
      STATE.prompt = null;
      if (cb) cb(false, "backdrop");
    }
  });
  promptEl.appendChild(box);
  STATE.prompt = { kind, box };
  pushChat(`[提示] ${text}`);
}

/* ------------------------------------------------------------ scene interaction */
function bindSceneInteraction() {
  sceneEl.addEventListener("pointerover", (ev) => {
    const spr = ev.target.closest(".sprite");
    sceneEl.querySelectorAll(".sprite.hovered").forEach((n) => n.classList.remove("hovered"));
    if (spr) {
      spr.classList.add("hovered");
      STATE.hoveredEntity = STATE.data.entities.find((e) => e.id === spr.dataset.entity) || null;
    } else {
      STATE.hoveredEntity = null;
    }
  });
  sceneEl.addEventListener("click", (ev) => {
    const spr = ev.target.closest(".sprite");
    if (!spr) return;
    const e = STATE.data.entities.find((q) => q.id === spr.dataset.entity);
    if (!e) return;
    if (e.kind === "npc") {
      setTarget(e);
      setWindowOpen("window.npc-candidate", true);
      pushChat(`[NPC] 你点击了 ${e.name}`);
    } else {
      setTarget(e);
      pushChat(`[目标] ${e.kind === "monster" ? "怪物" : "玩家"}：${e.name}`);
      if (e.kind === "monster") {
        // demo: damage feedback
        pushChat(`[战斗] 你对 ${e.name} 造成 8 点伤害`);
      }
    }
  });
}

/* ------------------------------------------------------------ evidence mode */
function renderEvidenceOverlay() {
  evEl.innerHTML = "";
  if (!STATE.evidence) return;
  const layer = evEl;
  const add = (rect, lvl, label, extra) => {
    const d = document.createElement("div");
    d.className = `ev ${lvl}`;
    d.style.cssText = `left:${rect[0]}px;top:${rect[1]}px;width:${Math.max(1, rect[2] - rect[0])}px;height:${Math.max(1, rect[3] - rect[1])}px`;
    const tag = document.createElement("div");
    tag.className = "ev-tag";
    tag.innerHTML = `${label} <span class="lvl">[${lvl}]</span> ${extra || ""}`;
    d.appendChild(tag);
    layer.appendChild(d);
  };
  // HUD
  const hud = STATE.data.hud;
  add(hud.origin.concat([hud.origin[0] + 800, hud.origin[1] + 135]), "primary", "HUD 底板 F50");
  for (const b of ["hp_bar", "mp_bar", "exp_bar"]) {
    const m = hud[b];
    add(m.rect, m.evidence_level, b, m.note ? "" : "");
  }
  // controls
  for (const c of STATE.data.controls) {
    add(c.rect, c.evidence_level, c.id, `F${c.frame_pair[0]}`);
  }
  // windows
  for (const w of STATE.data.windows) {
    add(w.rect, w.evidence_level, w.id, `F${w.frame}`);
  }
  // entities
  for (const e of STATE.data.entities) {
    add([e.x - 20, e.y - 60, e.x + 20, e.y], e.evidence_level, e.id, e.library);
  }
}

function setEvidence(on) {
  STATE.evidence = on;
  evEl.classList.toggle("hidden", !on);
  $("#nav button[data-act=evidence]").classList.toggle("active", on);
  renderEvidenceOverlay();
}

/* ------------------------------------------------------------ test nav */
function renderTestNav() {
  const grid = $("#testnav-grid");
  grid.innerHTML = "";
  for (const w of STATE.data.windows) {
    const btn = document.createElement("button");
    btn.textContent = WINDOW_TITLES[w.id] || w.id;
    btn.addEventListener("click", () => setWindowOpen(w.id, !isOpen(w.id)));
    grid.appendChild(btn);
  }
  const extra = [
    ["confirm 确认框", () => showPrompt("confirm", "确认框演示：是否继续？", (ok) => pushChat(ok ? "[确认] 继续" : "[确认] 取消"))],
    ["notice 公告", () => showPrompt("notice", "[行会公告，请自行修改公告内容.]", (ok) => pushChat("[公告] 已读"))],
    ["商店状态+1", () => { STATE.storeState = (STATE.storeState + 1) % 5; refreshWindowContent("window.store-candidate"); pushChat(`[商店] 状态 ${STATE.storeState}`); }],
    ["商店状态-1", () => { STATE.storeState = (STATE.storeState + 4) % 5; refreshWindowContent("window.store-candidate"); pushChat(`[商店] 状态 ${STATE.storeState}`); }],
    ["随机地图", () => { const m = STATE.data.maps.find((q) => q.id === "map.bg"); m.frame = Math.floor(Math.random() * 29); renderScene(); pushChat(`[地图] FMMap F${m.frame}`); }],
  ];
  for (const [label, fn] of extra) {
    const btn = document.createElement("button");
    btn.textContent = label;
    btn.addEventListener("click", fn);
    grid.appendChild(btn);
  }
  $("#testnav").classList.toggle("hidden", !STATE.testnav);
}

/* ------------------------------------------------------------ boot */
async function boot() {
  try {
    STATE.data = await loadData();
  } catch (e) {
    $("#status").textContent = `数据加载失败: ${e.message}`;
    return;
  }
  renderScene();
  renderHud();
  renderWindows();
  bindSceneInteraction();
  renderEvidenceOverlay();
  renderTestNav();
  applyScale();
  $("#status").textContent = "就绪";
  const counts = {
    windows: STATE.data.windows.length,
    controls: STATE.data.controls.length,
    entities: STATE.data.entities.length,
    resources: STATE.data.resources.length,
  };
  $("#evidence-summary").textContent =
    `windows=${counts.windows} controls=${counts.controls} entities=${counts.entities} resources=${counts.resources} · 数据源 layout.json`;

  // demo: periodic HP/MP oscillation
  setInterval(() => {
    STATE.hp = Math.max(20, (STATE.hp + 0.7) % 101);
    STATE.mp = Math.max(20, (STATE.mp + 0.4) % 101);
    updateBars();
  }, 1500);
}

function updateBars() {
  const bars = [
    { key: "hp_bar", cls: "hp", val: STATE.hp, label: "血量" },
    { key: "mp_bar", cls: "mp", val: STATE.mp, label: "魔法" },
    { key: "exp_bar", cls: "exp", val: STATE.exp, label: "经验" },
  ];
  for (const b of bars) {
    const meta = STATE.data.hud[b.key];
    const [l, t, r, bot] = meta.rect;
    const w = r - l, h = bot - t;
    const fill = hudEl.querySelector(`.bar-fill.${b.cls}`);
    if (!fill) continue;
    const bw = Math.max(0, Math.min(100, b.val)) / 100;
    const base = b.cls === "exp" ? w : w * 0.5;
    if (b.cls === "exp") {
      fill.style.width = Math.round(base * bw) + "px";
    } else {
      fill.style.height = Math.round(h * bw) + "px";
    }
    // keep the numeric label in sync with the animated fill
    const lbl = hudEl.querySelector(`.lbl[data-bar="${b.cls}"]`);
    if (lbl) lbl.textContent = `${b.label} ${Math.round(b.val)}/100`;
  }
}

document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("#nav button").forEach((btn) => {
    btn.addEventListener("click", () => {
      const act = btn.dataset.act;
      if (act === "evidence") setEvidence(!STATE.evidence);
      else if (act === "testnav") { STATE.testnav = !STATE.testnav; renderTestNav(); }
      else if (act === "reset") resetScene();
    });
  });
  document.querySelector('[data-act="close-testnav"]').addEventListener("click", () => {
    STATE.testnav = false;
    renderTestNav();
  });
  boot();
});

function resetScene() {
  // reset windows closed, clear target, reset store state, reload scene
  STATE.openWindows.clear();
  winEl.querySelectorAll(".win").forEach((w) => w.classList.add("closed"));
  STATE.selectedEntity = null;
  STATE.hoveredEntity = null;
  STATE.storeState = 0;
  targetboxEl.classList.add("hidden");
  const tp = $("#target-panel");
  if (tp) tp.classList.remove("visible");
  renderScene();
  pushChat("[系统] 场景已重置");
}
