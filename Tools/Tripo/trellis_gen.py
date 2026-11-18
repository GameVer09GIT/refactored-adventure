# Ảnh → model 3D bằng TRELLIS (Microsoft, MIT) qua Hugging Face Space miễn phí.
#   python -I Tools/Tripo/trellis_gen.py <ảnh> <file.glb> [seed]
import shutil, sys, time
from PIL import Image
from gradio_client import Client, handle_file

src, out = sys.argv[1], sys.argv[2]
seed = int(sys.argv[3]) if len(sys.argv) > 3 else 1
# Cắt vuông quanh nhân vật (ảnh 16:9 thừa hai bên) để TRELLIS thấy rõ người
im = Image.open(src).convert("RGB")
w, h = im.size
side = min(w, h)
cx = w // 2
sq = im.crop((cx - side // 2, 0, cx + side // 2, side))
tmp = out.replace(".glb", "_input.png")
sq.save(tmp)

c = Client("trellis-community/TRELLIS", verbose=False)
try:
    c.predict(api_name="/start_session")
except Exception as e:
    print("start_session:", e)
t = time.time()
res = c.predict(handle_file(tmp), [], seed, 7.5, 12, 3.0, 12, "stochastic", 0.9, 2048, api_name="/generate_and_extract_glb")
glb = res[2] if res[2] else res[1]
shutil.copy(glb, out)
print("OK", out, "%.0fs" % (time.time() - t))
