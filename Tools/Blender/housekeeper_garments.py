# Trang phục + tóc cho Mrs. Peake (con ma quản gia) — dựng bằng Blender trên khung xương gốc của civilian_girl.
#
# Chạy trong Blender đang mở (qua MCP bridge):   exec(open(r".../Tools/Blender/housekeeper_garments.py", encoding="utf-8").read())
# hoặc chạy nền:  blender.exe --background --factory-startup --python Tools/Blender/housekeeper_garments.py
#
# Không sửa file asset pack: chỉ ĐỌC civilian_girl.FBX để lấy khung xương + hình thể, rồi xuất FBX mới gồm:
#   HK_Skirt  — váy tang dài thời Victoria, xòe, có nếp gấp, gấu rách (skin theo chân + hông để không xuyên khi bước)
#   HK_Hair   — tóc đen dài rũ che nửa mặt (dải tóc alpha), skin theo đầu → cổ → lưng
#   HK_Shawl  — khăn choàng vai rách che phần cổ/vai
# Unity (KhoaHonHousekeeperBuilder) gắn các lưới này vào đúng xương của bà theo tên.
import bpy, bmesh, math, os, random
from mathutils import Vector, kdtree, noise

ROOT = r"D:/refactored-adventure"
SRC = os.path.join(ROOT, "Assets", "civilian_girl", "Models", "civilian_girl.FBX")
for cand in (SRC, os.path.join(ROOT, "Assets", "ThirdParty", "civilian_girl", "Models", "civilian_girl.FBX")):
    if os.path.exists(cand):
        SRC = cand
        break
OUT_DIR = os.path.join(ROOT, "Tools", "Blender", "out", "characters")
OUT = os.path.join(OUT_DIR, "KH_HK_Garments.fbx")
os.makedirs(OUT_DIR, exist_ok=True)
random.seed(1921)

# ----------------------------------------------------------------------------- nhập khung xương gốc
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete()
for block in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials):
    for b in list(block):
        if b.users == 0:
            block.remove(b)
bpy.ops.import_scene.fbx(filepath=SRC, automatic_bone_orientation=False, use_anim=False)
arm = bpy.data.objects["Armature"]
BONES = {b.name for b in arm.data.bones}


def body_verts(names):
    """[(vị trí world, {xương: trọng số})] của các lưới thân gốc."""
    out = []
    for n in names:
        o = bpy.data.objects[n]
        gname = {g.index: g.name for g in o.vertex_groups}
        mw = o.matrix_world
        for v in o.data.vertices:
            w = {gname[g.group]: g.weight for g in v.groups if g.weight > 0.001}
            out.append((mw @ v.co, w))
    return out


LEGS = body_verts(["pant", "boot_left", "boot_right"])
TORSO = body_verts(["shirt_t", "chest", "pant"])
ARMS = body_verts(["hand_left", "hand_right"])


def envelope(z, band=0.045, src=LEGS):
    xs = [p.x for p, _ in src if abs(p.z - z) < band]
    ys = [p.y for p, _ in src if abs(p.z - z) < band]
    if not xs:
        return 0.12, -0.10, 0.12
    return max(abs(min(xs)), abs(max(xs))), min(ys), max(ys)


def make_tree(src):
    t = kdtree.KDTree(len(src))
    for i, (p, _) in enumerate(src):
        t.insert(p, i)
    t.balance()
    return t


def idw_weights(tree, src, co, k=12, remap=None, power=2.0):
    acc = {}
    for _, idx, d in tree.find_n(co, k):
        f = 1.0 / (d ** power + 1e-5)
        for g, w in src[idx][1].items():
            if remap:
                g = remap.get(g, g)
            acc[g] = acc.get(g, 0.0) + w * f
    s = sum(acc.values()) or 1.0
    return {g: w / s for g, w in acc.items()}


def top4(w):
    items = sorted(w.items(), key=lambda kv: -kv[1])[:4]
    s = sum(v for _, v in items) or 1.0
    return {g: v / s for g, v in items if v / s > 0.004}


def build_object(name, verts, faces, uvs, weights, mat_name):
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], faces)
    me.update()
    uvl = me.uv_layers.new(name="UVMap")
    for poly in me.polygons:
        for li in poly.loop_indices:
            uvl.data[li].uv = uvs[me.loops[li].vertex_index]
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    m = bpy.data.materials.get(mat_name) or bpy.data.materials.new(mat_name)
    me.materials.append(m)
    groups = {}
    for i, w in enumerate(weights):
        for g, v in top4(w).items():
            if g not in BONES:
                continue
            if g not in groups:
                groups[g] = ob.vertex_groups.new(name=g)
            groups[g].add([i], v, "REPLACE")
    ob.parent = arm
    ob.matrix_parent_inverse = arm.matrix_world.inverted()
    mod = ob.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    return ob


