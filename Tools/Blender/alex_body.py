# Thân 3D của Alex (người chơi) từ model AI (TRELLIS, ảnh → 3D) gắn lên khung xương gốc của civilian_girl.
#   blender.exe --background --factory-startup --python Tools/Blender/alex_body.py
#
# Không sửa file asset pack: chỉ ĐỌC civilian_girl.FBX lấy khung xương + hình thể. Các bước:
#   1. Đặt khung xương gốc (tư thế chữ A) sang chữ T cho khớp model AI.
#   2. Bỏ tấm nền thừa của model, co giãn cho cùng chiều cao, kéo tay khớp sải tay + độ cao vai của khung xương.
#   3. Lấy trọng số xương theo bề mặt thân gốc gần nhất (IDW), rồi "tháo tư thế" từng đỉnh về tư thế bind (chữ A)
#      bằng nghịch đảo phép skin — Unity gắn lại vào đúng xương người chơi nên IK cầm đồ/animation dùng chung.
#   4. Tách đầu (PL_Alex_Head: chỉ đổ bóng, không che camera góc nhìn thứ nhất) và thân (PL_Alex_Body).
# Xuất: Tools/Blender/out/characters/BW_Alex_Body.fbx + texture màu Tools/Blender/out/characters/T_PL_Alex_Body_A.png
import bpy, bmesh, math, os
from mathutils import Vector, Matrix, kdtree

ROOT = r"D:/refactored-adventure"
GIRL = os.path.join(ROOT, "Assets", "ThirdParty", "civilian_girl", "Models", "civilian_girl.FBX")
SRC = os.path.join(ROOT, "Tools", "Tripo", "alex_trellis.glb")
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
for n in BODY:
    o = bpy.data.objects[n]
    gname = {g.index: g.name for g in o.vertex_groups}
    ev = o.evaluated_get(dg).to_mesh()
    mw = o.matrix_world
    for v, ve in zip(o.data.vertices, ev.vertices):
        w = {gname[g.group]: g.weight for g in v.groups if g.weight > 0.001 and gname[g.group] in BONES}
        if w:
            SRCV.append((mw @ ve.co, w))
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

# Hàn đỉnh trùng (model AI là nhiều mảnh bề mặt rời) rồi mới bỏ mảnh rời phẳng (tấm nền dưới chân) và mảnh vụn
bm = bmesh.new(); bm.from_mesh(alex.data)
_h = max(v.co.z for v in bm.verts) - min(v.co.z for v in bm.verts)
_n0 = len(bm.verts)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0015 * _h)
print("WELD", _n0, "->", len(bm.verts))
# Tấm nền (dày ~1% chiều cao, rộng gần bằng sải tay): xóa mọi đỉnh dưới đế giày + phần nền ngoài bàn chân
_zmin = min(v.co.z for v in bm.verts)
_cx = sum(v.co.x for v in bm.verts) / len(bm.verts)
_plate = [v for v in bm.verts if v.co.z < _zmin + 0.0099 * _h or
          (v.co.z < _zmin + 0.013 * _h and abs(v.co.x - _cx) > 0.17 * _h)]
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
# Ma trận biến dạng từng xương (world): posed @ rest^-1
D = {}
for pb in arm.pose.bones:
    D[pb.name] = arm.matrix_world @ pb.matrix @ pb.bone.matrix_local.inverted() @ arm.matrix_world.inverted()
for v, w in zip(vs, W):
    M = Matrix(((0, 0, 0, 0), (0, 0, 0, 0), (0, 0, 0, 0), (0, 0, 0, 0)))
    for g, f in w.items():
        M = M + D[g] * f
    v.co = M.inverted() @ v.co
alex.data.update()

# Về lại tư thế bind
for pb in arm.pose.bones:
    pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()

groups = {}
for i, w in enumerate(W):
    for g, f in w.items():
        if g not in groups: groups[g] = alex.vertex_groups.new(name=g)
        groups[g].add([i], f, "REPLACE")
alex.parent = arm
alex.matrix_parent_inverse = arm.matrix_world.inverted()
mod = alex.modifiers.new("Armature", "ARMATURE"); mod.object = arm

# Cẳng tay + bàn tay: bỏ — góc nhìn thứ nhất vẫn dùng tay gốc + tay áo/găng 3D (BlackwoodPlayerGarments) đã khớp IK
FORE = ("lowerarm", "hand_", "thumb", "index", "middle", "ring", "pinky")
for i, w in enumerate(W):
    vs[i].select = sum(f for g, f in w.items() if g.startswith(FORE)) > 0.5
bm = bmesh.new(); bm.from_mesh(alex.data)
bm.verts.ensure_lookup_table()
_cut = [bm.verts[i] for i in range(len(W)) if vs[i].select]
bmesh.ops.delete(bm, geom=_cut, context="VERTS")
bm.to_mesh(alex.data); bm.free()
print("FOREARM verts removed", len(_cut))
for p in alex.data.polygons: p.use_smooth = True

# ----------------------------------------------------------------------------- 4. tách đầu
HEADB = {"head", "neck_01"}
bpy.ops.object.select_all(action="DESELECT")
alex.select_set(True); bpy.context.view_layer.objects.active = alex
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="DESELECT"); bpy.ops.object.mode_set(mode="OBJECT")
_hidx = {g.index for g in alex.vertex_groups if g.name in HEADB}
for v in alex.data.vertices:
    v.select = sum(g.weight for g in v.groups if g.group in _hidx) > 0.5
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.separate(type="SELECTED"); bpy.ops.object.mode_set(mode="OBJECT")
head = [o for o in bpy.context.selected_objects if o != alex]
if head: head[0].name = "PL_Alex_Head"

# Texture màu gốc của model AI
img = None
for m in alex.data.materials:
    if m and m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image:
                img = n.image; break
    if img: break
if img:
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
