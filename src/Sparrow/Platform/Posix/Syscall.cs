﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Voron.Platform.Posix;

namespace Sparrow.Platform.Posix
{
    public static unsafe class Syscall
    {
        internal const string LIBC_6 = "libc";

        [DllImport(LIBC_6, EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = false)]
        [SecurityCritical]
        public static extern IntPtr Copy(byte* dest, byte* src, long count);

        [DllImport(LIBC_6, EntryPoint = "memcmp", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SecurityCritical]
        public static extern int Compare(byte* b1, byte* b2, long count);

        [DllImport(LIBC_6, EntryPoint = "memmove", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SecurityCritical]
        public static extern int Move(byte* dest, byte* src, long count);

        [DllImport(LIBC_6, EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SecurityCritical]
        public static extern IntPtr Set(byte* dest, int c, long count);

        [DllImport(LIBC_6, EntryPoint = "syscall", SetLastError = true)]
        public static extern long syscall0(long number);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int sched_getaffinity(int pid, IntPtr cpusetsize, ref ulong cpuset);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int sched_setaffinity(int pid, IntPtr cpusetsize, ref ulong cpuset);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int setpriority(int which, int who, int prio);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int getpriority(int which, int who);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int sysinfo(ref sysinfo_t info);
        
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int sysinfo(ref sysinfo_t_32bit info);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern long times(ref TimeSample info);
        
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern long times(ref TimeSample_32bit info);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int sprintf(char* str, char* format);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int mkdir(
            [MarshalAs(UnmanagedType.LPStr)] string filename,
            [MarshalAs(UnmanagedType.U2)] ushort mode);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int close(int fd);

        // pread(2)
        //    ssize_t pread(int fd, void *buf, size_t count, off_t offset);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern IntPtr pread(int fd, IntPtr buf, UIntPtr count, UIntPtr offset);

        public static long pread(int fd, void* buf, ulong count, long offset)
        {
            return (long)pread(fd, (IntPtr)buf, (UIntPtr)count, (UIntPtr)offset);
        }

        // posix_fallocate(P)
        //    int posix_fallocate(int fd, off_t offset, size_t len);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int posix_fallocate64(int fd, long offset, long len);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int msync(IntPtr start, UIntPtr len, MsyncFlags flags);


        [DllImport(LIBC_6, EntryPoint = "mmap64", SetLastError = true)]
        private static extern IntPtr mmap64_posix(IntPtr start, UIntPtr length,
            MmapProts prot, MmapFlags flags, int fd, long offset);

        [DllImport(LIBC_6, EntryPoint = "mmap", SetLastError = true)]
        private static extern IntPtr mmap64_mac(IntPtr start, UIntPtr length,
            MmapProts prot, MmapFlags flags, int fd, long offset);

        public static IntPtr mmap64(IntPtr start, UIntPtr length,
            MmapProts prot, MmapFlags flags, int fd, long offset)
        {
            if (PlatformDetails.RunningOnMacOsx)
                return mmap64_mac(start, length, prot, flags, fd, offset);
            return mmap64_posix(start, length, prot, flags, fd, offset);
        }

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int munmap(IntPtr start, UIntPtr length);

        // posix_memalign(3)
        //     int posix_memalign(void** memptr, size_t alignment, size_t size);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int posix_memalign(byte** pPtr, IntPtr allignment, IntPtr count);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern void free(IntPtr ptr);

        // getpid(2)
        //    pid_t getpid(void);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int getpid();

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int unlink(
            [MarshalAs(UnmanagedType.LPStr)] string filename);

        // int mincore(void *addr, size_t length, unsigned char *vec);
        // The vec argument must point to an array containing at least (length+PAGE_SIZE-1) / PAGE_SIZE bytes
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int mincore(void* addr, IntPtr length, char* vec);

        // flock(2)
        //    int flock(int fd, int operation); 
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int flock(
            int fd,
            FLockOperations operation);

        [Flags]
        public enum FLockOperations
        {
            LOCK_SH = 1,
            LOCK_EX = 2,
            LOCK_NB = 4,
            LOCK_UN = 8
        }

        // open(2)
        //    int open(const char *pathname, int flags, mode_t mode);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int open(
            [MarshalAs(UnmanagedType.LPStr)] string pathname,
            [MarshalAs(UnmanagedType.I4)] OpenFlags flags,
            [MarshalAs(UnmanagedType.U2)] FilePermissions mode);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int fcntl(int fd, FcntlCommands cmd, IntPtr args);

        public static int FSync(int fd)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return fcntl(fd, FcntlCommands.F_FULLFSYNC, IntPtr.Zero); // F_FULLFSYNC ignores args
            }
            return fsync(fd);
        }


        [DllImport(LIBC_6, SetLastError = true)]
        private static extern int fsync(int fd);


        // read(2)
        //    ssize_t read(int fd, void *buf, size_t count);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern IntPtr read(int fd, IntPtr buf, UIntPtr count);

        public static long read(int fd, void* buf, ulong count)
        {
            return (long)read(fd, (IntPtr)buf, (UIntPtr)count);
        }


        // pwrite(2)
        //    ssize_t pwrite(int fd, const void *buf, size_t count, off_t offset);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern IntPtr pwrite(int fd, IntPtr buf, UIntPtr count, IntPtr offset);

        public static long pwrite(int fd, void* buf, ulong count, long offset)
        {
            return (long)pwrite(fd, (IntPtr)buf, (UIntPtr)count, (IntPtr)offset);
        }


        // write(2)
        //    ssize_t write(int fd, const void *buf, size_t count);
        [DllImport(LIBC_6, SetLastError = true)]
        public static extern IntPtr write(int fd, IntPtr buf, UIntPtr count);