# ----------------------------------------------------------------------------- VÁY
def build_skirt():
    SEG, RINGS = 80, 26
    z_waist = 1.262
    leg_tree = make_tree(LEGS)
    remap = {"foot_l": "calf_l", "ball_l": "calf_l", "foot_r": "calf_r", "ball_r": "calf_r",
             "spine_02": "spine_01", "spine_03": "spine_01"}
    verts, uvs, weights = [], [], []
    # gấu váy lởm chởm theo góc
    hem = []
    for s in range(SEG):
        th = 2 * math.pi * s / SEG
        n = noise.noise(Vector((math.cos(th) * 1.7, math.sin(th) * 1.7, 3.1)))
        n2 = noise.noise(Vector((math.cos(th) * 6.0, math.sin(th) * 6.0, 9.4)))
        hem.append(0.028 + 0.028 * n + 0.018 * n2)  # gấu chấm đất, che kín đôi bốt
    for r in range(RINGS):
        t = r / (RINGS - 1)
        tt = t ** 0.85  # dồn vòng về phía hông (chỗ cong nhiều)
        for s in range(SEG):
            th = 2 * math.pi * s / SEG
            z = z_waist + (hem[s] - z_waist) * tt
            ex, eymin, eymax = envelope(z)
            flare = tt ** 0.92
            rx = max(ex + 0.022, 0.150 + 0.30 * flare)
            ryf = max(-eymin + 0.024, 0.118 + 0.25 * flare)
            ryb = max(eymax + 0.024, 0.140 + 0.36 * flare)
            if tt < 0.12:  # ôm sát eo
                k = tt / 0.12
                rx = (ex + 0.012) * (1 - k) + rx * k
                ryf = (-eymin + 0.012) * (1 - k) + ryf * k
                ryb = (eymax + 0.012) * (1 - k) + ryb * k
            # nếp gấp: sâu dần về phía gấu
            amp = 0.085 * max(0.0, (tt - 0.08) / 0.92) ** 1.2
            pleat = math.sin(13 * th + 1.9 * math.sin(3 * th) + 2.5 * tt) * 0.65
            pleat += noise.noise(Vector((math.cos(th) * 4.0, math.sin(th) * 4.0, tt * 2.0))) * 0.6
            sc = 1.0 + amp * pleat
            c = math.cos(th)
            x = rx * math.sin(th) * sc
            y = 0.012 + (-c) * (ryf if c > 0 else ryb) * sc
            co = Vector((x, y, z))
            verts.append(co)
            uvs.append((s / SEG * 3.0, 1.0 - t))
            w = idw_weights(leg_tree, LEGS, co, k=14, remap=remap, power=1.6)
            # Trộn hông: gần eo bám hẳn hông; xuống dưới theo chân ~72% (đủ để chân không xuyên váy khi bước)
            leg_share = 0.72 * min(1.0, tt / 0.18)
            w = {g: v * leg_share for g, v in w.items()}
            w["pelvis"] = w.get("pelvis", 0.0) + (1.0 - leg_share)
            weights.append(w)
    # vòng nối khép kín ở đường may (lặp cột 0 để UV liền)
    stride = SEG
    # làm mượt trọng số quanh vòng (xóa đường chia trái/phải ở giữa trước và giữa sau)
    for _ in range(5):
        new = []
        for r in range(RINGS):
            for s in range(SEG):
                i = r * stride + s
                a = weights[r * stride + (s - 1) % SEG]
                b = weights[r * stride + (s + 1) % SEG]
                c0 = weights[i]
                m = {}
                for src_w, f in ((c0, 0.5), (a, 0.25), (b, 0.25)):
                    for g, v in src_w.items():
                        m[g] = m.get(g, 0.0) + v * f
                new.append(m)
        weights = new
    faces = []
    for r in range(RINGS - 1):
        for s in range(SEG):
            a = r * stride + s
            b = r * stride + (s + 1) % SEG
            faces.append((a, b, b + stride, a + stride))
    # UV đường may: tách cột cuối để u không quấn ngược → nhân đôi cột 0 với u = 3.0
    base = len(verts)
    for r in range(RINGS):
        verts.append(verts[r * stride].copy())
        uvs.append((3.0, 1.0 - r / (RINGS - 1)))
        weights.append(dict(weights[r * stride]))
    faces2 = []
    for (a, b, c, d) in faces:
        if (a % stride) == SEG - 1:  # mặt cuối vòng → dùng cột nhân đôi
            ra = a // stride
            faces2.append((a, base + ra, base + ra + 1, d))
        else:
            faces2.append((a, b, c, d))
    return build_object("HK_Skirt", verts, faces2, uvs, weights, "HK_Cloth")


