# Dựng đạo cụ Khóa Hồn bằng Blender (chạy nền):
#   blender.exe --background --factory-startup --python Tools/Blender/khoahon_props.py -- <output_dir>
# Mỗi đạo cụ xuất 1 file FBX (mét, trục Y-up cho Unity), vật liệu Principled có màu/độ nhám/kim loại.
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else os.path.join(os.getcwd(), "Tools", "Blender", "out")
os.makedirs(OUT, exist_ok=True)


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.curves):
        for b in list(block):
            block.remove(b)


def mat(name, color, rough=0.6, metal=0.0, emit=None, emit_strength=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1.0)
    p.inputs["Roughness"].default_value = rough
    p.inputs["Metallic"].default_value = metal
    if emit is not None:
        key = "Emission Color" if "Emission Color" in p.inputs else "Emission"
        p.inputs[key].default_value = (*emit, 1.0)
        p.inputs["Emission Strength"].default_value = emit_strength
    return m


def assign(obj, m):
    obj.data.materials.clear()
    obj.data.materials.append(m)


def smooth(obj, angle=40):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(angle))
    except Exception:
        bpy.ops.object.shade_smooth()
    obj.select_set(False)


def bevel(obj, width=0.004, seg=2):
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = seg
    mod.limit_method = "ANGLE"
    return mod


def add(kind, name, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), **kw):
    ops = {
        "cube": bpy.ops.mesh.primitive_cube_add,
        "cyl": bpy.ops.mesh.primitive_cylinder_add,
        "cone": bpy.ops.mesh.primitive_cone_add,
        "sphere": bpy.ops.mesh.primitive_uv_sphere_add,
        "torus": bpy.ops.mesh.primitive_torus_add,
    }
    ops[kind](location=loc, rotation=rot, **kw)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return o


