# Đạo cụ phương Tây cho "Blackwood Manor" — dùng lại helper của khoahon_props.py.
#   blender.exe --background --factory-startup --python Tools/Blender/blackwood_props.py -- <output_dir> [TênModel...] [--preview]
# Xuất FBX (mét, Y-up) tên KH_BW_*; converter Unity tự nhập từ Tools/Blender/out.
import bpy, math, os, sys, random

sys.argv_backup = list(sys.argv)
HERE = os.path.dirname(os.path.abspath(__file__))
# Nạp helper (reset, mat, add, lathe, join, bevel, smooth, export, M, render_preview) mà không chạy phần main
_src = open(os.path.join(HERE, "khoahon_props.py"), encoding="utf-8").read()
_src = _src.split("\nonly = set(")[0]
exec(compile(_src, "khoahon_props_helpers", "exec"))


def apply_all(o):
    bpy.context.view_layer.objects.active = o
    for md in list(o.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=md.name)
        except Exception:
            pass


def BW():
    m = M()
    m.update({
        "brass": mat("M_BW_Brass", (0.55, 0.40, 0.18), 0.32, 1.0),
        "brass_dull": mat("M_BW_BrassDull", (0.30, 0.22, 0.12), 0.55, 0.9),
        "oak": mat("M_BW_Oak", (0.16, 0.09, 0.05), 0.5),
        "leather": mat("M_BW_Leather", (0.12, 0.05, 0.03), 0.65),
        "screen": mat("M_BW_Screen", (0.01, 0.012, 0.015), 0.08, 0.0, (0.25, 0.35, 0.5), 0.6),
        "ir": mat("M_BW_IR", (0.2, 0.0, 0.0), 0.3, 0.0, (0.6, 0.02, 0.02), 2.0),
        "obsidian": mat("M_BW_Obsidian", (0.012, 0.01, 0.015), 0.06, 0.2),
        "vein": mat("M_BW_RelicVein", (0.3, 0.0, 0.0), 0.3, 0.0, (1.0, 0.05, 0.02), 6.0),
        "carpaint": mat("M_BW_CarPaint", (0.08, 0.1, 0.12), 0.25, 0.6),
        "tyre": mat("M_BW_Tyre", (0.02, 0.02, 0.02), 0.85),
        "glass_car": mat("M_BW_CarGlass", (0.02, 0.025, 0.03), 0.03),
        "headlight": mat("M_BW_Headlight", (0.9, 0.9, 0.8), 0.1, 0.0, (1.0, 0.95, 0.8), 3.0),
        "ghost": mat("M_BW_Ghost", (0.55, 0.62, 0.7), 0.4, 0.0, (0.35, 0.45, 0.6), 1.5),
        "ghost_dark": mat("M_BW_GhostDark", (0.05, 0.06, 0.08), 0.6, 0.0, (0.08, 0.1, 0.14), 0.6),
        "sign_white": mat("M_BW_SignWhite", (0.75, 0.75, 0.7), 0.6),
        "sign_green": mat("M_BW_SignGreen", (0.03, 0.18, 0.08), 0.5),
        "typewriter": mat("M_BW_Typewriter", (0.03, 0.035, 0.03), 0.35, 0.4),
        "keycap": mat("M_BW_Keycap", (0.85, 0.82, 0.72), 0.5),
        "canvas": mat("M_BW_Canvas", (0.13, 0.15, 0.09), 0.92),
        "canvas_dark": mat("M_BW_CanvasDark", (0.06, 0.065, 0.05), 0.9),
    })
    return m


# ---------------------------------------------------------------- Phòng thờ phương Tây
def plaque(m, gilded):
    """Bia tưởng niệm: tấm gỗ sồi viền, mặt đồng (mạ sáng nếu là Mắt Trận), đinh tán 4 góc."""
    board = add("cube", "Board", (0, 0, 0.18), (0.13, 0.018, 0.18)); bevel(board, 0.006, 2); assign(board, m["oak"])
    face = add("cube", "Face", (0, -0.019, 0.18), (0.1, 0.003, 0.14)); bevel(face, 0.003, 2)
    assign(face, m["brass"] if gilded else m["brass_dull"])
    parts = [board, face]
    for sx in (-1, 1):
        for sz in (-1, 1):
            r = add("sphere", "Rivet", (sx * 0.085, -0.022, 0.18 + sz * 0.125), (0.007, 0.004, 0.007), segments=10, ring_count=6)
            assign(r, m["brass"]); parts.append(r)
    for i in range(3):  # dòng chữ khắc (rãnh tối)
        g = add("cube", "Line", (0, -0.0225, 0.24 - i * 0.05), (0.07 - i * 0.012, 0.0008, 0.005)); assign(g, m["bronze_dark"]); parts.append(g)
    o = join(parts, "KH_BW_Plaque" + ("_Eye" if gilded else ""))
    smooth(o, 30)


