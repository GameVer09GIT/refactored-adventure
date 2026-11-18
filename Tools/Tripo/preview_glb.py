# Render xem trước một file GLB (mặt trước, nghiêng, sau) — blender --background --python preview_glb.py -- <glb> <out_prefix>
import bpy, sys, math
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, outp = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
mn = Vector((1e9, 1e9, 1e9)); mx = -mn
tris = 0
for o in meshes:
    tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c)
        mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
ctr = (mn + mx) / 2; size = (mx - mn)
print("MESHES", len(meshes), "TRIS", tris, "SIZE", tuple(round(v, 3) for v in size), "MIN", tuple(round(v, 3) for v in mn))
sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
sc.render.resolution_x, sc.render.resolution_y = 700, 900
sc.world = bpy.data.worlds.new("W"); sc.world.use_nodes = True
sc.world.node_tree.nodes["Background"].inputs[0].default_value = (0.25, 0.26, 0.28, 1)
sc.world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
for rot, e in (((50, 0, 30), 3.0), ((60, 0, 200), 1.5)):
    ld = bpy.data.lights.new("L", "SUN"); ld.energy = e
    lo = bpy.data.objects.new("L", ld); lo.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(lo)
cd = bpy.data.cameras.new("C"); cd.lens = 50
cam = bpy.data.objects.new("C", cd); sc.collection.objects.link(cam); sc.camera = cam
h = max(size.x, size.y, size.z)
for name, ang in (("front", 0), ("side", 50), ("back", 180)):
    a = math.radians(ang)
    # glTF: Y-up vào Blender thành Z-up, mặt nhân vật thường nhìn -Y
    cam.location = ctr + Vector((math.sin(a) * h * 2.2, -math.cos(a) * h * 2.2, 0))
    d = ctr - cam.location
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = f"{outp}_{name}.png"
    bpy.ops.render.render(write_still=True)
print("DONE")
