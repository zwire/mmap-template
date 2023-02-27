import time
import cv2
from shared_memory_pool import *

if __name__ == "__main__":
  cap = cv2.VideoCapture(os.path.join(os.path.dirname(__file__), "sample.mp4"))
  delay = 1 / cap.get(cv2.CAP_PROP_FPS)
  path = os.path.join(os.path.dirname(__file__), "shared.pool")
  buf_capacity = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH) * cap.get(cv2.CAP_PROP_FRAME_HEIGHT) * 3)
  with SharedMemoryPool(path, buf_capacity, 2) as memory:
    memory.flush()
    while True:
      time.sleep(delay)
      ret, frame = cap.read()
      if ret:
        buf = frame.tobytes()
        while memory.try_write(buf) == False: pass
        print(f"Wrote {len(buf)} bytes.")
      else: 
        break