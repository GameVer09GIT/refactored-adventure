# Thân 3D của Alex (người chơi) từ model AI (TRELLIS, ảnh → 3D) gắn lên khung xương gốc của civilian_girl.
#   blender.exe --background --factory-startup --python Tools/Blender/alex_body.py
#
# Không sửa file asset pack: chỉ ĐỌC civilian_girl.FBX lấy khung xương + hình thể. Các bước:
#   1. Đặt khung xương gốc (tư thế chữ A) sang chữ T cho khớp model AI.
#   2. Bỏ tấm nền thừa của model, co giãn cho cùng chiều cao, kéo tay khớp sải tay + độ cao vai của khung xương.
#   3. Lấy trọng số xương theo bề mặt thân gốc gần nhất (IDW); tư thế chữ T thành tư thế bind của khung xương xuất ra
#      — Unity gắn vào đúng xương người chơi (theo tên) nên IK cầm đồ/animation dùng chung.
#   4. Tách đầu (PL_Alex_Head: chỉ đổ bóng, không che camera góc nhìn thứ nhất) và thân (PL_Alex_Body).
# Xuất: Tools/Blender/out/characters/BW_Alex_Body.fbx + texture màu Tools/Blender/out/characters/T_PL_Alex_Body_A.png
import bpy, bmesh, math, os
from mathutils import Vector, Matrix, kdtree

ROOT = r"D:/refactored-adventure"
GIRL = os.path.join(ROOT, "Assets", "ThirdParty", "civilian_girl", "Models", "civilian_girl.FBX")
SRC = os.path.join(ROOT, "Tools", "Tripo", "alex_trellis2.glb")  # TRELLIS.2 (bản cũ: alex_trellis.glb)
TARGET_TRIS = 70000  # tam giác sau khi giảm (model TRELLIS.2 ~400k)
OUT_DIR = os.path.join(ROOT, "Tools", "Blender", "out", "characters")
OUT = os.path.join(OUT_DIR, "BW_Alex_Body.fbx")
TEX = os.path.join(OUT_DIR, "T_PL_Alex_Body_A.png")
os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=GIRL, automatic_bone_orientation=False, use_anim=False)
arm = bpy.data.objects["Armature"]
BONES = {b.name for b in arm.data.bones}
BODY = ["shirt_t", "pant", "hand_left", "hand_right", "chest", "head_01", "boot_left", "boot_right", "hair"]

# ----------------------------------------------------------------------------- 1. khung xương sang chữ T
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")


def aim(bone, target_bone, direction):
    """Xoay `bone` (quanh đầu xương) để vector đầu bone → đầu target_bone trùng `direction` (hệ armature)."""
    pb = arm.pose.bones[bone]; pt = arm.pose.bones[target_bone]
    bpy.context.view_layer.update()
    cur = (pt.head - pb.head).normalized()
    q = cur.rotation_difference(direction.normalized())
    h = pb.head.copy()
    pb.matrix = Matrix.Translation(h) @ q.to_matrix().to_4x4() @ Matrix.Translation(-h) @ pb.matrix
    bpy.context.view_layer.update()


for s, sx in (("l", 1), ("r", -1)):
    out = Vector((sx, 0.0, 0.0))
    aim("upperarm_" + s, "lowerarm_" + s, out)
    aim("lowerarm_" + s, "hand_" + s, out)
    aim("hand_" + s, "middle_01_" + s, out)
bpy.ops.object.mode_set(mode="OBJECT")
bpy.context.view_layer.update()

# Đỉnh thân gốc ở tư thế chữ T (world) + trọng số
dg = bpy.context.evaluated_depsgraph_get()
SRCV = []
HANDL = []  # da cánh tay trái gốc (tay áo 3D của game dựng từ lưới này)
for n in BODY:
    o = bpy.data.objects[n]
    gname = {g.index: g.name for g in o.vertex_groups}
    ev = o.evaluated_get(dg).to_mesh()
    mw = o.matrix_world
    for v, ve in zip(o.data.vertices, ev.vertices):
        w = {gname[g.group]: g.weight for g in v.groups if g.weight > 0.001 and gname[g.group] in BONES}
        if w:
            SRCV.append((mw @ ve.co, w))
            if n == "hand_left": HANDL.append(mw @ ve.co)
    o.evaluated_get(dg).to_mesh_clear()