def candle_stand(m):
    """Giá nến sắt rèn: đế tròn bậc, thân tiện kiểu con tiện (không xoắn), đĩa hứng sáp, chân ba móng."""
    parts = []
    prof = [(0.0, 0.0), (0.078, 0.0), (0.08, 0.01), (0.07, 0.018), (0.05, 0.026), (0.03, 0.04), (0.018, 0.055),
            (0.024, 0.07), (0.024, 0.08), (0.012, 0.095), (0.01, 0.15), (0.016, 0.17), (0.016, 0.18), (0.009, 0.2),
            (0.009, 0.26), (0.02, 0.275), (0.012, 0.29), (0.0, 0.29)]
    body = lathe("Body", prof, 32); assign(body, m["iron"]); parts.append(body)
    for k in range(3):
        a = k * 2 * math.pi / 3
        foot = add("sphere", "Foot", (0.068 * math.cos(a), 0.068 * math.sin(a), 0.006), (0.016, 0.016, 0.009), segments=12, ring_count=8)
        assign(foot, m["iron"]); parts.append(foot)
    pan = lathe("Pan", [(0.0, 0.0), (0.012, 0.0), (0.05, 0.006), (0.058, 0.016), (0.055, 0.018), (0.046, 0.01), (0.0, 0.008)], 32)
    pan.location = (0, 0, 0.288); assign(pan, m["iron"]); parts.append(pan)
    spike = add("cone", "Spike", (0, 0, 0.318), (0.005, 0.005, 0.018), vertices=10); assign(spike, m["iron"]); parts.append(spike)
    for p in parts: apply_all(p)
    o = join(parts, "KH_BW_CandleStand")
    smooth(o, 50)


def backpack(m):
    """Balo leo núi của Julian: thân vải bo tròn, nắp trùm, túi trước, hai quai đeo, khóa kéo, quai xách."""
    body = add("cube", "Body", (0, 0, 0.24), (0.15, 0.1, 0.22)); bevel(body, 0.05, 4); assign(body, m["canvas"])
    lid = add("cube", "Lid", (0, -0.005, 0.455), (0.152, 0.105, 0.035)); bevel(lid, 0.03, 3); assign(lid, m["canvas_dark"])
    flap = add("cube", "Flap", (0, -0.1, 0.4), (0.13, 0.012, 0.07)); bevel(flap, 0.01, 2); assign(flap, m["canvas_dark"])
    pocket = add("cube", "Pocket", (0, -0.115, 0.16), (0.11, 0.035, 0.1)); bevel(pocket, 0.025, 3); assign(pocket, m["canvas_dark"])
    zip1 = add("cube", "Zip", (0, -0.151, 0.2), (0.09, 0.002, 0.003)); assign(zip1, m["plastic"])
    pull = add("cube", "Pull", (0.07, -0.153, 0.195), (0.005, 0.002, 0.012)); assign(pull, m["chrome"])
    parts = [body, lid, flap, pocket, zip1, pull]
    for s in (-1, 1):
        strap = add("cube", "Strap", (s * 0.075, 0.115, 0.24), (0.028, 0.012, 0.2)); bevel(strap, 0.008, 2); assign(strap, m["canvas_dark"]); parts.append(strap)
        side = add("cube", "SidePocket", (s * 0.155, 0, 0.14), (0.02, 0.07, 0.08)); bevel(side, 0.015, 2); assign(side, m["canvas"]); parts.append(side)
        buckle = add("cube", "Buckle", (s * 0.06, -0.113, 0.4), (0.012, 0.004, 0.012)); assign(buckle, m["plastic"]); parts.append(buckle)
    handle = add("torus", "Handle", (0, 0.06, 0.49), (0.035, 0.035, 0.035), (math.pi / 2, 0, 0), major_radius=1.0, minor_radius=0.18); assign(handle, m["canvas_dark"]); parts.append(handle)
    stain = add("cube", "Stain", (0.05, -0.152, 0.11), (0.04, 0.001, 0.03)); assign(stain, m["blood"]); parts.append(stain)
    for p in parts: apply_all(p)
    o = join(parts, "KH_BW_Backpack")
    smooth(o, 40)


