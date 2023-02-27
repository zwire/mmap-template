#ifndef SHARED_MEMORY_POOL_H_
#define SHARED_MEMORY_POOL_H_

#include <iostream>
#include <memory>
#include <string.h>

#ifdef __unix__
#include <unistd.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <fcntl.h>
#define sleep_msec(n) usleep(n * 1e+3)
#else
#include <stdio.h>
#include <windows.h>
#define sleep_msec(n) Sleep(n)
#endif

class SharedMemoryPool final
{

private:
#ifdef __unix__
	int _fd;
#else
	HANDLE _map_handle;
#endif
	char* _data;
	int _buf_capacity;
	int _pool_size;

public:
	explicit SharedMemoryPool(
		const char* file_path,
		const int& buf_capacity,
		const int& pool_size = 1
	)
	{
		_buf_capacity = buf_capacity;
		_pool_size = pool_size;
#ifdef __unix__
		int page_size = sysconf(_SC_PAGE_SIZE);
		int total_size = (_pool_size * (_buf_capacity + 6) / page_size + 1) * page_size;
		_fd = open(file_path, O_RDWR);
		if (_fd == -1) 
		{
			_fd = open(file_path, O_RDWR | O_CREAT);
			if (_fd == -1) throw "file open failed";
			char buf[total_size];
			write(_fd, buf, total_size);
		}
		lseek(_fd, 0, SEEK_SET);
		_data = (char*)mmap(NULL, total_size, PROT_READ | PROT_WRITE, MAP_SHARED, _fd, 0);
		if (_data == MAP_FAILED) throw "file mapping failed";
#else
		wchar_t fname[260];
		int i = 0;
		while (*file_path != '\0') 
		{
			mbtowc(&fname[i++], file_path, MB_CUR_MAX);
			file_path++;
		}
		fname[i] = '\0';
		HANDLE handle = CreateFileW(
			fname, 
			GENERIC_READ | GENERIC_WRITE, 
			FILE_SHARE_READ | FILE_SHARE_WRITE, 
			0, 
			OPEN_EXISTING, 
			FILE_ATTRIBUTE_NORMAL, 
			0
		);
		if (handle == INVALID_HANDLE_VALUE)
		{
			handle = CreateFileW(
				fname,
				GENERIC_READ | GENERIC_WRITE,
				FILE_SHARE_READ | FILE_SHARE_WRITE,
				0,
				CREATE_NEW,
				FILE_ATTRIBUTE_NORMAL,
				0
			);
			if (handle == INVALID_HANDLE_VALUE) throw "file open failed";
		}
		int total_size = (_buf_capacity + 6) * _pool_size;
		_map_handle = CreateFileMappingW(handle, 0, PAGE_READWRITE, 0, total_size, NULL);
		if (_map_handle == 0) throw "file mapping failed";
		_data = (char*)MapViewOfFile(_map_handle, FILE_MAP_ALL_ACCESS, 0, 0, total_size);
		if (_data == 0) throw "file mapping failed";
		CloseHandle(handle);
		handle = INVALID_HANDLE_VALUE;
#endif
	}

	~SharedMemoryPool()
	{
#ifdef __unix__
		close(_fd);
#else
		UnmapViewOfFile(_data);
		if (_map_handle != INVALID_HANDLE_VALUE) 
		{
			CloseHandle(_map_handle);
			_map_handle = INVALID_HANDLE_VALUE;
		}
		_data = NULL;
#endif
	}

	void flush()
	{
		memset(_data, 0, _pool_size * 6);
	}

	bool try_read(void* buf, int& size) const
	{
		for (int t = 1; t <= _pool_size; t++)
		{
			for (int i = 0; i < _pool_size; i++)
			{
				if (_data[i] == t)
				{
					int locker = _pool_size + i;
					if (_data[locker] != 2) break;
					_data[locker] = 1;
					unsigned char b_size[4];
					memcpy(b_size, _data + 2 * _pool_size + i * 4, 4);
					size = b_size[0] | b_size[1] << 8 | b_size[2] << 16 | b_size[3] << 24;
					memcpy(buf, _data + 6 * _pool_size + i * _buf_capacity, size);
					_data[locker] = 3;
					return true;
				}
			}
		}
		return false;
	}

	bool try_write(const void* buf, int size) const
	{
		char b_size[4];
		b_size[0] = size;
		b_size[1] = size >> 8;
		b_size[2] = size >> 16;
		b_size[3] = size >> 24;
		for (int i = 0; i < _pool_size; i++)
		{
			int order = _data[i];
			if (order == _pool_size || order == 0)
			{
				int locker = _pool_size + i;
				if (_data[locker] == 1) break;
				_data[locker] = 1;
				memcpy(_data + 2 * _pool_size + i * 4, b_size, 4);
				memcpy(_data + 6 * _pool_size + i * _buf_capacity, buf, size);
				_data[locker] = 2;
				for (int j = 0; j < _pool_size; j++)
					if (_data[j] > 0)
						_data[j] = _data[j] + 1;
				_data[i] = 1;
				return true;
			}
		}
		return false;
	}

};

#endif