zs = [p.z for p, _ in SRCV]; g_min, g_max = min(zs), max(zs)
xs = [p.x for p, _ in SRCV]
g_span = max(abs(min(xs)), abs(max(xs)))
pa = lambda n: arm.matrix_world @ arm.pose.bones[n].head
g_shoulder_z = (pa("upperarm_l").z + pa("upperarm_r").z) / 2
g_shoulder_x = abs(pa("upperarm_l").x)
g_cy = sum(p.y for p, _ in SRCV if p.z < g_min + 0.4) / max(1, sum(1 for p, _ in SRCV if p.z < g_min + 0.4))
print("GIRL height %.3f span %.3f shoulder z %.3f x %.3f" % (g_max - g_min, g_span, g_shoulder_z, g_shoulder_x))

# ----------------------------------------------------------------------------- 2. model AI
before = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=SRC)
new = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
bpy.ops.object.select_all(action="DESELECT")
for o in new: o.select_set(True)
bpy.context.view_layer.objects.active = new[0]
if len(new) > 1: bpy.ops.object.join()
alex = bpy.context.active_object
alex.name = "PL_Alex_Body"
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Ảnh màu gốc (model PBR có cả ảnh nhám/kim loại — chỉ lấy ảnh nối vào Base Color)
BASE_IMG = None
for m in alex.data.materials:
    if m and m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type == "BSDF_PRINCIPLED":
                for l in n.inputs["Base Color"].links:
                    nd = l.from_node
                    while nd.type != "TEX_IMAGE" and nd.inputs and nd.inputs[0].links:
                        nd = nd.inputs[0].links[0].from_node
                    if nd.type == "TEX_IMAGE": BASE_IMG = nd.image
_tris = sum(len(p.vertices) - 2 for p in alex.data.polygons)
if _tris > TARGET_TRIS * 1.2:
    dm = alex.modifiers.new("Dec", "DECIMATE"); dm.ratio = TARGET_TRIS / _tris
    bpy.ops.object.modifier_apply(modifier="Dec")
print("TRIS", _tris, "->", sum(len(p.vertices) - 2 for p in alex.data.polygons), "base image", BASE_IMG.name if BASE_IMG else None)

# Hàn đỉnh trùng (model AI là nhiều mảnh bề mặt rời) rồi mới bỏ mảnh rời phẳng (tấm nền dưới chân) và mảnh vụn
bm = bmesh.new(); bm.from_mesh(alex.data)
_h = max(v.co.z for v in bm.verts) - min(v.co.z for v in bm.verts)
_n0 = len(bm.verts)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0015 * _h)
print("WELD", _n0, "->", len(bm.verts))
# Tấm nền (dày ~1% chiều cao, rộng gần bằng sải tay): xóa mọi đỉnh dưới đế giày + phần nền ngoài bàn chân
_zmin = min(v.co.z for v in bm.verts)
_cx = sum(v.co.x for v in bm.verts) / len(bm.verts)
_bot = [v for v in bm.verts if v.co.z < _zmin + 0.005 * _h]
_has_plate = bool(_bot) and (max(v.co.x for v in _bot) - min(v.co.x for v in _bot)) > 0.6 * _h
_plate = [v for v in bm.verts if v.co.z < _zmin + 0.0099 * _h or
          (v.co.z < _zmin + 0.013 * _h and abs(v.co.x - _cx) > 0.17 * _h)] if _has_plate else []
bmesh.ops.delete(bm, geom=_plate, context="VERTS")
print("PLATE faces removed", len(_plate))
bm.verts.ensure_lookup_table()
seen = set(); parts = []
for v in bm.verts:
    if v.index in seen: continue
    stack = [v]; comp = []
    seen.add(v.index)
    while stack:
        x = stack.pop(); comp.append(x)
        for e in x.link_edges:
            o2 = e.other_vert(x)
            if o2.index not in seen: seen.add(o2.index); stack.append(o2)
    parts.append(comp)