def vademecum(m):
    """Sách cổ bìa da, gáy nổi, khóa đồng."""
    cover = add("cube", "Cover", (0, 0, 0.022), (0.11, 0.15, 0.022)); bevel(cover, 0.006, 3); assign(cover, m["leather"])
    pages = add("cube", "Pages", (0.006, 0, 0.022), (0.104, 0.144, 0.018)); assign(pages, m["paper"])
    parts = [cover, pages]
    for y in (-0.1, -0.03, 0.04, 0.11):
        rib = add("cyl", "Rib", (-0.11, y, 0.022), (0.004, 0.004, 0.024), (0, 0, 0), vertices=8); assign(rib, m["leather"]); parts.append(rib)
    clasp = add("cube", "Clasp", (0.113, 0, 0.022), (0.006, 0.02, 0.026)); assign(clasp, m["brass"]); parts.append(clasp)
    emblem = add("torus", "Emblem", (0.0, 0.0, 0.045), (0.045, 0.045, 0.045), major_radius=1.0, minor_radius=0.08); assign(emblem, m["brass"]); parts.append(emblem)
    o = join(parts, "KH_BW_Vademecum")
    smooth(o, 40)


def relic(m):
    """Khối Cổ Vật obsidian: bát diện kéo dài, mạch đỏ phát sáng."""
    core = add("sphere", "Core", (0, 0, 0), (0.14, 0.14, 0.24), segments=6, ring_count=3); assign(core, m["obsidian"])
    parts = [core]
    random.seed(13)
    for i in range(9):
        a = random.uniform(0, 6.28); z = random.uniform(-0.15, 0.15)
        v = add("cube", "Vein", (0.11 * math.cos(a), 0.11 * math.sin(a), z), (0.004, 0.004, random.uniform(0.04, 0.09)), (random.uniform(-0.5, 0.5), 0, a))
        assign(v, m["vein"]); parts.append(v)
    o = join(parts, "KH_BW_Relic")


# ---------------------------------------------------------------- Công cụ của Alex
def phone(m):
    """Điện thoại: thân bo góc, màn hình phát sáng nhẹ, cụm camera + đèn flash."""
    body = add("cube", "Body", (0, 0, 0), (0.037, 0.0045, 0.078)); bevel(body, 0.006, 4); assign(body, m["plastic"])
    scr = add("cube", "Screen", (0, -0.0047, 0), (0.034, 0.0004, 0.074)); bevel(scr, 0.004, 3); assign(scr, m["screen"])
    cam = add("cube", "CamBump", (-0.018, 0.005, 0.06), (0.012, 0.0015, 0.013)); bevel(cam, 0.003, 2); assign(cam, m["plastic"])
    l1 = add("cyl", "Lens1", (-0.022, 0.0068, 0.066), (0.005, 0.005, 0.001), (math.pi / 2, 0, 0), vertices=16); assign(l1, m["glass"])
    l2 = add("cyl", "Lens2", (-0.022, 0.0068, 0.054), (0.005, 0.005, 0.001), (math.pi / 2, 0, 0), vertices=16); assign(l2, m["glass"])
    fl = add("cyl", "Flash", (-0.012, 0.0066, 0.06), (0.0025, 0.0025, 0.0008), (math.pi / 2, 0, 0), vertices=12); assign(fl, m["wax"])
    o = join([body, scr, cam, l1, l2, fl], "KH_BW_Phone")
    smooth(o, 30)


