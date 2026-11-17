# Đạo cụ phương Tây cho "Blackwood Manor" — dùng lại helper của props_common.py.
#   blender.exe --background --factory-startup --python Tools/Blender/blackwood_props.py -- <output_dir> [TênModel...] [--preview]
# Xuất FBX (mét, Y-up) tên BW_*; converter Unity tự nhập từ Tools/Blender/out.
import bpy, math, os, sys, random

sys.argv_backup = list(sys.argv)
HERE = os.path.dirname(os.path.abspath(__file__))
# Nạp helper (reset, mat, add, lathe, join, bevel, smooth, export, M, render_preview) mà không chạy phần main
_src = open(os.path.join(HERE, "props_common.py"), encoding="utf-8").read()
_src = _src.split("\nonly = set(")[0]
exec(compile(_src, "props_common_helpers", "exec"))


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
    o = join(parts, "BW_Plaque" + ("_Eye" if gilded else ""))
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
    o = join(parts, "BW_CandleStand")
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
    o = join(parts, "BW_Backpack")
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
    o = join(parts, "BW_Vademecum")
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
    o = join(parts, "BW_Relic")


# ---------------------------------------------------------------- helper dựng hình
def prism(name, pts, half_w, narrow=1.0):
    """Đùn biên dạng (y, z) theo trục X thành khối đặc; narrow < 1 bóp hẹp dần phần cao (khoang kính xe)."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    zs = [p[1] for p in pts]
    z0, z1 = min(zs), max(zs)

    def w(z):
        t = (z - z0) / (z1 - z0) if z1 > z0 else 0.0
        return half_w * (1 + (narrow - 1) * t)

    L = [bm.verts.new((-w(z), y, z)) for y, z in pts]
    R = [bm.verts.new((w(z), y, z)) for y, z in pts]
    n = len(pts)
    bm.faces.new(L)
    bm.faces.new(list(reversed(R)))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((L[i], R[i], R[j], L[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(o)
    return o


def turned(name, profile, loc, rot=(0, 0, 0), scale=(1, 1, 1), seg=16):
    """Vật tròn xoay đặt tại loc (profile [(r, z)] tính từ gốc của chính nó)."""
    o = lathe(name, profile, seg)
    o.location = loc
    o.rotation_euler = rot
    o.scale = scale
    return o


# ---------------------------------------------------------------- Công cụ của Alex
def phone(m):
    """Điện thoại: khung kim loại, mặt kính viền đen, màn hình sáng, cụm camera viền crôm, nút bên hông."""
    P = []
    body = add("cube", "Body", (0, 0.0006, 0), (0.037, 0.004, 0.078)); bevel(body, 0.006, 4); assign(body, m["plastic"]); P.append(body)
    frame = add("cube", "Frame", (0, 0, 0), (0.0376, 0.0028, 0.0786)); bevel(frame, 0.0065, 4); assign(frame, m["chrome"]); P.append(frame)
    glass = add("cube", "Glass", (0, -0.0037, 0), (0.0358, 0.0004, 0.0768)); bevel(glass, 0.0055, 3); assign(glass, m["glass"]); P.append(glass)
    scr = add("cube", "Screen", (0, -0.0042, 0.001), (0.0328, 0.0002, 0.069)); bevel(scr, 0.003, 2); assign(scr, m["screen"]); P.append(scr)
    ear = add("cube", "Earpiece", (0, -0.00425, 0.0738), (0.006, 0.0002, 0.0007)); assign(ear, m["plastic"]); P.append(ear)
    cam = add("cube", "CamBump", (-0.018, 0.0052, 0.06), (0.012, 0.0012, 0.013)); bevel(cam, 0.004, 3); assign(cam, m["plastic"]); P.append(cam)
    for z in (0.066, 0.054):
        l = add("cyl", "Lens", (-0.022, 0.0066, z), (0.0046, 0.0046, 0.0008), (math.pi / 2, 0, 0), vertices=20); assign(l, m["glass"]); P.append(l)
        r = add("torus", "LensRing", (-0.022, 0.0066, z), (0.0054, 0.0054, 0.0054), (math.pi / 2, 0, 0), major_radius=1.0, minor_radius=0.16, major_segments=24, minor_segments=8)
        assign(r, m["chrome"]); P.append(r)
    fl = add("cyl", "Flash", (-0.012, 0.0066, 0.06), (0.0025, 0.0025, 0.0006), (math.pi / 2, 0, 0), vertices=12); assign(fl, m["wax"]); P.append(fl)
    for x, z, h in ((0.0378, 0.022, 0.009), (-0.0378, 0.034, 0.006), (-0.0378, 0.018, 0.006)):
        b = add("cube", "Button", (x, 0, z), (0.0007, 0.0014, h)); bevel(b, 0.0005, 1); assign(b, m["chrome"]); P.append(b)
    port = add("cube", "Port", (0, 0, -0.0785), (0.0042, 0.0012, 0.0005)); assign(port, m["plastic"]); P.append(port)
    for p in P: apply_all(p)
    o = join(P, "BW_Phone")
    smooth(o, 30)


def camcorder(m):
    """Máy quay nhìn đêm cầm tay: ống kính nhiều nấc, vòng đèn hồng ngoại, báng cầm + quai da, ống ngắm, màn lật mở."""
    P = []
    body = add("cube", "Body", (0, 0, 0), (0.045, 0.11, 0.055)); bevel(body, 0.01, 3); assign(body, m["plastic"]); P.append(body)
    grip = add("cube", "Grip", (0.05, 0.005, -0.006), (0.013, 0.085, 0.044)); bevel(grip, 0.011, 3); assign(grip, m["plastic"]); P.append(grip)
    strap = add("cube", "Strap", (0.0665, 0.005, -0.002), (0.0035, 0.075, 0.03)); bevel(strap, 0.003, 2); assign(strap, m["leather"]); P.append(strap)
    for y in (-0.072, 0.082):
        a = add("cube", "StrapAnchor", (0.064, y, -0.002), (0.005, 0.004, 0.032)); bevel(a, 0.002, 1); assign(a, m["chrome"]); P.append(a)
    barrel = turned("Lens", [(0, 0), (0.037, 0), (0.037, 0.012), (0.033, 0.014), (0.033, 0.03), (0.0375, 0.032), (0.0375, 0.05),
                             (0.0345, 0.052), (0.0345, 0.064), (0.031, 0.064), (0.0285, 0.056), (0, 0.056)],
                    (0, -0.108, 0.008), (math.pi / 2, 0, 0), seg=28)
    assign(barrel, m["plastic"]); P.append(barrel)
    glass = add("cyl", "Glass", (0, -0.1645, 0.008), (0.0285, 0.0285, 0.0012), (math.pi / 2, 0, 0), vertices=28); assign(glass, m["glass"]); P.append(glass)
    ring = add("torus", "IRRing", (0, -0.1722, 0.008), (0.0328, 0.0328, 0.0328), (math.pi / 2, 0, 0), major_radius=1.0, minor_radius=0.075, major_segments=32, minor_segments=8)
    assign(ring, m["ir"]); P.append(ring)
    mic = add("cube", "Mic", (0, -0.07, 0.062), (0.015, 0.03, 0.008)); bevel(mic, 0.005, 2); assign(mic, m["plastic"]); P.append(mic)
    grille = add("cube", "MicGrille", (0, -0.1005, 0.062), (0.011, 0.0008, 0.005)); assign(grille, m["iron"]); P.append(grille)
    shoe = add("cube", "HotShoe", (0, 0.0, 0.0562), (0.012, 0.014, 0.0015)); assign(shoe, m["chrome"]); P.append(shoe)
    zoom = add("cube", "Zoom", (0.03, 0.05, 0.0565), (0.006, 0.014, 0.002)); bevel(zoom, 0.0015, 1); assign(zoom, m["chrome"]); P.append(zoom)
    vf = add("cyl", "Viewfinder", (0.016, 0.135, 0.034), (0.014, 0.014, 0.03), (math.pi / 2, 0, 0), vertices=20); assign(vf, m["plastic"]); P.append(vf)
    cup = turned("EyeCup", [(0, 0), (0.016, 0), (0.021, 0.006), (0.022, 0.014), (0.017, 0.014), (0.015, 0.006), (0, 0.006)],
                 (0.016, 0.164, 0.034), (-math.pi / 2, 0, 0), seg=20)
    assign(cup, m["leather"]); P.append(cup)
    batt = add("cube", "Battery", (-0.012, 0.117, -0.012), (0.026, 0.009, 0.032)); bevel(batt, 0.004, 2); assign(batt, m["plastic"]); P.append(batt)
    rec = add("cyl", "RecButton", (0.03, 0.1115, -0.03), (0.003, 0.003, 0.0015), (math.pi / 2, 0, 0), vertices=14); assign(rec, m["led"]); P.append(rec)
    # màn lật mở vuông góc với thân, mặt màn quay về phía người cầm (+Y)
    hinge = add("cyl", "Hinge", (-0.047, -0.064, 0.01), (0.004, 0.004, 0.03), vertices=12); assign(hinge, m["plastic"]); P.append(hinge)
    panel = add("cube", "LCDPanel", (-0.091, -0.064, 0.01), (0.042, 0.004, 0.031)); bevel(panel, 0.003, 2); assign(panel, m["plastic"]); P.append(panel)
    lcd = add("cube", "LCD", (-0.091, -0.0597, 0.01), (0.037, 0.0004, 0.026)); assign(lcd, m["screen"]); P.append(lcd)
    for p in P: apply_all(p)
    o = join(P, "BW_Camcorder")
    smooth(o, 35)


# ---------------------------------------------------------------- Cảnh mở đầu / kết
def sedan(m):
    """Xe sedan thập niên 90: thân có biên dạng nắp máy/cốp, khoang kính thu hẹp, trụ A/B/C, hốc bánh, cản, lưới tản nhiệt,
    gương, tay nắm, biển số. Đầu xe hướng -Y."""
    P = []
    body = prism("Body", [(-2.25, 0.30), (-2.25, 0.62), (-2.14, 0.75), (-0.95, 0.86), (1.5, 0.88), (2.2, 0.84), (2.25, 0.62),
                          (2.25, 0.32), (1.9, 0.24), (-1.9, 0.24)], 0.88)
    bevel(body, 0.05, 3); assign(body, m["carpaint"]); P.append(body)
    cabin = prism("Cabin", [(-0.95, 0.85), (-0.35, 1.36), (0.95, 1.38), (1.55, 0.87)], 0.80, 0.86)
    bevel(cabin, 0.03, 2); assign(cabin, m["glass_car"]); P.append(cabin)
    roof = prism("Roof", [(-0.44, 1.335), (-0.36, 1.39), (0.97, 1.41), (1.04, 1.355)], 0.70)
    bevel(roof, 0.015, 2); assign(roof, m["carpaint"]); P.append(roof)
    a_ang = math.atan2(0.5, 0.6)
    for sx in (-1, 1):
        tilt = -sx * math.atan2(0.11, 0.5)
        pa = add("cube", "PillarA", (sx * 0.755, -0.65, 1.11), (0.03, 0.4, 0.03), (a_ang, tilt, 0)); assign(pa, m["carpaint"]); P.append(pa)
        pb = add("cube", "PillarB", (sx * 0.755, 0.3, 1.12), (0.03, 0.045, 0.26), (0, tilt, 0)); assign(pb, m["carpaint"]); P.append(pb)
        pc = add("cube", "PillarC", (sx * 0.755, 1.25, 1.125), (0.03, 0.4, 0.035), (-a_ang, tilt, 0)); assign(pc, m["carpaint"]); P.append(pc)
        trim = add("cube", "BeltTrim", (sx * 0.872, 0.28, 0.872), (0.012, 1.25, 0.008)); assign(trim, m["chrome"]); P.append(trim)
        for y in (-0.92, 0.3, 1.38):
            seam = add("cube", "DoorSeam", (sx * 0.881, y, 0.57), (0.0025, 0.005, 0.28)); assign(seam, m["plastic"]); P.append(seam)
        for y in (-0.05, 1.05):
            h = add("cube", "Handle", (sx * 0.886, y, 0.76), (0.008, 0.06, 0.012)); bevel(h, 0.004, 1); assign(h, m["chrome"]); P.append(h)
        mir = add("cube", "Mirror", (sx * 0.95, -0.82, 0.93), (0.07, 0.03, 0.045)); bevel(mir, 0.015, 2); assign(mir, m["carpaint"]); P.append(mir)
        for sy in (-1, 1):
            y = sy * 1.45
            arch = add("cyl", "Arch", (sx * 0.8, y, 0.36), (0.45, 0.45, 0.086), (0, math.pi / 2, 0), vertices=28); assign(arch, m["plastic"]); P.append(arch)
            w = add("cyl", "Wheel", (sx * 0.83, y, 0.34), (0.34, 0.34, 0.11), (0, math.pi / 2, 0), vertices=28); bevel(w, 0.035, 3); assign(w, m["tyre"]); P.append(w)
            hub = turned("Hub", [(0, 0), (0.2, 0), (0.2, 0.012), (0.17, 0.02), (0.07, 0.035), (0.05, 0.05), (0, 0.05)],
                         (sx * 0.935, y, 0.34), (0, sx * math.pi / 2, 0), seg=20)
            assign(hub, m["chrome"]); P.append(hub)
        hl = add("cube", "Headlight", (sx * 0.6, -2.262, 0.63), (0.17, 0.02, 0.075)); bevel(hl, 0.01, 1); assign(hl, m["headlight"]); P.append(hl)
        ind = add("cube", "Indicator", (sx * 0.81, -2.255, 0.63), (0.035, 0.02, 0.07)); assign(ind, m["wax"]); P.append(ind)
        tl = add("cube", "Taillight", (sx * 0.62, 2.262, 0.66), (0.2, 0.02, 0.065)); bevel(tl, 0.01, 1); assign(tl, m["led"]); P.append(tl)
        wiper = add("cube", "Wiper", (sx * 0.3 - 0.05, -0.83, 0.925), (0.24, 0.006, 0.006), (0, 0, sx * 0.05)); assign(wiper, m["plastic"]); P.append(wiper)
    for sy in (-1, 1):
        bump = add("cube", "Bumper", (0, sy * 2.285, 0.42), (0.9, 0.07, 0.08)); bevel(bump, 0.035, 3); assign(bump, m["plastic"]); P.append(bump)
        plate = add("cube", "Plate", (0, sy * 2.358, 0.43), (0.21, 0.004, 0.05)); assign(plate, m["sign_white"]); P.append(plate)
    grille = add("cube", "Grille", (0, -2.262, 0.63), (0.4, 0.015, 0.06)); assign(grille, m["plastic"]); P.append(grille)
    for k in range(5):
        bar = add("cube", "GrilleBar", (0, -2.279, 0.585 + k * 0.022), (0.39, 0.003, 0.004)); assign(bar, m["chrome"]); P.append(bar)
    hood = add("cube", "HoodLine", (0, -0.98, 0.862), (0.8, 0.006, 0.004)); assign(hood, m["plastic"]); P.append(hood)
    trunk = add("cube", "TrunkLine", (0, 1.6, 0.882), (0.8, 0.006, 0.004)); assign(trunk, m["plastic"]); P.append(trunk)
    ex = add("cyl", "Exhaust", (-0.5, 2.3, 0.27), (0.03, 0.03, 0.09), (math.pi / 2, 0, 0), vertices=14); assign(ex, m["chrome"]); P.append(ex)
    for p in P: apply_all(p)
    o = join(P, "BW_Sedan")
    smooth(o, 35)


def signpost(m):
    """Biển báo đường quê: cột gỗ + bảng xanh viền trắng (chữ do Unity vẽ bằng TMP)."""
    post = add("cube", "Post", (0, 0, 1.1), (0.05, 0.05, 1.1)); bevel(post, 0.008, 2); assign(post, m["wood_dark"])
    board = add("cube", "Board", (0, -0.06, 1.85), (0.75, 0.02, 0.28)); bevel(board, 0.02, 2); assign(board, m["sign_white"])
    inner = add("cube", "Inner", (0, -0.081, 1.85), (0.71, 0.002, 0.24)); assign(inner, m["sign_green"])
    P = [post, board, inner]
    for sx in (-1, 1):
        for z in (1.72, 1.98):
            bolt = add("cyl", "Bolt", (sx * 0.02, -0.084, z), (0.012, 0.012, 0.004), (math.pi / 2, 0, 0), vertices=10); assign(bolt, m["iron"]); P.append(bolt)
    for p in P: apply_all(p)
    o = join(P, "BW_Signpost")
    smooth(o, 35)


def typewriter(m):
    """Máy chữ cổ (điểm lưu): thân dốc về phía phím, 4 hàng phím tròn có chân, quạt cần chữ, cuộn ruy băng,
    bàn trượt + trục cao su, núm vặn, cần xuống dòng, tờ giấy."""
    P = []
    body = prism("Body", [(-0.15, 0.0), (-0.15, 0.035), (-0.02, 0.075), (0.0, 0.12), (0.15, 0.12), (0.15, 0.0)], 0.17)
    bevel(body, 0.008, 3); assign(body, m["typewriter"]); P.append(body)
    for r in range(4):
        y = -0.135 + r * 0.03
        top = 0.035 + (y + 0.15) * 0.3077 + 0.022
        for k in range(10):
            x = -0.142 + k * 0.03 + (r % 2) * 0.015
            stem = add("cyl", "Stem", (x, y, top - 0.012), (0.0028, 0.0028, 0.012), vertices=8); assign(stem, m["chrome"]); P.append(stem)
            rim = add("cyl", "KeyRim", (x, y, top), (0.0128, 0.0128, 0.0022), vertices=16); assign(rim, m["chrome"]); P.append(rim)
            cap = add("cyl", "Key", (x, y, top + 0.0022), (0.0108, 0.0108, 0.0012), vertices=16); assign(cap, m["keycap"]); P.append(cap)
    space = add("cube", "Space", (0, -0.164, 0.04), (0.095, 0.008, 0.004)); bevel(space, 0.003, 2); assign(space, m["keycap"]); P.append(space)
    for sx in (-1, 1):
        arm = add("cube", "SpaceArm", (sx * 0.08, -0.156, 0.034), (0.003, 0.01, 0.003)); assign(arm, m["chrome"]); P.append(arm)
        spool = add("cyl", "Spool", (sx * 0.095, 0.035, 0.127), (0.03, 0.03, 0.007), vertices=24); assign(spool, m["plastic"]); P.append(spool)
        nub = add("cyl", "SpoolNub", (sx * 0.095, 0.035, 0.136), (0.007, 0.007, 0.003), vertices=12); assign(nub, m["chrome"]); P.append(nub)
        knob = add("cyl", "Knob", (sx * 0.218, 0.115, 0.16), (0.027, 0.027, 0.012), (0, math.pi / 2, 0), vertices=20); bevel(knob, 0.004, 2); assign(knob, m["plastic"]); P.append(knob)
        foot = add("cyl", "Foot", (sx * 0.14, 0.0, -0.004), (0.014, 0.014, 0.005), vertices=12); assign(foot, m["plastic"]); P.append(foot)
        for yy in (-0.12, 0.12):
            f2 = add("cyl", "Foot", (sx * 0.14, yy, -0.004), (0.014, 0.014, 0.005), vertices=12); assign(f2, m["plastic"]); P.append(f2)
    well = add("cyl", "Basket", (0, 0.02, 0.1205), (0.062, 0.062, 0.002), vertices=28); assign(well, m["iron"]); P.append(well)
    for k in range(17):
        a = math.radians(-72 + k * 9)
        bar = add("cube", "TypeBar", (math.sin(a) * 0.032, 0.02 - math.cos(a) * 0.032, 0.1235), (0.0011, 0.027, 0.0013), (0, 0, -a)); assign(bar, m["chrome"]); P.append(bar)
    badge = add("cube", "Badge", (0, -0.0115, 0.098), (0.05, 0.0015, 0.011), (-0.42, 0, 0)); assign(badge, m["brass"]); P.append(badge)
    carriage = add("cube", "Carriage", (0, 0.112, 0.134), (0.2, 0.034, 0.014)); bevel(carriage, 0.006, 2); assign(carriage, m["typewriter"]); P.append(carriage)
    platen = add("cyl", "Roller", (0, 0.115, 0.16), (0.021, 0.021, 0.192), (0, math.pi / 2, 0), vertices=24); assign(platen, m["plastic"]); P.append(platen)
    bail = add("cyl", "PaperBail", (0, 0.096, 0.181), (0.003, 0.003, 0.175), (0, math.pi / 2, 0), vertices=8); assign(bail, m["chrome"]); P.append(bail)
    lever = add("cube", "ReturnLever", (-0.205, 0.07, 0.165), (0.004, 0.055, 0.003), (0, 0, 0.45)); bevel(lever, 0.002, 1); assign(lever, m["chrome"]); P.append(lever)
    paper = add("cube", "Paper", (0, 0.127, 0.24), (0.105, 0.0008, 0.085), (math.radians(-12), 0, 0)); assign(paper, m["paper"]); P.append(paper)
    for p in P: apply_all(p)
    o = join(P, "BW_Typewriter")
    smooth(o, 40)


def lord_ghost(m):
    """Bóng ma Lord Blackwood: quý ông áo đuôi tôm, mũ chóp cao, tay phải chống gậy. Thân/tay/chân dựng bằng khối tròn xoay
    (không còn khối hộp), mặt có hốc mắt, mũi, râu quai nón để bóng in rõ dáng người."""
    P = []
    D, G = m["ghost_dark"], m["ghost"]

    def put(o, mt):
        assign(o, mt); P.append(o); return o

    for s in (-1, 1):
        put(add("sphere", "Shoe", (s * 0.1, -0.045, 0.045), (0.055, 0.13, 0.045), segments=14, ring_count=8), D)
        put(turned("Leg", [(0, 0.05), (0.058, 0.05), (0.062, 0.25), (0.07, 0.48), (0.086, 0.75), (0.094, 0.96), (0, 0.96)], (s * 0.1, 0, 0)), D)
        put(add("cube", "Tail", (s * 0.085, 0.118, 0.62), (0.078, 0.016, 0.3), (-0.07, 0, s * 0.1)), D)
        bevel(P[-1], 0.012, 2)
        put(add("sphere", "Shoulder", (s * 0.25, 0, 1.435), (0.09, 0.085, 0.075), segments=14, ring_count=8), D)
        put(turned("Arm", [(0, 0), (0.048, 0), (0.056, 0.1), (0.06, 0.3), (0.07, 0.55), (0.076, 0.62), (0, 0.65)], (s * 0.335, -0.01, 0.84), (0, -s * 0.1, 0)), D)
        put(add("cyl", "Cuff", (s * 0.336, -0.01, 0.845), (0.052, 0.052, 0.02), vertices=14), G)
        put(add("sphere", "Hand", (s * 0.338, -0.015, 0.775), (0.04, 0.03, 0.065), segments=12, ring_count=8), G)
        put(add("sphere", "Thumb", (s * 0.325, -0.045, 0.79), (0.014, 0.016, 0.035), segments=8, ring_count=6), G)
        put(add("cube", "Collar", (s * 0.045, -0.078, 1.545), (0.03, 0.006, 0.04), (0.2, 0, -s * 0.5)), G)
        put(add("sphere", "Eye", (s * 0.036, -0.097, 1.7), (0.025, 0.014, 0.018), segments=10, ring_count=6), D)
        put(add("sphere", "Whisker", (s * 0.072, -0.055, 1.625), (0.03, 0.045, 0.055), segments=10, ring_count=6), D)
        put(add("sphere", "Ear", (s * 0.097, 0.005, 1.68), (0.012, 0.02, 0.03), segments=8, ring_count=6), G)
    put(turned("Torso", [(0, 0.82), (0.2, 0.82), (0.215, 0.95), (0.2, 1.08), (0.215, 1.25), (0.238, 1.38), (0.21, 1.47), (0.1, 1.535), (0, 1.535)],
               (0, 0, 0), scale=(1, 0.62, 1), seg=24), D)
    put(add("cube", "Shirt", (0, -0.139, 1.3), (0.045, 0.006, 0.13)), G); bevel(P[-1], 0.004, 1)
    put(add("sphere", "Cravat", (0, -0.133, 1.455), (0.05, 0.022, 0.05), segments=12, ring_count=8), G)
    for k in range(3):
        put(add("sphere", "Button", (0, -0.147, 1.2 + k * 0.07), (0.009, 0.005, 0.009), segments=8, ring_count=6), D)
    put(add("cyl", "Neck", (0, 0, 1.55), (0.05, 0.05, 0.05), vertices=14), G)
    put(add("sphere", "Head", (0, 0, 1.685), (0.095, 0.11, 0.125), segments=24, ring_count=14), G)
    put(add("sphere", "Jaw", (0, -0.022, 1.605), (0.074, 0.086, 0.08), segments=16, ring_count=10), G)
    put(add("cone", "Nose", (0, -0.112, 1.672), (0.017, 0.017, 0.03), (math.pi / 2 - 0.35, 0, 0), vertices=10), G)
    put(add("cube", "Brow", (0, -0.1, 1.727), (0.07, 0.012, 0.008)), G); bevel(P[-1], 0.006, 2)
    put(add("cube", "Mouth", (0, -0.103, 1.622), (0.03, 0.006, 0.004)), D)
    put(turned("Hat", [(0, 1.79), (0.168, 1.79), (0.182, 1.802), (0.168, 1.808), (0.099, 1.808), (0.093, 1.825), (0.103, 2.045), (0, 2.05)],
               (0, 0, 0), seg=28), D)
    put(add("cyl", "HatBand", (0, 0, 1.835), (0.097, 0.097, 0.014), vertices=28), G)
    put(add("cyl", "Cane", (0.345, -0.06, 0.37), (0.011, 0.011, 0.37), vertices=10), D)
    put(add("sphere", "CaneKnob", (0.345, -0.06, 0.755), (0.028, 0.028, 0.028), segments=12, ring_count=8), G)
    for p in P: apply_all(p)
    o = join(P, "BW_LordGhost")
    # Vật liệu bóng ma trong suốt: nếu để các khối chồng nhau thì nhìn xuyên thấy "ruột" (chân trong vạt áo, cổ trong đầu…).
    # Hợp tất cả thành MỘT lớp vỏ kín bằng voxel remesh rồi giảm lưới.
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    try:
        o.data.remesh_voxel_size = 0.009
        bpy.ops.object.voxel_remesh()
        o = bpy.context.active_object
        sm = o.modifiers.new("Soften", "SMOOTH"); sm.factor = 0.6; sm.iterations = 4
        dec = o.modifiers.new("Reduce", "DECIMATE"); dec.ratio = 0.2
        apply_all(o)
        # giữ đủ 2 ô vật liệu như bản cũ (Unity gán cùng một vật liệu ma cho cả hai): băng mũ dùng ô thứ hai
        while len(o.data.materials) < 2:
            o.data.materials.append(G)
        o.data.materials[0] = D
        o.data.materials[1] = G
        for poly in o.data.polygons:
            poly.material_index = 1 if 1.822 < poly.center.z < 1.848 else 0
        bpy.ops.object.shade_smooth()
    except Exception as e:
        print("REMESH FAILED", e)
        smooth(o, 50)
    o.name = "BW_LordGhost"


BW_BUILDERS = [
    ("BW_Plaque", lambda m: plaque(m, False)), ("BW_Plaque_Eye", lambda m: plaque(m, True)),
    ("BW_CandleStand", candle_stand), ("BW_Vademecum", vademecum), ("BW_Relic", relic),
    ("BW_Phone", phone), ("BW_Camcorder", camcorder), ("BW_Sedan", sedan),
    ("BW_Signpost", signpost), ("BW_Typewriter", typewriter), ("BW_LordGhost", lord_ghost),
    ("BW_Backpack", backpack),
]

only_bw = set(a for a in argv[1:] if not a.startswith("--"))
if "--preview" in argv:
    render_preview([n for n, _ in BW_BUILDERS], os.path.join(OUT, "preview_bw.png"))
elif "--icons" in argv:
    render_icons(["BW_Phone", "BW_Camcorder"], os.path.join(OUT, "icons"))
else:
    for name, fn in BW_BUILDERS:
        if only_bw and name not in only_bw:
            continue
        reset()
        fn(BW())
        export(name)
print("BW DONE")