# ----------------------------------------------------------------------------- KHĂN CHOÀNG VAI
def build_shawl():
    """Dải vải rách phủ vai + ngực trên + lưng trên: lấy bề mặt thân gốc đẩy ra ngoài rồi kéo rũ xuống."""
    tor_tree = make_tree(TORSO + ARMS)
    src = TORSO + ARMS
    SEG, RINGS = 64, 9
    verts, uvs, weights = [], [], []
    AX = Vector((0.0, 0.045))
    surf = [(p.z, math.atan2(p.x - AX.x, -(p.y - AX.y)), math.hypot(p.x - AX.x, p.y - AX.y))
            for p, _ in src if 1.30 < p.z < 1.76 and abs(p.x) < 0.235]

    def body_radius(z, th):
        best = 0.0
        for vz, vth, vr in surf:
            if abs(vz - z) > 0.04:
                continue
            d = abs((vth - th + math.pi) % (2 * math.pi) - math.pi)
            if d < 0.24 and vr > best:
                best = vr
        return best

    radii = []
    for r in range(RINGS):
        t = r / (RINGS - 1)
        for s in range(SEG + 1):
            th = 2 * math.pi * s / SEG
            c, sn = math.cos(th), math.sin(th)
            # vòng trên ôm cổ (z≈1.70), vòng dưới rũ qua vai xuống ngực/lưng (z≈1.40)
            n = noise.noise(Vector((c * 2.2, sn * 2.2, 5.5)))
            z_low = 1.40 + 0.06 * n - 0.04 * abs(sn)  # hai bên vai rũ thấp hơn
            z = 1.705 + (z_low - 1.705) * (t ** 0.9)
            neck_r = 0.062
            # bán kính mục tiêu: vai rộng ±0.215, ngực −0.155, lưng +0.165
            k = t ** 0.75
            rx = neck_r + (0.225 - neck_r) * k
            ryf = neck_r + 0.01 + (0.165 - neck_r) * k
            ryb = neck_r + 0.02 + (0.175 - neck_r) * k
            fold = 1.0 + 0.045 * k * math.sin(9 * th + 1.3 * math.sin(2 * th))
            x = rx * sn * fold
            y = (-c) * (ryf if c > 0 else ryb) * fold
            design = math.hypot(x, y)
            rr = max(design, body_radius(z, th) + 0.016 + 0.012 * k)
            radii.append((rr, th, z, s, t))
    # làm mượt bán kính quanh vòng (bề mặt thân lồi lõm → khăn không gai góc)
    W = SEG + 1
    for _ in range(3):
        sm = list(radii)
        for r in range(RINGS):
            for s in range(W):
                i = r * W + s
                a = radii[r * W + (s - 1) % SEG][0]
                b = radii[r * W + (s + 1) % SEG][0]
                sm[i] = (max(radii[i][0], 0.5 * radii[i][0] + 0.25 * a + 0.25 * b),) + radii[i][1:]
        radii = sm
    for (rr, th, z, s, t) in radii:
        if True:
            co = Vector((AX.x + rr * math.sin(th), AX.y - rr * math.cos(th), z))
            verts.append(co)
            uvs.append((s / SEG * 2.0, 1.0 - t))
            weights.append(idw_weights(tor_tree, src, co, k=10, power=2.0))
    faces = []
    W = SEG + 1
    for r in range(RINGS - 1):
        for s in range(SEG):
            a = r * W + s
            faces.append((a, a + 1, a + 1 + W, a + W))
    return build_object("HK_Shawl", verts, faces, uvs, weights, "HK_Cloth")