def camcorder(m):
    """Máy quay nhìn đêm cầm tay: thân, ống kính, vòng đèn hồng ngoại, màn lật."""
    body = add("cube", "Body", (0, 0, 0), (0.045, 0.11, 0.055)); bevel(body, 0.008, 3); assign(body, m["plastic"])
    lens = add("cyl", "Lens", (0, -0.13, 0.008), (0.032, 0.032, 0.03), (math.pi / 2, 0, 0), vertices=24); assign(lens, m["plastic"])
    glass = add("cyl", "Glass", (0, -0.161, 0.008), (0.025, 0.025, 0.002), (math.pi / 2, 0, 0), vertices=24); assign(glass, m["glass"])
    ring = add("torus", "IRRing", (0, -0.158, 0.008), (0.03, 0.03, 0.03), (math.pi / 2, 0, 0), major_radius=1.0, minor_radius=0.12); assign(ring, m["ir"])
    strap = add("cube", "Strap", (0.05, 0.0, 0.0), (0.006, 0.08, 0.035)); bevel(strap, 0.004, 2); assign(strap, m["leather"])
    lcd = add("cube", "LCD", (-0.052, -0.02, 0.01), (0.004, 0.04, 0.03)); bevel(lcd, 0.003, 2); assign(lcd, m["screen"])
    o = join([body, lens, glass, ring, strap, lcd], "KH_BW_Camcorder")
    smooth(o, 35)


# ---------------------------------------------------------------- Cảnh mở đầu / kết
def sedan(m):
    """Xe sedan thập niên 90 (khối đơn giản, đủ cho cutscene đêm sương): thân, cabin, 4 bánh, đèn."""
    body = add("cube", "Body", (0, 0, 0.55), (0.88, 2.25, 0.32)); bevel(body, 0.08, 3); assign(body, m["carpaint"])
    cabin = add("cube", "Cabin", (0, 0.15, 1.05), (0.8, 1.15, 0.27)); bevel(cabin, 0.12, 3)
    tp = cabin.modifiers.new("Taper", "SIMPLE_DEFORM"); tp.deform_method = "TAPER"; tp.factor = -0.25; tp.deform_axis = "Z"
    assign(cabin, m["glass_car"])
    roof = add("cube", "Roof", (0, 0.15, 1.33), (0.68, 0.85, 0.03)); bevel(roof, 0.03, 2); assign(roof, m["carpaint"])
    parts = [body, cabin, roof]
    for sx in (-1, 1):
        for sy in (-1, 1):
            w = add("cyl", "Wheel", (sx * 0.82, sy * 1.45, 0.34), (0.34, 0.34, 0.12), (0, math.pi / 2, 0), vertices=24); assign(w, m["tyre"]); parts.append(w)
            hub = add("cyl", "Hub", (sx * 0.95, sy * 1.45, 0.34), (0.16, 0.16, 0.02), (0, math.pi / 2, 0), vertices=16); assign(hub, m["chrome"]); parts.append(hub)
        hl = add("cube", "Headlight", (sx * 0.6, -2.26, 0.62), (0.16, 0.02, 0.07)); assign(hl, m["headlight"]); parts.append(hl)
        tl = add("cube", "Taillight", (sx * 0.62, 2.26, 0.64), (0.14, 0.02, 0.06)); assign(tl, m["led"]); parts.append(tl)
    for p in parts: apply_all(p)
    o = join(parts, "KH_BW_Sedan")
    smooth(o, 35)


def signpost(m):
    """Biển báo đường quê: cột gỗ + bảng xanh viền trắng (chữ do Unity vẽ bằng TMP)."""
    post = add("cube", "Post", (0, 0, 1.1), (0.05, 0.05, 1.1)); assign(post, m["wood_dark"])
    board = add("cube", "Board", (0, -0.06, 1.85), (0.75, 0.02, 0.28)); bevel(board, 0.02, 2); assign(board, m["sign_white"])
    inner = add("cube", "Inner", (0, -0.081, 1.85), (0.71, 0.002, 0.24)); assign(inner, m["sign_green"])
    o = join([post, board, inner], "KH_BW_Signpost")