H0 = max(v.co.z for v in bm.verts) - min(v.co.z for v in bm.verts)
kill = []
for comp in parts:
    zz = [v.co.z for v in comp]; xx = [v.co.x for v in comp]; yy = [v.co.y for v in comp]
    flat = (max(zz) - min(zz)) < 0.02 * H0 and (max(xx) - min(xx)) > 0.25 * H0
    tiny = len(comp) < 12
    if flat or tiny: kill.extend(comp)
print("PARTS", len(parts), "removed verts", len(kill))
bmesh.ops.delete(bm, geom=list(set(kill)), context="VERTS")
bm.to_mesh(alex.data); bm.free()

# Co giãn cùng chiều cao, đặt chân trùng chân khung xương
vs = alex.data.vertices
a_min = min(v.co.z for v in vs); a_max = max(v.co.z for v in vs)
k = (g_max - g_min) / (a_max - a_min)
cx = sum(v.co.x for v in vs) / len(vs)
feet = [v.co for v in vs if v.co.z < a_min + 0.05 * (a_max - a_min)]
cy = sum(c.y for c in feet) / len(feet)
for v in vs:
    v.co = Vector(((v.co.x - cx) * k, (v.co.y - cy) * k + g_cy, (v.co.z - a_min) * k + g_min))
# Nắn tay: đường tâm tay model AI (theo từng lát dọc trục X) đặt trùng đường xương tay khung gốc ở tư thế chữ T,
# chiều dài nách → đầu ngón co giãn tuyến tính cho khớp; vùng vai chuyển mượt về thân.
def tip_of(s):
    b = arm.pose.bones.get("middle_03_" + s)
    return arm.matrix_world @ b.tail if b else None
GL = {}
for s, sgn in (("l", 1), ("r", -1)):
    pts = [pa("upperarm_" + s), pa("lowerarm_" + s), pa("hand_" + s)]
    t = tip_of(s)
    pts.append(t if t else pts[-1] + (pts[-1] - pts[-2]).normalized() * 0.18)
    GL[sgn] = sorted(pts, key=lambda q: abs(q.x))
g_arm_z = sum(q.z for q in GL[1] + GL[-1]) / 8
g_ap = max(abs(p.x) for p, _ in SRCV if g_arm_z - 0.30 < p.z < g_arm_z - 0.18)


def gline(sgn, gx):
    pts = GL[sgn]
    if gx <= abs(pts[0].x): return pts[0].y, pts[0].z
    for q0, q1 in zip(pts, pts[1:]):
        if gx <= abs(q1.x):
            f = (gx - abs(q0.x)) / max(1e-5, abs(q1.x) - abs(q0.x))
            return q0.y + (q1.y - q0.y) * f, q0.z + (q1.z - q0.z) * f
    return pts[-1].y, pts[-1].z