        public static long write(int fd, void* buf, ulong count)
        {
            return (long)write(fd, (IntPtr)buf, (UIntPtr)count);
        }


        [DllImport(LIBC_6, SetLastError = true)]
        public static extern long sysconf(int name, Errno defaultError);

        public static long sysconf(int name)
        {
            return sysconf(name, 0);
        }


        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int madvise(IntPtr addr, UIntPtr length, MAdvFlags madvFlags);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int ftruncate(int fd, IntPtr size);

        [DllImport(LIBC_6, EntryPoint = "lseek64", SetLastError = true)]
        public static extern long lseek64_posix(int fd, long offset, WhenceFlags whence);

        [DllImport(LIBC_6, EntryPoint = "lseek", SetLastError = true)]
        public static extern long lseek64_mac(int fd, long offset, WhenceFlags whence);

        public static long lseek64(int fd, long offset, WhenceFlags whence)
        {
            if (PlatformDetails.RunningOnMacOsx)
                return lseek64_mac(fd, offset, whence);
            return lseek64_posix(fd, offset, whence);
        }

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int statvfs(string path, ref Statvfs buf);

        [DllImport(LIBC_6, SetLastError = true)]
        public static extern int mprotect(IntPtr start, ulong size, ProtFlag protFlag);

        public static int AllocateFileSpace(int fd, long size, string file, out bool usingWrite)
        {
            usingWrite = false;
            int result;
            int retries = 1024;
            while (true)
            {
                var len = new FileInfo(file).Length;
                result = (int)Errno.EINVAL;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == false)
                    result = posix_fallocate64(fd, len, (size - len));
                switch (result)
                {
                    case (int)Errno.EINVAL:
                        // fallocate is not supported, we'll use lseek instead
                        usingWrite = true;
                        byte b = 0;
                        if (pwrite(fd, &b, 1, size - 1) != 1)
                        {
                            var err = Marshal.GetLastWin32Error();
                            ThrowLastError(err, "Failed to pwrite in order to fallocate where fallocate is not supported for " + file);
                        }
                        return 0;
                }

                if (result != (int)Errno.EINTR)
                    break;
                if (retries-- > 0)
                    throw new IOException($"Tried too many times to call posix_fallocate {file}, but always got EINTR, cannot retry again");
            }

            return result;
        }

        private static long? _pageSize;
        public static long PageSize
        {
            get
            {
                if (_pageSize == null)
                    _pageSize = sysconf(PerPlatformValues.SysconfNames._SC_PAGESIZE);
                return _pageSize.Value;
            }
        }


        public static void ThrowLastError(int lastError, string msg = null)
        {
            if (Enum.IsDefined(typeof(Errno), lastError) == false)
                throw new InvalidOperationException("Unknown errror ='" + lastError + "'. Message: " + msg);
            var error = (Errno)lastError;
            switch (error)
            {
                case Errno.ENOMEM:
                    throw new OutOfMemoryException("ENOMEM on " + msg);
                default:
                    throw new InvalidOperationException(error + " " + msg);
            }
        }


        public static void FsyncDirectoryFor(string file)
        {
            if (CheckSyncDirectoryAllowed(file) && SyncDirectory(file) == -1)
            {
                var err = Marshal.GetLastWin32Error();
                ThrowLastError(err, "FSync dir " + file);
            }
        }

        public static bool CheckSyncDirectoryAllowed(string filepath)
        {
            var allMounts = DriveInfo.GetDrives();
            var syncAllowed = true;
            var matchSize = 0;
            foreach (var m in allMounts)
            {
                var mountNameSize = m.Name.Length;
                if (filepath.StartsWith(m.Name))
                {
                    if (mountNameSize > matchSize)
                    {
                        matchSize = mountNameSize;
                        switch (m.DriveFormat)
                        {
                            case "cifs":
                            case "nfs":
                                syncAllowed = false;
                                break;
                            default:
                                syncAllowed = true;
                                break;
                        }
                        if (m.DriveType == DriveType.Unknown)
                            syncAllowed = false;
                    }
                }
            }
            return syncAllowed;
        }

        public static int SyncDirectory(string file)
        {
            var dir = Path.GetDirectoryName(file);
            var fd = open(dir, 0, 0);
            if (fd == -1)
                return -1;
            var fsyncRc = FSync(fd);
            if (fsyncRc == -1)
                return -1;
            return close(fd);
        }

        public static string GetRootMountString(DriveInfo[] drivesInfo, string filePath)
        {
            string root = null;
            var matchSize = 0;

            foreach (var driveInfo in drivesInfo)
            {
                var mountNameSize = driveInfo.Name.Length;
                if (filePath.StartsWith(driveInfo.Name) == false)
                    continue;

                if (matchSize >= mountNameSize)
                    continue;

                matchSize = mountNameSize;
                root = driveInfo.Name;
            }

            return root;
        }
    }

    [Flags]
    public enum FcntlCommands
    {
        F_NOCACHE = 0x00000030,
        F_FULLFSYNC = 0x00000033
    }

    public unsafe struct Statvfs
    {
        public ulong f_bsize;    /* file system block size */
        public ulong f_frsize;   /* fragment size */
        public ulong f_blocks;   /* size of fs in f_frsize units */
        public ulong f_bfree;    /* # free blocks */
        public ulong f_bavail;   /* # free blocks for unprivileged users */
        public ulong f_files;    /* # inodes */
        public ulong f_ffree;    /* # free inodes */
        public ulong f_favail;   /* # free inodes for unprivileged users */
        public ulong f_fsid;     /* file system ID */
        public ulong f_flag;     /* mount flags */
        public ulong f_namemax;  /* maximum filename length */
        public fixed int f_spare[6];
    }
}