def lathe(name, profile, segments=48):
    """Tạo vật tròn xoay từ profile [(r, z), ...] quanh trục Z."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    rings = []
    for i in range(segments):
        a = 2 * math.pi * i / segments
        ring = [bm.verts.new((r * math.cos(a), r * math.sin(a), z)) for r, z in profile]
        rings.append(ring)
    for i in range(segments):
        r0, r1 = rings[i], rings[(i + 1) % segments]
        for j in range(len(profile) - 1):
            bm.faces.new((r0[j], r1[j], r1[j + 1], r0[j + 1]))
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(o)
    return o


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    return o


def export(name):
    for o in bpy.context.scene.objects:
        for m in list(o.modifiers):
            bpy.context.view_layer.objects.active = o
            try:
                bpy.ops.object.modifier_apply(modifier=m.name)
            except Exception:
                pass
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             mesh_smooth_type="FACE", use_mesh_modifiers=True, bake_space_transform=True)
    print("EXPORTED", path)


# ---------------------------------------------------------------- vật liệu dùng chung
def M():
    return {
        "bronze": mat("M_Bronze", (0.42, 0.27, 0.12), 0.35, 1.0),
        "bronze_dark": mat("M_BronzeDark", (0.16, 0.11, 0.06), 0.5, 0.9),
        "iron": mat("M_Iron", (0.10, 0.10, 0.11), 0.55, 1.0),
        "rust": mat("M_Rust", (0.28, 0.12, 0.05), 0.85, 0.3),
        "wood_dark": mat("M_WoodDark", (0.09, 0.045, 0.025), 0.55),
        "lacquer_red": mat("M_LacquerRed", (0.33, 0.03, 0.02), 0.3),
        "gold": mat("M_Gilt", (0.75, 0.52, 0.18), 0.3, 1.0),
        "blood": mat("M_BloodBlack", (0.02, 0.0, 0.0), 0.05, 0.0, (0.35, 0.02, 0.01), 0.6),
        "cloth_beige": mat("M_ClothBeige", (0.55, 0.46, 0.34), 0.95),
        "cloth_red": mat("M_ClothRed", (0.40, 0.05, 0.05), 0.8),
        "horn": mat("M_Horn", (0.20, 0.13, 0.07), 0.4),
        "paper": mat("M_PaperOld", (0.74, 0.63, 0.42), 0.9),
        "plastic": mat("M_PlasticBlack", (0.03, 0.03, 0.03), 0.4),
        "plastic_red": mat("M_PlasticRed", (0.55, 0.04, 0.03), 0.35),
        "chrome": mat("M_Chrome", (0.8, 0.8, 0.82), 0.15, 1.0),
        "glass": mat("M_LensGlass", (0.02, 0.03, 0.04), 0.02, 0.0),
        "wax": mat("M_Wax", (0.85, 0.80, 0.66), 0.6),
        "flame": mat("M_Flame", (1.0, 0.6, 0.2), 0.5, 0.0, (1.0, 0.55, 0.15), 8.0),
        "led": mat("M_LedRed", (0.4, 0.0, 0.0), 0.3, 0.0, (1.0, 0.05, 0.02), 4.0),
        "stone": mat("M_StoneAltar", (0.22, 0.21, 0.20), 0.9),
        "skin": mat("M_DollSkin", (0.62, 0.50, 0.40), 0.9),
        "steel": mat("M_SteelSafe", (0.12, 0.14, 0.13), 0.45, 0.9),
    }


# ---------------------------------------------------------------- đạo cụ
def an_dong(m):
    """Ấn Đồng: khối ấn vuông, núm hình thú trừu tượng, chữ triện khắc nổi."""
    base = add("cube", "Base", (0, 0, 0.06), (0.09, 0.09, 0.06))
    bevel(base, 0.006, 3)
    neck = add("cyl", "Neck", (0, 0, 0.14), (0.035, 0.035, 0.03), vertices=24)
    knob = add("sphere", "Knob", (0, 0, 0.2), (0.06, 0.045, 0.05), segments=24, ring_count=12)
    ears = [add("torus", "Ring", (0, 0, 0.2), (1, 1, 1), (math.pi / 2, 0, 0), major_radius=0.05, minor_radius=0.008)]
    for o in (base, neck, knob, *ears):
        assign(o, m["bronze"])
    seal = []
    for i in range(3):
        for j in range(3):
            if (i + j) % 2 == 0:
                s = add("cube", "Glyph", (-0.05 + i * 0.05, -0.05 + j * 0.05, -0.002), (0.018, 0.018, 0.004))
                assign(s, m["bronze_dark"])
                seal.append(s)
    o = join([base, neck, knob, *ears, *seal], "KH_AnDong")
    smooth(o)


def vac(m):
    """Vạc máu đen: thân tròn xoay, 3 chân, 2 quai, mặt chất lỏng đen bóng."""
    prof = [(0.0, 0.25), (0.55, 0.25), (0.78, 0.45), (0.86, 0.75), (0.80, 1.0), (0.84, 1.05), (0.80, 1.07), (0.74, 1.0), (0.72, 0.78), (0.60, 0.4), (0.0, 0.35)]
    body = lathe("Body", prof, 64)
    assign(body, m["bronze_dark"])
    legs = []
    for k in range(3):
        a = k * 2 * math.pi / 3
        leg = add("cone", "Leg", (0.5 * math.cos(a), 0.5 * math.sin(a), 0.16), (0.09, 0.09, 0.2), vertices=12)
        assign(leg, m["bronze_dark"])
        legs.append(leg)
    handles = []
    for s in (-1, 1):
        h = add("torus", "Handle", (s * 0.86, 0, 1.15), (1, 1, 1), (math.pi / 2, 0, math.pi / 2), major_radius=0.13, minor_radius=0.025)
        assign(h, m["bronze"])
        handles.append(h)
    liquid = add("cyl", "Liquid", (0, 0, 0.95), (0.74, 0.74, 0.005), vertices=64)
    assign(liquid, m["blood"])
    o = join([body, *legs, *handles, liquid], "KH_VacMau")
    smooth(o, 50)


def bai_vi(m, marked):
    """Bài vị sơn mài đỏ viền vàng; marked = Mắt Trận (có ô chữ vàng)."""
    stand = add("cube", "Stand", (0, 0, 0.03), (0.09, 0.05, 0.03))
    board = add("cube", "Board", (0, 0, 0.27), (0.07, 0.015, 0.21))
    top = add("cyl", "Top", (0, 0, 0.49), (0.075, 0.075, 0.018), (math.pi / 2, 0, 0), vertices=24)
    for o in (stand, top):
        assign(o, m["wood_dark"])
        bevel(o, 0.004)
    assign(board, m["lacquer_red"])
    bevel(board, 0.003)
    parts = [stand, board, top]
    if marked:
        plate = add("cube", "Plate", (0, -0.016, 0.27), (0.045, 0.002, 0.16))
        assign(plate, m["gold"])
        parts.append(plate)
    o = join(parts, "KH_BaiVi" + ("_MatTran" if marked else ""))
    smooth(o)


def dai_sen(m):
    """Đài sen gỗ thếp vàng: đế tròn + 2 tầng cánh sen + mặt phẳng đặt bùa."""
    base = lathe("Base", [(0.0, 0.0), (0.16, 0.0), (0.16, 0.03), (0.10, 0.06), (0.08, 0.1), (0.0, 0.1)], 40)
    assign(base, m["wood_dark"])
    petals = []
    for layer, (r, z, n, tilt) in enumerate(((0.15, 0.12, 12, 0.9), (0.11, 0.15, 10, 0.6))):
        for k in range(n):
            a = k * 2 * math.pi / n + layer * 0.3
            p = add("sphere", "Petal", (r * math.cos(a), r * math.sin(a), z), (0.035, 0.012, 0.055), (tilt, 0, a + math.pi / 2), segments=12, ring_count=8)
            p.rotation_euler = (0, 0, 0)
            assign(p, m["gold"])
            petals.append(p)
    top = add("cyl", "Top", (0, 0, 0.165), (0.1, 0.1, 0.01), vertices=40)
    assign(top, m["gold"])
    o = join([base, *petals, top], "KH_DaiSen")
    smooth(o)


def bup_be(m):
    """Búp bê vải: đầu tròn, thân bao vải, tay chân bằng ống vải, nút mắt đen."""
    head = add("sphere", "Head", (0, 0, 0.21), (0.045, 0.042, 0.048), segments=24, ring_count=12)
    assign(head, m["cloth_beige"])
    body = lathe("Body", [(0.0, 0.05), (0.04, 0.05), (0.055, 0.09), (0.045, 0.15), (0.025, 0.17), (0.0, 0.17)], 24)
    assign(body, m["cloth_red"])
    parts = [head, body]
    for s in (-1, 1):
        arm = add("cyl", "Arm", (s * 0.06, 0, 0.13), (0.012, 0.012, 0.035), (0, s * 0.6, 0), vertices=12)
        leg = add("cyl", "Leg", (s * 0.022, 0, 0.025), (0.014, 0.014, 0.03), vertices=12)
        eye = add("sphere", "Eye", (s * 0.016, -0.04, 0.22), (0.007, 0.004, 0.007), segments=8, ring_count=6)
        assign(arm, m["cloth_beige"]); assign(leg, m["cloth_beige"]); assign(eye, m["plastic"])
        parts += [arm, leg, eye]
    o = join(parts, "KH_BupBe")
    smooth(o)


def luoc(m):
    """Lược sừng gãy: thân cong + răng lược, một đầu gãy."""
    spine = add("cube", "Spine", (0, 0, 0.03), (0.06, 0.004, 0.012))
    assign(spine, m["horn"])
    bevel(spine, 0.002)
    teeth = []
    for i in range(18):
        x = -0.055 + i * 0.0065
        length = 0.025 if i < 13 else 0.008
        t = add("cube", "Tooth", (x, 0, 0.03 - 0.012 - length / 2), (0.0018, 0.003, length / 2))
        assign(t, m["horn"])
        teeth.append(t)
    o = join([spine, *teeth], "KH_Luoc")
    o.rotation_euler = (0, 0, 0.1)


def bua(m):
    """Búa đóng đinh: cán gỗ, đầu sắt có vuốt nhổ đinh, vết máu."""
    handle = add("cyl", "Handle", (0, 0, 0.15), (0.014, 0.018, 0.15), vertices=16)
    assign(handle, m["wood_dark"])
    head = add("cube", "Head", (0.02, 0, 0.31), (0.06, 0.016, 0.018))
    assign(head, m["iron"])
    bevel(head, 0.003)
    face = add("cyl", "Face", (0.085, 0, 0.31), (0.02, 0.02, 0.012), (0, math.pi / 2, 0), vertices=16)
    claw = add("cone", "Claw", (-0.06, 0, 0.3), (0.016, 0.02, 0.04), (0, -math.pi / 2 - 0.4, 0), vertices=8)
    stain = add("sphere", "Blood", (0.06, -0.012, 0.3), (0.03, 0.004, 0.02), segments=12, ring_count=8)
    assign(face, m["iron"]); assign(claw, m["iron"]); assign(stain, m["rust"])
    o = join([handle, head, face, claw, stain], "KH_Bua")
    smooth(o, 30)


def hai_theu(m):
    """Đôi hài thêu: đế gỗ + mũi hài cong, thân vải đỏ thêu viền vàng."""
    parts = []
    for s in (-1, 1):
        sole = add("cube", "Sole", (s * 0.05, 0, 0.008), (0.032, 0.1, 0.008))
        upper = add("sphere", "Upper", (s * 0.05, 0.02, 0.03), (0.032, 0.075, 0.028), segments=20, ring_count=10)
        toe = add("cone", "Toe", (s * 0.05, 0.11, 0.035), (0.012, 0.012, 0.03), (-math.pi / 2 + 0.6, 0, 0), vertices=10)
        trim = add("torus", "Trim", (s * 0.05, -0.02, 0.05), (0.03, 0.05, 0.05), major_radius=1.0, minor_radius=0.08)
        assign(sole, m["wood_dark"]); assign(upper, m["cloth_red"]); assign(toe, m["cloth_red"]); assign(trim, m["gold"])
        parts += [sole, upper, toe, trim]
    o = join(parts, "KH_HaiTheu")
    smooth(o)


def coi(m):
    """Còi nhựa đỏ kỷ niệm của Quân, có khoen + dây."""
    body = add("cyl", "Body", (0, 0, 0), (0.012, 0.012, 0.03), (0, math.pi / 2, 0), vertices=24)
    mouth = add("cube", "Mouth", (-0.04, 0, 0.004), (0.015, 0.008, 0.005))
    ring = add("torus", "Ring", (0.032, 0, 0.012), (1, 1, 1), (math.pi / 2, 0, 0), major_radius=0.008, minor_radius=0.0015)
    for o in (body, mouth):
        assign(o, m["plastic_red"])
    assign(ring, m["chrome"])
    o = join([body, mouth, ring], "KH_Coi")
    smooth(o)


def may_ghi_am(m):
    """Máy ghi âm cassette cầm tay: thân, cửa sổ băng, 2 cuộn, phím, loa, LED đỏ."""
    body = add("cube", "Body", (0, 0, 0), (0.065, 0.018, 0.045))
    bevel(body, 0.004, 3)
    assign(body, m["plastic"])
    window = add("cube", "Window", (0, -0.0185, 0.008), (0.04, 0.001, 0.02))
    assign(window, m["glass"])
    parts = [body, window]
    for s in (-1, 1):
        reel = add("cyl", "Reel", (s * 0.02, -0.0175, 0.008), (0.011, 0.011, 0.002), (math.pi / 2, 0, 0), vertices=16)
        assign(reel, m["chrome"])
        parts.append(reel)
    for i in range(5):
        key = add("cube", "Key", (-0.04 + i * 0.016, 0, 0.047), (0.006, 0.012, 0.004))
        assign(key, m["chrome"] if i != 0 else m["plastic_red"])
        parts.append(key)
    grill = add("cube", "Grill", (0, -0.0185, -0.028), (0.05, 0.001, 0.012))
    assign(grill, m["iron"])
    led = add("cube", "LED", (0.052, -0.0185, 0.034), (0.003, 0.001, 0.003))
    assign(led, m["led"])
    o = join(parts + [grill], "KH_MayGhiAm")
    led.name = "KH_MayGhiAm_LED"


def may_anh(m):
    """Máy ảnh film rangefinder: thân, ống kính, đèn flash, nút chụp, cần lên phim."""
    body = add("cube", "Body", (0, 0, 0), (0.068, 0.022, 0.04))
    bevel(body, 0.004, 3)
    assign(body, m["plastic"])
    top = add("cube", "TopPlate", (0, 0, 0.042), (0.068, 0.022, 0.006))
    assign(top, m["chrome"])
    lens = lathe("Lens", [(0.0, 0.0), (0.022, 0.0), (0.022, 0.02), (0.019, 0.035), (0.0, 0.035)], 32)
    lens.rotation_euler = (math.pi / 2, 0, 0)
    lens.location = (0, -0.022, -0.002)
    bpy.context.view_layer.objects.active = lens
    lens.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    lens.select_set(False)
    assign(lens, m["iron"])
    glass = add("cyl", "Glass", (0, -0.058, -0.002), (0.016, 0.016, 0.002), (math.pi / 2, 0, 0), vertices=32)
    assign(glass, m["glass"])
    flash = add("cube", "Flash", (0.045, -0.005, 0.06), (0.018, 0.014, 0.012))
    assign(flash, m["chrome"])
    flash_glass = add("cube", "FlashGlass", (0.045, -0.0195, 0.06), (0.015, 0.001, 0.009))
    assign(flash_glass, m["wax"])
    btn = add("cyl", "Shutter", (-0.045, 0, 0.05), (0.006, 0.006, 0.004), vertices=16)
    assign(btn, m["chrome"])
    o = join([body, top, lens, glass, flash, flash_glass, btn], "KH_MayAnh")
    smooth(o, 35)


def xa_beng(m):
    """Xà beng: thanh thép lục giác, đầu cong, đầu dẹt."""
    curve = bpy.data.curves.new("CrowbarCurve", "CURVE")
    curve.dimensions = "3D"
    sp = curve.splines.new("BEZIER")
    pts = [(0, 0, 0), (0, 0, 0.5), (0, 0.04, 0.6), (0, 0.1, 0.58)]
    sp.bezier_points.add(len(pts) - 1)
    for bp, p in zip(sp.bezier_points, pts):
        bp.co = p
        bp.handle_left_type = bp.handle_right_type = "AUTO"
    curve.bevel_depth = 0.011
    curve.bevel_resolution = 1
    o = bpy.data.objects.new("KH_XaBeng", curve)
    bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.convert(target="MESH")
    o = bpy.context.active_object
    assign(o, m["rust"])
    tip = add("cube", "Tip", (0, 0, -0.01), (0.014, 0.004, 0.02))
    assign(tip, m["iron"])
    o = join([o, tip], "KH_XaBeng")
    smooth(o, 50)


def ket_sat(m):
    """Két sắt cổ: thân thép, cửa có bàn phím 4 số cơ, tay nắm, bản lề."""
    body = add("cube", "Body", (0, 0, 0.45), (0.36, 0.33, 0.45))
    bevel(body, 0.015, 3)
    assign(body, m["steel"])
    door = add("cube", "Door", (0, -0.335, 0.45), (0.3, 0.012, 0.38))
    bevel(door, 0.006)
    assign(door, m["steel"])
    pad = add("cube", "Keypad", (-0.05, -0.35, 0.55), (0.08, 0.006, 0.1))
    assign(pad, m["bronze_dark"])
    parts = [body, door, pad]
    for r in range(4):
        for c in range(3):
            k = add("cyl", "Key", (-0.09 + c * 0.04, -0.358, 0.62 - r * 0.045), (0.012, 0.012, 0.005), (math.pi / 2, 0, 0), vertices=12)
            assign(k, m["bronze"])
            parts.append(k)
    handle = add("torus", "Handle", (0.17, -0.36, 0.42), (1, 1, 1), (math.pi / 2, 0, 0), major_radius=0.06, minor_radius=0.01)
    assign(handle, m["chrome"])
    for z in (0.2, 0.7):
        h = add("cyl", "Hinge", (-0.3, -0.345, z), (0.015, 0.015, 0.05), vertices=12)
        assign(h, m["iron"])
        parts.append(h)
    o = join(parts + [handle], "KH_KetSat")
    smooth(o, 30)


def nen(m):
    """Nến sáp trên đế đồng; ngọn lửa phát sáng (để Unity tạo Light)."""
    holder = lathe("Holder", [(0.0, 0.0), (0.05, 0.0), (0.05, 0.008), (0.015, 0.02), (0.012, 0.05), (0.03, 0.06), (0.03, 0.065), (0.0, 0.065)], 32)
    assign(holder, m["bronze"])
    wax = lathe("Wax", [(0.0, 0.065), (0.016, 0.065), (0.016, 0.17), (0.013, 0.178), (0.0, 0.175)], 24)
    assign(wax, m["wax"])
    flame = add("sphere", "Flame", (0, 0, 0.195), (0.006, 0.006, 0.016), segments=12, ring_count=8)
    assign(flame, m["flame"])
    o = join([holder, wax, flame], "KH_Nen")
    smooth(o)


def xich(m):
    """Đoạn xích 12 mắt (sắt gỉ), dài ~0.9 m."""
    links = []
    for i in range(12):
        l = add("torus", "Link", (0, 0, i * 0.075), (1, 1.6, 1), (0, 0, (i % 2) * math.pi / 2), major_radius=0.03, minor_radius=0.008)
        l.rotation_euler = (math.pi / 2, 0, (i % 2) * math.pi / 2)
        assign(l, m["rust"])
        links.append(l)
    join(links, "KH_Xich")


def nap_ham(m):
    """Nắp hầm sắt 4.4 × 1.6 m có gân, 2 vòng kéo."""
    plate = add("cube", "Plate", (0, 0, 0.03), (2.2, 0.8, 0.03))
    assign(plate, m["iron"])
    parts = [plate]
    for i in range(7):
        r = add("cube", "Rib", (-1.8 + i * 0.6, 0, 0.07), (0.04, 0.78, 0.02))
        assign(r, m["rust"])
        parts.append(r)
    for x in (-0.6, 0.6):
        ring = add("torus", "Pull", (x, 0, 0.1), (1, 1, 1), (math.pi / 2, 0, 0), major_radius=0.07, minor_radius=0.012)
        assign(ring, m["iron"])
        parts.append(ring)
    join(parts, "KH_NapHam")


def chum_chia_khoa(m):
    """Chùm chìa khóa sắt cổ: vòng lớn + 7 chìa đủ kiểu, treo lủng lẳng (gốc ở đỉnh vòng)."""
    ring = add("torus", "Ring", (0, 0, -0.05), (1, 1, 1), (math.pi / 2, 0, 0), major_radius=0.05, minor_radius=0.005)
    assign(ring, m["iron"])
    parts = [ring]
    for k in range(7):
        a = -0.9 + k * 0.3
        length = 0.07 + (k % 3) * 0.02
        cx, cz = math.sin(a) * 0.05, -0.05 - math.cos(a) * 0.05
        bow = add("torus", "Bow", (cx, 0, cz - 0.012), (1, 1, 1), (math.pi / 2, 0, 0), major_radius=0.012, minor_radius=0.003)
        shaft = add("cyl", "Shaft", (cx + math.sin(a) * length / 2, 0, cz - 0.024 - math.cos(a) * length / 2), (0.0035, 0.0035, length / 2), (0, a, 0), vertices=8)
        bit = add("cube", "Bit", (cx + math.sin(a) * length, 0.006, cz - 0.024 - math.cos(a) * length), (0.006, 0.006, 0.008), (0, a, 0))
        mt = m["rust"] if k % 2 else m["bronze_dark"]
        for o in (bow, shaft, bit):
            assign(o, mt)
        parts += [bow, shaft, bit]
    join(parts, "KH_ChumChiaKhoa")


def ao_ba_ba_than(m):
    """Thân áo bà ba (đen, vải thô): ống rộng từ vai xuống hông, cổ tròn, hàng nút; trục +Z (Unity +Y), cao 1 đơn vị (scale theo xương)."""
    cloth = mat("M_AoBaBa", (0.025, 0.022, 0.024), 0.92)
    prof = [(0.0, 1.0), (0.07, 1.0), (0.085, 0.97), (0.15, 0.93), (0.175, 0.86), (0.17, 0.6), (0.165, 0.35), (0.18, 0.12), (0.19, 0.0)]
    outer = lathe("Than", prof, 40)
    outer.scale = (1.0, 0.72, 1.0)
    bpy.context.view_layer.objects.active = outer
    outer.select_set(True)
    bpy.ops.object.transform_apply(scale=True)
    outer.select_set(False)
    sol = outer.modifiers.new("Solid", "SOLIDIFY")
    sol.thickness = 0.008
    assign(outer, cloth)
    buttons = []
    for i in range(5):
        b = add("sphere", "Nut", (0.03, -0.13, 0.85 - i * 0.12), (0.008, 0.004, 0.008), segments=8, ring_count=6)
        assign(b, m["horn"])
        buttons.append(b)
    o = join([outer, *buttons], "KH_AoBaBa_Than")
    smooth(o, 50)


def ao_ba_ba_vat(m):
    """Hai vạt áo trước/sau từ hông xuống đùi, hơi xòe; gốc ở mép trên, rủ theo -Z."""
    cloth = mat("M_AoBaBa", (0.025, 0.022, 0.024), 0.92)
    parts = []
    for s in (-1, 1):
        flap = add("cube", "Vat", (0, s * 0.11, -0.2), (0.17, 0.008, 0.2))
        bm_obj = flap
        mod = bm_obj.modifiers.new("Taper", "SIMPLE_DEFORM")
        mod.deform_method = "TAPER"
        mod.factor = -0.35
        mod.deform_axis = "Z"
        assign(flap, cloth)
        parts.append(flap)
    join(parts, "KH_AoBaBa_Vat")


def ao_ba_ba_tay(m):
    """Ống tay áo rộng (dài 1 đơn vị theo +Z, scale theo chiều dài xương), miệng tay loe."""
    cloth = mat("M_AoBaBa", (0.025, 0.022, 0.024), 0.92)
    o = lathe("Tay", [(0.058, 0.0), (0.06, 0.4), (0.066, 0.8), (0.075, 1.0)], 24)
    sol = o.modifiers.new("Solid", "SOLIDIFY")
    sol.thickness = 0.006
    assign(o, cloth)
    o.name = "KH_AoBaBa_Tay"
    smooth(o, 60)


def toc_dai(m):
    """Tóc dài đen xõa sau lưng: dải cong từ đỉnh đầu rủ xuống giữa lưng (gốc ở đỉnh đầu, rủ -Z, hướng sau = +Y)."""
    hair = mat("M_TocDai", (0.012, 0.010, 0.010), 0.45)
    me = bpy.data.meshes.new("Toc")
    bm = bmesh.new()
    rows, cols = 14, 9
    verts = []
    for r in range(rows):
        t = r / (rows - 1)
        z = -t * 0.75
        back = 0.07 + 0.05 * math.sin(t * math.pi * 0.8)
        width = 0.11 + 0.07 * t
        row = []
        for c in range(cols):
            u = c / (cols - 1) * 2 - 1
            x = u * width
            y = back + (1 - u * u) * 0.03 - (0.05 if t < 0.15 else 0)
            row.append(bm.verts.new((x, y, z)))
        verts.append(row)
    for r in range(rows - 1):
        for c in range(cols - 1):
            bm.faces.new((verts[r][c], verts[r][c + 1], verts[r + 1][c + 1], verts[r + 1][c]))
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new("KH_TocDai", me)
    bpy.context.collection.objects.link(o)
    sol = o.modifiers.new("Solid", "SOLIDIFY")
    sol.thickness = 0.012
    assign(o, hair)
    smooth(o, 70)



def M_bone():
    return mat("M_BoneOld", (0.62, 0.55, 0.42), 0.75), mat("M_BloodDried", (0.12, 0.01, 0.01), 0.35)


def so_nguoi(m):
    """Sọ người: hộp sọ + hàm, hốc mắt sâu, răng; vết máu khô."""
    bone, blood = M_bone()
    cran = add("sphere", "Cranium", (0, 0, 0.11), (0.075, 0.095, 0.08), segments=32, ring_count=16)
    face = add("sphere", "Face", (0, -0.06, 0.07), (0.06, 0.05, 0.06), segments=24, ring_count=12)
    jaw = add("cube", "Jaw", (0, -0.065, 0.015), (0.045, 0.03, 0.018))
    bevel(jaw, 0.01, 3)
    parts = [cran, face, jaw]
    for o in parts:
        assign(o, bone)
    for s in (-1, 1):
        eye = add("sphere", "Eye", (s * 0.028, -0.1, 0.085), (0.02, 0.02, 0.022), segments=12, ring_count=8)
        assign(eye, mat("M_Void", (0.0, 0.0, 0.0), 1.0))
        parts.append(eye)
    nose = add("cone", "Nose", (0, -0.112, 0.055), (0.012, 0.008, 0.015), (math.pi / 2, 0, 0), vertices=3)
    assign(nose, mat("M_Void", (0.0, 0.0, 0.0), 1.0)); parts.append(nose)
    for i in range(8):
        t = add("cube", "Tooth", (-0.026 + i * 0.0075, -0.096, 0.03), (0.003, 0.003, 0.006))
        assign(t, bone); parts.append(t)
    st = add("sphere", "Stain", (0.03, -0.05, 0.16), (0.04, 0.05, 0.02), segments=12, ring_count=8)
    assign(st, blood); parts.append(st)
    o = join(parts, "KH_SoNguoi")
    smooth(o, 50)


def xuong_dui(m):
    bone, blood = M_bone()
    shaft = add("cyl", "Shaft", (0, 0, 0.22), (0.016, 0.016, 0.2), vertices=12)
    parts = [shaft]
    for z in (0.02, 0.42):
        for s in (-1, 1):
            k = add("sphere", "Knob", (s * 0.014, 0, z), (0.022, 0.02, 0.02), segments=12, ring_count=8)
            parts.append(k)
    for o in parts:
        assign(o, bone)
    o = join(parts, "KH_XuongDui")
    smooth(o, 50)


def long_nguc(m):
    bone, blood = M_bone()
    spine = add("cyl", "Spine", (0, 0.06, 0.15), (0.015, 0.015, 0.17), vertices=10)
    parts = [spine]
    for i in range(7):
        z = 0.27 - i * 0.035
        r = 0.09 + 0.012 * math.sin(i / 6 * math.pi)
        rib = add("torus", "Rib", (0, 0, z), (1.0, 0.75, 1.0), major_radius=r, minor_radius=0.006)
        parts.append(rib)
    for o in parts:
        assign(o, bone)
    o = join(parts, "KH_LongNguc")
    smooth(o, 60)


def dong_xuong(m):
    """Đống xương vụn trộn sọ (đặt trong vách hầm / quanh vạc)."""
    bone, blood = M_bone()
    import random
    random.seed(5)
    parts = []
    for i in range(26):
        x, y = random.uniform(-0.35, 0.35), random.uniform(-0.25, 0.25)
        b = add("cyl", "Bone", (x, y, 0.02 + random.uniform(0, 0.1)), (0.012, 0.012, random.uniform(0.08, 0.2)),
                (random.uniform(1.2, 1.9), 0, random.uniform(0, 6.28)), vertices=8)
        assign(b, bone if i % 5 else blood)
        parts.append(b)
    for i in range(3):
        s = add("sphere", "Skull", (random.uniform(-0.25, 0.25), random.uniform(-0.15, 0.15), 0.08), (0.07, 0.085, 0.07), segments=16, ring_count=10)
        assign(s, bone); parts.append(s)
        for e in (-1, 1):
            h = add("sphere", "Socket", (s.location.x + e * 0.025, s.location.y - 0.075, 0.09), (0.017, 0.012, 0.018), segments=8, ring_count=6)
            assign(h, mat("M_Void", (0.0, 0.0, 0.0), 1.0)); parts.append(h)
    o = join(parts, "KH_DongXuong")
    smooth(o, 50)


def gie_mau(m):
    """Giẻ rách đẫm máu vứt dưới sàn."""
    blood = mat("M_RagBlood", (0.22, 0.02, 0.015), 0.6)
    o = add("cube", "Rag", (0, 0, 0.005), (0.25, 0.18, 0.004))
    sub = o.modifiers.new("Sub", "SUBSURF"); sub.levels = 2
    disp = o.modifiers.new("Wave", "WAVE"); disp.height = 0.015; disp.width = 0.12
    assign(o, blood)
    o.name = "KH_GieMau"


def moc_treo(m):
    """Móc sắt treo trên xích (trần hầm), dính máu."""
    _, blood = M_bone()
    links = []
    for i in range(10):
        l = add("torus", "Link", (0, 0, -i * 0.075), (1, 1.6, 1), (math.pi / 2, 0, (i % 2) * math.pi / 2), major_radius=0.028, minor_radius=0.007)
        assign(l, m["rust"]); links.append(l)
    curve = bpy.data.curves.new("Hook", "CURVE"); curve.dimensions = "3D"
    sp = curve.splines.new("BEZIER"); pts = [(0, 0, -0.75), (0, 0, -0.95), (0.08, 0, -1.02), (0.12, 0, -0.92)]
    sp.bezier_points.add(len(pts) - 1)
    for bp, p in zip(sp.bezier_points, pts):
        bp.co = p; bp.handle_left_type = bp.handle_right_type = "AUTO"
    curve.bevel_depth = 0.012
    hook = bpy.data.objects.new("HookObj", curve); bpy.context.collection.objects.link(hook)
    bpy.context.view_layer.objects.active = hook; hook.select_set(True); bpy.ops.object.convert(target="MESH")
    hook = bpy.context.active_object; assign(hook, m["iron"])
    drip = add("sphere", "Drip", (0.12, 0, -0.95), (0.02, 0.02, 0.04), segments=10, ring_count=8)
    assign(drip, blood)
    join(links + [hook, drip], "KH_MocTreo")


def xac_quan(m):
    """Xác Quân trong xà lim: thi thể nằm co, bọc vải liệm cột dây, máu thấm loang, một bàn tay xám thò ra."""
    shroud = mat("M_Shroud", (0.30, 0.26, 0.20), 0.93)
    dirt = mat("M_ShroudDirt", (0.13, 0.09, 0.06), 0.95)
    soak = mat("M_ShroudBlood", (0.16, 0.006, 0.005), 0.3)
    rope = mat("M_Rope", (0.18, 0.13, 0.08), 0.95)
    skin = mat("M_CorpseHand", (0.27, 0.28, 0.27), 0.55)
    wr = bpy.data.textures.new("Wrinkle", "CLOUDS"); wr.noise_scale = 0.06
    def wrinkle(o, h=0.012):
        sub = o.modifiers.new("Sub", "SUBSURF"); sub.levels = 2
        d = o.modifiers.new("Disp", "DISPLACE"); d.texture = wr; d.strength = h; d.mid_level = 0.5
    def apply_mods(o):
        bpy.context.view_layer.objects.active = o
        for md in list(o.modifiers):
            bpy.ops.object.modifier_apply(modifier=md.name)
    def hug(o, target, off):
        sw = o.modifiers.new("Hug", "SHRINKWRAP"); sw.target = target; sw.wrap_method = "NEAREST_SURFACEPOINT"; sw.offset = off
        apply_mods(o)
    # Thân nằm nghiêng (dọc trục Y), lưng còng, chân gập
    pieces = []
    torso = add("sphere", "Torso", (0, 0.05, 0.19), (0.17, 0.36, 0.19), segments=32, ring_count=16)
    head = add("sphere", "Head", (0.02, 0.5, 0.17), (0.11, 0.12, 0.12), segments=24, ring_count=12)
    neck = add("cyl", "Neck", (0.01, 0.4, 0.18), (0.07, 0.07, 0.07), (math.pi / 2, 0, 0), vertices=16)
    thigh = add("cyl", "Thigh", (-0.12, -0.28, 0.17), (0.11, 0.11, 0.24), (math.radians(80), 0, math.radians(-55)), vertices=16)
    shin = add("cyl", "Shin", (-0.3, -0.12, 0.13), (0.085, 0.085, 0.22), (math.radians(90), 0, math.radians(35)), vertices=16)
    knee = add("sphere", "Knee", (-0.3, -0.38, 0.15), (0.11, 0.11, 0.11), segments=16, ring_count=10)
    foot = add("sphere", "Feet", (-0.36, 0.08, 0.11), (0.09, 0.12, 0.09), segments=16, ring_count=10)
    for o in (torso, head, neck, thigh, shin, knee, foot):
        assign(o, shroud); wrinkle(o); apply_mods(o); pieces.append(o)
    body = join(pieces, "Body")
    parts = [body]
    import random
    random.seed(11)
    # Máu thấm và vết bẩn: mảng mỏng "dán" sát mặt vải
    stains = [(0.12, 0.12, 0.28, 0.13, 0.2, soak), (0.08, 0.52, 0.22, 0.09, 0.09, soak), (0.15, -0.05, 0.14, 0.09, 0.15, soak),
              (0.0, 0.32, 0.36, 0.1, 0.11, soak), (0.1, 0.55, 0.1, 0.07, 0.07, soak), (-0.22, -0.32, 0.24, 0.1, 0.12, soak),
              (-0.1, 0.0, 0.36, 0.14, 0.2, dirt), (-0.34, 0.05, 0.16, 0.08, 0.1, dirt), (-0.15, -0.42, 0.1, 0.12, 0.1, dirt)]
    for (x, y, z, sx, sy, mm) in stains:
        b = add("sphere", "Stain", (x, y, z), (sx, sy, min(sx, sy)), segments=24, ring_count=12)
        wrinkle(b, 0.03); apply_mods(b); hug(b, body, 0.003); assign(b, mm); parts.append(b)
    # Dây cột vải liệm, ôm sát thân
    for y in (0.3, 0.02, -0.2, 0.56):
        r = add("torus", "Rope", (0, y, 0.18), (1, 1, 1), (math.pi / 2, 0, 0), major_radius=0.24, minor_radius=0.01, major_segments=48, minor_segments=8)
        hug(r, body, 0.008); assign(r, rope); parts.append(r)
    # Bàn tay xám thò ra khỏi vải, đặt sát sàn; đầu ngón dính máu
    wrist = add("cyl", "Wrist", (0.2, 0.15, 0.07), (0.028, 0.028, 0.055), (0, math.radians(127), 0), vertices=12)
    palm = add("cube", "Palm", (0.27, 0.15, 0.03), (0.04, 0.038, 0.011))
    bevel(palm, 0.006, 2); apply_mods(palm)
    for o in (wrist, palm):
        assign(o, skin); parts.append(o)
    for i in range(4):
        y = 0.15 - 0.027 + i * 0.018
        ln = 0.024 + (0.004 if i in (1, 2) else 0)
        f = add("cyl", "Finger", (0.31 + ln, y, 0.024), (0.0075, 0.0075, ln), (0, math.radians(90 + random.uniform(-8, 12)), math.radians(random.uniform(-10, 10))), vertices=8)
        t = add("sphere", "Tip", (0.31 + ln * 2, y, 0.022), (0.009, 0.009, 0.008), segments=8, ring_count=6)
        assign(f, skin); assign(t, soak); parts += [f, t]
    th = add("cyl", "Thumb", (0.28, 0.2, 0.026), (0.008, 0.008, 0.02), (0, math.radians(90), math.radians(55)), vertices=8)
    assign(th, skin); parts.append(th)
    o = join(parts, "KH_XacQuan")
    smooth(o, 60)


def render_preview(names, path):
    """Ảnh xem trước toàn bộ model (EEVEE) để soát nhanh."""
    reset()
    x = 0.0
    for n in names:
        fp = os.path.join(OUT, n + ".fbx")
        if not os.path.exists(fp):
            continue
        before = set(bpy.context.scene.objects)
        bpy.ops.import_scene.fbx(filepath=fp)
        objs = [o for o in bpy.context.scene.objects if o not in before]
        mx = 0.0
        for o in objs:
            mx = max(mx, max(o.dimensions))
        s = 0.5 / mx if mx > 0 else 1
        for o in objs:
            o.scale = (s, s, s)
            o.location = (x, 0, 0)
        x += 0.75
    bpy.ops.object.light_add(type="AREA", location=(x / 2, -3, 4))
    bpy.context.active_object.data.energy = 900
    bpy.context.active_object.data.size = 6
    bpy.ops.object.camera_add(location=(x / 2 - 0.35, -6.5, 1.6), rotation=(math.radians(80), 0, 0))
    cam = bpy.context.active_object
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = x + 0.5
    bpy.context.scene.camera = cam
    world = bpy.data.worlds.new("W"); world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.12, 0.12, 0.13, 1)
    bpy.context.scene.world = world
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = 1800, 300
    try:
        sc.render.engine = "BLENDER_EEVEE_NEXT"
    except Exception:
        sc.render.engine = "BLENDER_EEVEE"
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("PREVIEW", path)


BUILDERS = [
    ("KH_AnDong", an_dong), ("KH_VacMau", vac), ("KH_BaiVi", lambda m: bai_vi(m, False)),
    ("KH_BaiVi_MatTran", lambda m: bai_vi(m, True)), ("KH_DaiSen", dai_sen), ("KH_BupBe", bup_be),
    ("KH_Luoc", luoc), ("KH_Bua", bua), ("KH_HaiTheu", hai_theu), ("KH_Coi", coi),
    ("KH_MayGhiAm", may_ghi_am), ("KH_MayAnh", may_anh), ("KH_XaBeng", xa_beng), ("KH_KetSat", ket_sat),
    ("KH_Nen", nen), ("KH_Xich", xich), ("KH_NapHam", nap_ham), ("KH_ChumChiaKhoa", chum_chia_khoa),
    ("KH_AoBaBa_Than", ao_ba_ba_than), ("KH_AoBaBa_Vat", ao_ba_ba_vat), ("KH_AoBaBa_Tay", ao_ba_ba_tay), ("KH_TocDai", toc_dai),
    ("KH_SoNguoi", so_nguoi), ("KH_XuongDui", xuong_dui), ("KH_LongNguc", long_nguc), ("KH_DongXuong", dong_xuong), ("KH_GieMau", gie_mau), ("KH_MocTreo", moc_treo), ("KH_XacQuan", xac_quan),
]

def render_icons(names, out_dir):
    """Icon hotbar 256×256 nền trong suốt cho từng model."""
    os.makedirs(out_dir, exist_ok=True)
    for n in names:
        reset()
        fp = os.path.join(OUT, n + ".fbx")
        if not os.path.exists(fp):
            continue
        bpy.ops.import_scene.fbx(filepath=fp)
        objs = [o for o in bpy.context.scene.objects]
        mn = Vector((1e9, 1e9, 1e9)); mx = Vector((-1e9, -1e9, -1e9))
        for o in objs:
            for c in o.bound_box:
                w = o.matrix_world @ Vector(c)
                mn = Vector((min(mn.x, w.x), min(mn.y, w.y), min(mn.z, w.z)))
                mx = Vector((max(mx.x, w.x), max(mx.y, w.y), max(mx.z, w.z)))
        ctr = (mn + mx) / 2; size = max((mx - mn).x, (mx - mn).y, (mx - mn).z)
        bpy.ops.object.light_add(type="AREA", location=(ctr.x + size, ctr.y - size * 2, ctr.z + size * 2))
        bpy.context.active_object.data.energy = 60 * size * size + 20
        bpy.context.active_object.data.size = size * 3
        d = size * 2.2
        bpy.ops.object.camera_add(location=(ctr.x + d * 0.6, ctr.y - d, ctr.z + d * 0.55))
        cam = bpy.context.active_object
        cam.data.type = "ORTHO"; cam.data.ortho_scale = size * 1.25
        direction = ctr - cam.location
        cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        sc = bpy.context.scene; sc.camera = cam
        sc.render.resolution_x = sc.render.resolution_y = 256
        sc.render.film_transparent = True
        try:
            sc.render.engine = "BLENDER_EEVEE_NEXT"
        except Exception:
            sc.render.engine = "BLENDER_EEVEE"
        sc.render.filepath = os.path.join(out_dir, n + ".png")
        bpy.ops.render.render(write_still=True)
        print("ICON", sc.render.filepath)


only = set(a for a in argv[1:] if not a.startswith("--"))
if "--icons" in argv:
    render_icons(["KH_MayGhiAm", "KH_MayAnh", "KH_XaBeng", "KH_BupBe", "KH_Luoc", "KH_Bua", "KH_HaiTheu", "KH_Coi"], os.path.join(OUT, "icons"))
elif "--preview" in argv:
    render_preview([n for n, _ in BUILDERS], os.path.join(OUT, "preview.png"))
else:
    for name, fn in BUILDERS:
        if only and name not in only:
            continue
        reset()
        fn(M())
        export(name)
print("ALL DONE")