a_span = max(abs(v.co.x) for v in vs)
far = [v.co.z for v in vs if abs(v.co.x) > 0.6 * a_span]
a_arm_z = sorted(far)[len(far) // 2]
a_ap = max(abs(v.co.x) for v in vs if a_arm_z - 0.30 < v.co.z < a_arm_z - 0.18)
BIN = 0.025
cl = {}
for v in vs:
    if abs(v.co.x) > a_ap - 0.10 and abs(v.co.z - a_arm_z) < 0.25:
        cl.setdefault((1 if v.co.x > 0 else -1, int(abs(v.co.x) / BIN)), []).append(v.co)
CL = {}
for key, cs in cl.items():
    ys = sorted(c.y for c in cs); zz = sorted(c.z for c in cs)
    CL[key] = (ys[len(ys) // 2], zz[len(zz) // 2])


def acl(sgn, ax):
    i = int(ax / BIN)
    acc = [CL[(sgn, j)] for j in (i - 1, i, i + 1) if (sgn, j) in CL]
    if not acc: return None
    return sum(a[0] for a in acc) / len(acc), sum(a[1] for a in acc) / len(acc)


print("GIRL arm z %.3f armpit %.3f tip %.3f | ALEX arm z %.3f armpit %.3f tip %.3f" %
      (g_arm_z, g_ap, g_span, a_arm_z, a_ap, a_span))
for v in vs:
    ax = abs(v.co.x); sgn = 1 if v.co.x > 0 else -1
    t = (ax - (a_ap - 0.07)) / 0.10
    if t <= 0: continue
    t = min(1.0, t); t = t * t * (3 - 2 * t)
    # chỉ vùng ngang vai trở lên (không đụng hông, ba lô, giày ở cùng khoảng X)
    tz = min(1.0, max(0.0, (v.co.z - (a_arm_z - 0.40)) / 0.15)); t *= tz * tz * (3 - 2 * tz)
    if t <= 0: continue
    c = acl(sgn, ax)
    if c is None: continue
    # Giữ bề rộng vai của Alex (thân nam rộng hơn khung gốc), chỉ co chiều dài nách → đầu ngón cho khớp xương
    gx = ax if ax < a_ap else a_ap + (ax - a_ap) / (a_span - a_ap) * (g_span - a_ap)
    gy, gz = gline(sgn, gx)
    nv = Vector((sgn * gx, v.co.y - c[0] + gy, v.co.z - c[1] + gz))
    v.co = v.co.lerp(nv, t)

# Cánh tay: thân Alex rộng vai hơn khung gốc (khớp vai gốc nằm sâu trong người) nên ống tay áo nỉ bám xương kiểu gì cũng
# nhăn khi hạ/giơ tay. Cắt ngay tại nách, bịt kín thành mặt vai tròn — cả cánh tay là tay áo/găng 3D của game (khớp IK sẵn).
CUT_X = a_ap + 0.012
# Hạ dốc vai (ở chữ T vai nằm ngang nối vào tay; bỏ tay đi thì thành gờ ngang như cánh): xoay vùng vai quanh gốc cổ
SL_X0, SL_ANG = 0.09, math.radians(28)
for v in vs:
    ax = abs(v.co.x)
    if ax <= SL_X0: continue
    tz = (v.co.z - (g_arm_z - 0.13)) / 0.10
    if tz <= 0: continue
    tz = min(1.0, tz); tz = tz * tz * (3 - 2 * tz)
    tx = min(1.0, (ax - SL_X0) / (CUT_X - SL_X0)); tx = tx * tx * (3 - 2 * tx)
    a = SL_ANG * tx * tz
    sgn = 1 if v.co.x > 0 else -1
    dx, dz = ax - SL_X0, v.co.z - (g_arm_z + 0.06)
    nx = dx * math.cos(a) + dz * math.sin(a); nz = -dx * math.sin(a) + dz * math.cos(a)
    v.co.x = sgn * (SL_X0 + nx); v.co.z = g_arm_z + 0.06 + nz
alex.data.update()
bm = bmesh.new(); bm.from_mesh(alex.data)
for sgn in (1, -1):
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-5, plane_co=Vector((sgn * CUT_X, 0, 0)),
                           plane_no=Vector((sgn, 0, 0)), clear_outer=True)
rim = [e for e in bm.edges if e.is_boundary and all(abs(abs(v.co.x) - CUT_X) < 2e-4 for v in e.verts)]
caps = bmesh.ops.holes_fill(bm, edges=rim, sides=0)["faces"]
bmesh.ops.recalc_face_normals(bm, faces=caps)
for f in caps:
    if (f.normal.x > 0) != (f.calc_center_median().x > 0): f.normal_flip()
bmesh.ops.triangulate(bm, faces=caps)
caps = [f for f in bm.faces if all(abs(abs(v.co.x) - CUT_X) < 2e-4 for v in f.verts)]
uvl = bm.loops.layers.uv.active
if uvl:  # mặt bịt lấy màu vải tại một điểm trên mép (UV gộp một điểm, không kéo texture)
    for f in caps:
        uv0 = f.loops[0][uvl].uv.copy()
        for lp in f.loops: lp[uvl].uv = uv0
bm.to_mesh(alex.data); bm.free()
vs = alex.data.vertices
print("ARMHOLE cut at x=%.3f, rim edges %d, cap faces %d, verts %d" % (CUT_X, len(rim), len(caps), len(vs)))
alex.data.update()

# ----------------------------------------------------------------------------- 3. trọng số + tháo tư thế
tree = kdtree.KDTree(len(SRCV))
for i, (p, _) in enumerate(SRCV): tree.insert(p, i)
tree.balance()


def idw(co, k=10, power=2.0):
    acc = {}
    for _, idx, d in tree.find_n(co, k):
        f = 1.0 / (d ** power + 1e-6)
        for g, w in SRCV[idx][1].items():
            acc[g] = acc.get(g, 0.0) + w * f
    items = sorted(acc.items(), key=lambda kv: -kv[1])[:4]
    s = sum(w for _, w in items) or 1.0
    return {g: w / s for g, w in items if w / s > 0.01}


W = [idw(v.co, k=16) for v in vs]
# Vai: thân Alex rộng hơn khung gốc nên điểm gần nhất trên thân gốc cho trọng số nhiễu (nếp gãy khi hạ tay).
# Đặt lại: từ nách ra mỏm cụt tay áo, trọng số chuyển mượt thân (xương đòn/cột sống) → xương cánh tay trên.
ARM_PREFIX = ("upperarm", "lowerarm", "hand", "thumb", "index", "middle", "ring", "pinky")
SH0, SH1 = g_ap + 0.02, CUT_X
SH_MAX = 0.35  # mặt vai chỉ theo cánh tay một phần (giơ đèn pin không kéo méo cả vai)
for i, v in enumerate(vs):
    ax = abs(v.co.x)
    if ax <= SH0 or not (g_arm_z - 0.10 < v.co.z < g_arm_z + 0.14): continue
    side = "l" if v.co.x > 0 else "r"
    w = min(1.0, (ax - SH0) / (SH1 - SH0)); w = w * w * (3 - 2 * w) * SH_MAX
    base = {g: f for g, f in W[i].items() if not g.startswith(ARM_PREFIX)} or {"clavicle_" + side: 1.0}
    t = sum(base.values())
    nw = {g: f / t * (1 - w) for g, f in base.items()}
    nw["upperarm_" + side] = nw.get("upperarm_" + side, 0.0) + w
    W[i] = {g: f for g, f in nw.items() if f > 0.01}
# Làm mượt trọng số theo cạnh lưới (nối tay–thân, hông–đùi biến dạng êm, không gãy gắt)
nbr = [[] for _ in vs]
for e in alex.data.edges:
    a0, a1 = e.vertices; nbr[a0].append(a1); nbr[a1].append(a0)
for _ in range(4):
    NW = []
    for i, w in enumerate(W):
        acc = dict((g, f * 0.5) for g, f in w.items())
        if nbr[i]:
            share = 0.5 / len(nbr[i])
            for j in nbr[i]:
                for g, f in W[j].items(): acc[g] = acc.get(g, 0.0) + f * share
        else:
            acc = dict(w)
        items = sorted(acc.items(), key=lambda kv: -kv[1])[:4]
        t = sum(f for _, f in items) or 1.0
        NW.append({g: f / t for g, f in items if f / t > 0.01})
    W = NW
# Giữ lưới ở tư thế chữ T và lấy chính tư thế T làm tư thế nghỉ (bind) của khung xương xuất ra: Unity tính bindpose
# theo xương trong FBX này, khi chạy xương của Player (animation/IK) kéo lưới theo skin xuôi chuẩn — không "tháo tư thế"
# bằng nghịch đảo skin (gây nếp gãy ở vai khi góc xoay lớn).
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.pose.armature_apply(selected=False)
bpy.ops.object.mode_set(mode="OBJECT")
bpy.context.view_layer.update()

groups = {}
for i, w in enumerate(W):
    for g, f in w.items():
        if g not in groups: groups[g] = alex.vertex_groups.new(name=g)
        groups[g].add([i], f, "REPLACE")
alex.parent = arm
alex.matrix_parent_inverse = arm.matrix_world.inverted()
mod = alex.modifiers.new("Armature", "ARMATURE"); mod.object = arm

# Cánh tay đã cắt ở nách (CUT_X) — góc nhìn thứ nhất vẫn dùng tay gốc + tay áo/găng 3D
for p in alex.data.polygons: p.use_smooth = True

# ----------------------------------------------------------------------------- 4. tách đầu
HEADB = {"head", "neck_01"}
bpy.ops.object.select_all(action="DESELECT")
alex.select_set(True); bpy.context.view_layer.objects.active = alex
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="DESELECT"); bpy.ops.object.mode_set(mode="OBJECT")
# Cắt theo mặt phẳng ngang giữa cổ và đầu: mép mũ áo/cổ áo gọn một vòng (không răng cưa như tách theo trọng số)
# Đầu Alex (model AI) thấp hơn xương đầu khung gốc → lấy mép dưới vùng da cổ/mặt trên texture làm mặt cắt,
# để cả mặt + cổ nằm trong lưới đầu (chỉ đổ bóng, không lọt vào camera góc nhìn thứ nhất).
_img = BASE_IMG
_px = _img.pixels[:] if _img else None
_W, _H = (_img.size if _img else (0, 0))
_uv = alex.data.uv_layers.active.data if alex.data.uv_layers.active else None
_skz = []
if _px and _uv:
    for poly in alex.data.polygons:
        for li in poly.loop_indices:
            vi = alex.data.loops[li].vertex_index
            co = alex.data.vertices[vi].co
            if abs(co.x) > 0.12 or co.z < g_arm_z - 0.15: continue
            u, w = _uv[li].uv
            i = (min(_H - 1, max(0, int(w * _H))) * _W + min(_W - 1, max(0, int(u * _W)))) * 4
            r, g, b_ = _px[i], _px[i + 1], _px[i + 2]
            if r > 0.25 and r > g > b_ and r - b_ > 0.10:
                _skz.append(co.z)
_skz.sort()
Z_SKIN = _skz[int(len(_skz) * 0.03)] if len(_skz) > 50 else pa("neck_01").z
Z_HEAD = Z_SKIN - 0.005
print("SKIN samples %d, lowest skin z %.3f (neck bone %.3f, head bone %.3f)" % (len(_skz), Z_SKIN, pa("neck_01").z, pa("head").z))
bm = bmesh.new(); bm.from_mesh(alex.data)
bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-5,
                       plane_co=Vector((0, 0, Z_HEAD)), plane_no=Vector((0, 0, 1)))
