# Ảnh → model 3D bằng TRELLIS.2 (Microsoft, MIT; bản 2025, chi tiết + texture PBR cao hơn TRELLIS 1)
# qua Hugging Face Space miễn phí microsoft/TRELLIS.2.
#   python -I Tools/Tripo/trellis2_gen.py <ảnh vuông> <file.glb> [seed] [resolution 512|1024|1536] [texture]
import os, shutil, sys, time
from gradio_client import Client, handle_file

src, out = sys.argv[1], sys.argv[2]
seed = int(sys.argv[3]) if len(sys.argv) > 3 else 1
res = sys.argv[4] if len(sys.argv) > 4 else "1536"
tex = int(sys.argv[5]) if len(sys.argv) > 5 else 4096

# Token Hugging Face (miễn phí, nhiều hạn mức GPU hơn khách) đặt NGOÀI repo: %USERPROFILE%/.blackwood/hf_token
_tok = os.path.join(os.path.expanduser("~"), ".blackwood", "hf_token")
token = open(_tok, encoding="utf-8").read().strip() if os.path.exists(_tok) else None
print("HF token:", "có" if token else "không (dùng hạn mức khách)")
c = Client("microsoft/TRELLIS.2", verbose=False, **({"token": token} if token else {}))
try:
    c.predict(api_name="/start_session")
except Exception as e:
    print("start_session:", e)
t = time.time()
img = c.predict(handle_file(src), api_name="/preprocess_image")  # tách nền
print("preprocess", "%.0fs" % (time.time() - t))
# Bước lấy mẫu nhiều hơn mặc định (12) cho hình + texture kỹ hơn
st = int(sys.argv[6]) if len(sys.argv) > 6 else 20
c.predict(handle_file(img), seed, res, 7.5, 0.7, st, 5.0, 7.5, 0.5, st, 3.0, 1.0, 0.0, st, 3.0, api_name="/image_to_3d")
print("image_to_3d", "%.0fs" % (time.time() - t))
glb, dl = c.predict(400000, tex, api_name="/extract_glb")
shutil.copy(dl or glb, out)
print("OK", out, "%.0fs" % (time.time() - t))