def typewriter(m):
    """Máy chữ cổ (điểm lưu): thân, trục cuốn giấy, tờ giấy, hàng phím."""
    body = add("cube", "Body", (0, 0, 0.06), (0.17, 0.15, 0.06)); bevel(body, 0.02, 3); assign(body, m["typewriter"])
    roller = add("cyl", "Roller", (0, 0.11, 0.135), (0.02, 0.02, 0.2), (0, math.pi / 2, 0), vertices=20); assign(roller, m["typewriter"])
    paper = add("cube", "Paper", (0, 0.12, 0.2), (0.1, 0.002, 0.08), (math.radians(-15), 0, 0)); assign(paper, m["paper"])
    parts = [body, roller, paper]
    for row in range(3):
        for k in range(9):
            key = add("cyl", "Key", (-0.12 + k * 0.03 + row * 0.01, -0.05 - row * 0.03 + 0.04, 0.125 - row * 0.012), (0.011, 0.011, 0.006), vertices=12)
            assign(key, m["keycap"]); parts.append(key)
    o = join(parts, "KH_BW_Typewriter")
    smooth(o, 40)


def lord_ghost(m):
    """Bóng ma Lord Blackwood: quý ông áo đuôi tôm, mũ chóp, ngồi được trên ghế (tư thế đứng thẳng, Unity chỉnh)."""
    parts = []
    legs = [add("cyl", "Leg", (s * 0.11, 0, 0.45), (0.075, 0.075, 0.45), vertices=12) for s in (-1, 1)]
    for l in legs: assign(l, m["ghost_dark"]); parts.append(l)
    torso = add("cube", "Torso", (0, 0, 1.18), (0.22, 0.13, 0.32)); bevel(torso, 0.06, 3); assign(torso, m["ghost_dark"]); parts.append(torso)
    tails = add("cube", "Tails", (0, 0.1, 0.8), (0.2, 0.03, 0.22)); bevel(tails, 0.03, 2); assign(tails, m["ghost_dark"]); parts.append(tails)
    for s in (-1, 1):
        arm = add("cyl", "Arm", (s * 0.28, 0, 1.12), (0.055, 0.055, 0.32), (0, s * 0.12, 0), vertices=10); assign(arm, m["ghost_dark"]); parts.append(arm)
        hand = add("sphere", "Hand", (s * 0.32, 0, 0.78), (0.045, 0.035, 0.06), segments=10, ring_count=6); assign(hand, m["ghost"]); parts.append(hand)
    collar = add("cube", "Collar", (0, -0.12, 1.42), (0.08, 0.02, 0.08)); assign(collar, m["ghost"]); parts.append(collar)
    head = add("sphere", "Head", (0, 0, 1.66), (0.1, 0.11, 0.13), segments=20, ring_count=12); assign(head, m["ghost"]); parts.append(head)
    brim = add("cyl", "Brim", (0, 0, 1.8), (0.17, 0.17, 0.008), vertices=24); assign(brim, m["ghost_dark"]); parts.append(brim)
    hat = add("cyl", "Hat", (0, 0, 1.92), (0.1, 0.1, 0.12), vertices=24); assign(hat, m["ghost_dark"]); parts.append(hat)
    for p in parts: apply_all(p)
    o = join(parts, "KH_BW_LordGhost")
    smooth(o, 45)


BW_BUILDERS = [
    ("KH_BW_Plaque", lambda m: plaque(m, False)), ("KH_BW_Plaque_Eye", lambda m: plaque(m, True)),
    ("KH_BW_CandleStand", candle_stand), ("KH_BW_Vademecum", vademecum), ("KH_BW_Relic", relic),
    ("KH_BW_Phone", phone), ("KH_BW_Camcorder", camcorder), ("KH_BW_Sedan", sedan),
    ("KH_BW_Signpost", signpost), ("KH_BW_Typewriter", typewriter), ("KH_BW_LordGhost", lord_ghost),
    ("KH_BW_Backpack", backpack),
]

only_bw = set(a for a in argv[1:] if not a.startswith("--"))
if "--preview" in argv:
    render_preview([n for n, _ in BW_BUILDERS], os.path.join(OUT, "preview_bw.png"))
elif "--icons" in argv:
    render_icons(["KH_BW_Phone", "KH_BW_Camcorder"], os.path.join(OUT, "icons"))
else:
    for name, fn in BW_BUILDERS:
        if only_bw and name not in only_bw:
            continue
        reset()
        fn(BW())
        export(name)
print("BW DONE")