# ----------------------------------------------------------------------------- TÓC
def build_hair():
    C = Vector((0.0, 0.032, 1.852))
    R = Vector((0.118, 0.128, 0.128))
    verts, uvs, weights, faces = [], [], [], []

    def w_for(z):
        if z > 1.76:
            return {"head": 1.0}
        if z > 1.64:
            k = (z - 1.64) / 0.12
            return {"head": k, "neck_01": 1 - k}
        if z > 1.50:
            k = (z - 1.50) / 0.14
            return {"neck_01": k * 0.6, "spine_03": 1 - k * 0.6}
        if z > 1.32:
            k = (z - 1.32) / 0.18
            return {"spine_03": k, "spine_02": 1 - k}
        k = max(0.0, (z - 1.15) / 0.17)
        return {"spine_02": k, "spine_01": 1 - k}

    def strand(phi, length, width, layer, side_bias):
        NSEG = 14
        c, sn = math.cos(phi), math.sin(phi)
        infl = 1.0 + 0.045 * layer
        pts = []
        # phần ôm sọ: từ đỉnh xuống "xích đạo"
        for i in range(5):
            al = (0.12 + 0.88 * i / 4) * math.radians(88)
            pts.append(Vector((C.x + R.x * infl * math.sin(al) * sn,
                               C.y - R.y * infl * math.sin(al) * c,
                               C.z + R.z * infl * math.cos(al))))
        eq = pts[-1]
        front = c > 0.35
        back = c < -0.35
        wave_ph = random.uniform(0, 6.28)
        wave_amp = random.uniform(0.004, 0.012)
        n_hang = NSEG - 4
        for i in range(1, n_hang + 1):
            d = length * i / n_hang
            k = min(1.0, d / 0.22)
            x = eq.x * (1.0 + 0.45 * k)
            x = max(-0.185, min(0.185, x))
            if front:
                y = eq.y + (-(0.150 + 0.012 * layer) - eq.y) * min(1.0, d / 0.30)
            elif back:
                y = eq.y + ((0.170 + 0.012 * layer) - eq.y) * min(1.0, d / 0.25)
            else:
                tgt = -0.125 if side_bias < 0 else 0.150
                y = eq.y + (tgt - eq.y) * min(1.0, d / 0.20)
            z = eq.z - d
            # lượn nhẹ
            x += math.sin(d * 9.0 + wave_ph) * wave_amp
            y += math.cos(d * 7.0 + wave_ph) * wave_amp * 0.6
            pts.append(Vector((x, y, z)))
        # dải phẳng quanh đường tâm; mỗi dải lấy một nửa texture (2 biến thể lọn tóc)
        u0 = random.choice((0.0, 0.5))
        base = len(verts)
        n = len(pts)
        for i, p in enumerate(pts):
            tng = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
            radial = Vector((p.x - C.x, p.y - C.y, 0.0))
            if radial.length < 1e-4:
                radial = Vector((sn, -c, 0.0))
            radial.normalize()
            side = tng.cross(radial)
            if side.length < 1e-4:
                side = Vector((c, sn, 0.0))
            side.normalize()
            v = i / (n - 1)
            wdt = width * (1.0 - 0.45 * v ** 1.6) * (0.55 + 0.45 * min(1.0, i / 2.0))
            # hơi cong lòng máng để bắt sáng như lọn tóc
            verts.append(p - side * wdt * 0.5)
            verts.append(p + side * wdt * 0.5)
            for u in (u0, u0 + 0.5):
                uvs.append((u, 1.0 - v))
            for _ in range(2):
                weights.append(w_for(p.z))
        for i in range(n - 1):
            a = base + i * 2
            faces.append((a, a + 1, a + 3, a + 2))

    # lớp ngoài + lớp trong, phủ kín quanh đầu; phía trước chừa khe hở một bên mắt
    for layer in (0, 1, 2):
        count = (64, 52, 40)[layer]
        for i in range(count):
            phi = -math.pi + 2 * math.pi * (i + random.uniform(-0.3, 0.3)) / count
            c = math.cos(phi)
            deg = math.degrees(phi)
            width = random.uniform(0.030, 0.048)
            if c > 0.35:  # trước mặt
                # Rẽ ngôi: để lộ khuôn mặt, tóc rũ hai bên má; chỉ vài lọn mảnh vắt ngang mặt
                if abs(deg) < 30:
                    if layer != 0 or random.random() > 0.42:
                        continue
                    width = random.uniform(0.010, 0.018)
                length = random.uniform(0.30, 0.52)
            elif c < -0.35:
                length = random.uniform(0.62, 0.86)
            else:
                length = random.uniform(0.50, 0.72)
            if c <= 0.35:
                width = random.uniform(0.030, 0.048)
            strand(phi, length, width, layer, random.choice((-1, 1)))
    return build_object("HK_Hair", verts, faces, uvs, weights, "HK_Hair")


skirt = build_skirt()
shawl = build_shawl()
hair = build_hair()

for ob in (skirt, shawl, hair):
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")

# ----------------------------------------------------------------------------- xuất
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
for ob in (skirt, shawl, hair):
    ob.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, object_types={"ARMATURE", "MESH"},
                         add_leaf_bones=False, bake_anim=False, mesh_smooth_type="FACE",
                         use_armature_deform_only=False, axis_forward="-Z", axis_up="Y")
print("GARMENTS_OK", OUT, "skirt", len(skirt.data.vertices), "shawl", len(shawl.data.vertices), "hair", len(hair.data.vertices))
