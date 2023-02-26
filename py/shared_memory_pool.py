import mmap

class SharedMemoryPool:

  def __init__(self, file_path: str, buf_capacity: int, pool_size: int = 1):
    self._file_path = file_path
    self._mm = None
    self._buf_capacity = buf_capacity
    self._pool_size = pool_size
    total_size = self._pool_size * (self._buf_capacity + 6)
    try:
      file = open(self._file_path, "r+b")
    except:
      file = open(self._file_path, "w+b")
    file.write(b'\0' * self._pool_size * 6)
    self._mm = mmap.mmap(file.fileno(), total_size)
    file.close()
  
  def __del__(self):
    self._mm.close()
  
  def __enter__(self):
    return self
  
  def __exit__(self, ex_type, ex_value, trace):
    self.__del__()
  
  def flush(self):
    self._mm.flush()
    self._mm.seek(0)
    self._mm.write(b'\0' * self._pool_size * 6)

  def try_read(self) -> tuple[bool, bytes]:
    for t in range(1, self._pool_size + 1):
      for i in range(self._pool_size):
        if self._mm[i] == t:
          locker = self._pool_size + i
          if self._mm[locker] != 2: break
          self._mm[locker] = 1
          self._mm.seek(2 * self._pool_size + i * 4)
          size = int.from_bytes(self._mm.read(4), 'little')
          self._mm.seek(6 * self._pool_size + i * self._buf_capacity)
          buf = self._mm.read(size)
          self._mm[locker] = 3
          return True, buf
    return False, None

  def try_write(self, data: bytes) -> bool:
    for i in range(self._pool_size):
      order = self._mm[i]
      if order == self._pool_size or order == 0:
        locker = self._pool_size + i
        if self._mm[locker] == 1: break
        self._mm[locker] = 1
        self._mm.seek(2 * self._pool_size + i * 4)
        self._mm.write(len(data).to_bytes(4, 'little'))
        self._mm.seek(6 * self._pool_size + i * self._buf_capacity)
        self._mm.write(data)
        self._mm[locker] = 2
        for j in range(self._pool_size):
          if self._mm[j] > 0: 
            self._mm[j] += 1
        self._mm[i] = 1
        return True
    return False