bm.to_mesh(alex.data); bm.free()
for v in alex.data.vertices:
    v.select = v.co.z >= Z_HEAD - 1e-5
print("HEAD split at z=%.3f" % Z_HEAD)
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.separate(type="SELECTED"); bpy.ops.object.mode_set(mode="OBJECT")
head = [o for o in bpy.context.selected_objects if o != alex]
if head: head[0].name = "PL_Alex_Head"

# Texture màu gốc của model AI
img = BASE_IMG
if img:
    # Tràn màu ra nền đen giữa các mảnh UV (khi thu nhỏ mipmap, nền đen loang thành đường nứt ở mép mảnh)
    import numpy as np
    w_, h_ = img.size
    px = np.empty(w_ * h_ * 4, dtype=np.float32); img.pixels.foreach_get(px)
    px = px.reshape(h_, w_, 4)
    rgb = px[..., :3].copy(); filled = rgb.max(axis=2) > 6 / 255
    for _ in range(24):
        if filled.all(): break
        acc = np.zeros_like(rgb); cnt = np.zeros(filled.shape, np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
            f = np.roll(np.roll(filled, dy, 0), dx, 1)
            acc += np.roll(np.roll(rgb, dy, 0), dx, 1) * f[..., None]; cnt += f
        grow = (~filled) & (cnt > 0)
        rgb[grow] = acc[grow] / cnt[grow][:, None]; filled = filled | grow
    px[..., :3] = rgb; px[..., 3] = 1.0
    img.pixels.foreach_set(px.ravel())
    print("TEX dilate: còn trống %.4f" % (1 - filled.mean()))
    img.filepath_raw = TEX; img.file_format = "PNG"; img.save()
print("TEXTURE", TEX if img else "NONE")

# Xuất
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
for o in bpy.data.objects:
    if o.name.startswith("PL_Alex"): o.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, object_types={"ARMATURE", "MESH"},
                         add_leaf_bones=False, bake_anim=False, mesh_smooth_type="FACE",
                         use_armature_deform_only=False, axis_forward="-Z", axis_up="Y")
for o in bpy.data.objects:
    if o.name.startswith("PL_Alex"): print("OBJ", o.name, len(o.data.vertices), "verts", len(o.vertex_groups), "groups")
print("ALEX_OK", OUT)
