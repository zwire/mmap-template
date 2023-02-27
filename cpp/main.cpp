#include "shared_memory_pool.h"
#include <vector>

int main(void)
{
    const int buf_capacity = 1920 * 1080 * 3;
    std::vector<unsigned char> buf(buf_capacity);
    SharedMemoryPool memory("../py/shared.pool", buf_capacity, 2);
    memory.try_write(buf.data(), buf.size());

    int buf_size = 0;
    while (true)
    {
        if (memory.try_read(buf.data(), buf_size))
        {
            std::cout << "Received: " << buf_size << " bytes." << std::endl;
        }
        else sleep_msec(10);
    }
    return 0;
}