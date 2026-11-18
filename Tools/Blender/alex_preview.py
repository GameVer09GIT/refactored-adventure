# Xem trước BW_Alex_Body.fbx: tư thế bind + tư thế thử (co tay, bước chân) — kiểm tra skin.
import bpy, math, os
from mathutils import Vector, Euler

ROOT = r"D:/refactored-adventure"
FBX = os.path.join(ROOT, "Tools/Blender/out/characters/BW_Alex_Body.fbx")
TEX = os.path.join(ROOT, "Tools/Blender/out/characters/T_PL_Alex_Body_A.png")
OUT = os.path.join(ROOT, "Temp/Blackwood")
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, automatic_bone_orientation=False)
arm = [o for o in bpy.data.objects if o.type == "ARMATURE"][0]
img = bpy.data.images.load(TEX)
mat = bpy.data.materials.new("M"); mat.use_nodes = True
bsdf = mat.node_tree.nodes["Principled BSDF"]
tx = mat.node_tree.nodes.new("ShaderNodeTexImage"); tx.image = img
mat.node_tree.links.new(tx.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.8
for o in bpy.data.objects:
    if o.type == "MESH":
        o.data.materials.clear(); o.data.materials.append(mat)
sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
sc.render.resolution_x, sc.render.resolution_y = 640, 900
sc.world = bpy.data.worlds.new("W"); sc.world.use_nodes = True
sc.world.node_tree.nodes["Background"].inputs[0].default_value = (0.5, 0.52, 0.55, 1)
for rot, e in (((50, 0, 20), 4.0), ((60, 0, 200), 2.0)):
    ld = bpy.data.lights.new("L", "SUN"); ld.energy = e
    lo = bpy.data.objects.new("L", ld); lo.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(lo)
cd = bpy.data.cameras.new("C"); cd.lens = 45
cam = bpy.data.objects.new("C", cd); sc.collection.objects.link(cam); sc.camera = cam


def shot(name, ang, ctr=Vector((0, 0, 1.0)), dist=4.2):
    a = math.radians(ang)
    cam.location = ctr + Vector((math.sin(a) * dist, -math.cos(a) * dist, 0.15))
    cam.rotation_euler = (ctr - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(OUT, "alex_" + name + ".png")
    bpy.ops.render.render(write_still=True)


shot("bind_front", 0); shot("bind_side", 60)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
P = arm.pose.bones
for n, r in (("lowerarm_l", (0, 0, -70)), ("lowerarm_r", (0, 0, -70)), ("upperarm_l", (0, 40, 0)),
             ("thigh_l", (0, 0, 30)), ("calf_l", (0, 0, -45)), ("spine_02", (0, 0, 10)), ("head", (0, 0, 15))):
    if n in P:
        P[n].rotation_mode = "XYZ"; P[n].rotation_euler = Euler([math.radians(v) for v in r])
bpy.ops.object.mode_set(mode="OBJECT")
shot("pose_front", 20); shot("pose_side", 80)
print("PREVIEW_OK")
