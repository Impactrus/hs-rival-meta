using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HearthstoneDeckTracker
{
    public static class MemoryReader
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            VirtualMemoryRead = 0x0010,
            QueryInformation = 0x0400
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public const uint MEM_COMMIT = 0x1000;
        public const uint PAGE_READWRITE = 0x04;
        public const uint PAGE_READONLY = 0x02;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Reads RAM memory of running Hearthstone process and extracts owned card DBF IDs + counts.
        /// </summary>
        public static Dictionary<int, int> ScanCollectionFromRam(HashSet<int> validDbfIds)
        {
            var result = new Dictionary<int, int>();
            if (validDbfIds == null || validDbfIds.Count == 0) return result;

            var proc = Process.GetProcessesByName("Hearthstone").FirstOrDefault();
            if (proc == null || proc.HasExited) return result;

            IntPtr hProcess = OpenProcess((uint)(ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.QueryInformation), false, proc.Id);
            if (hProcess == IntPtr.Zero) return result;

            try
            {
                IntPtr address = IntPtr.Zero;
                long maxAddress = 0x7FFFFFFF0000; // Standard 64-bit user space range limit

                while ((long)address < maxAddress)
                {
                    int queryResult = VirtualQueryEx(hProcess, address, out MEMORY_BASIC_INFORMATION mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                    if (queryResult == 0) break;

                    long regionSize = (long)mbi.RegionSize;
                    if (regionSize <= 0) break;

                    // Only scan committed read/write RAM memory blocks
                    bool isReadable = mbi.State == MEM_COMMIT && 
                                     (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_READONLY);

                    if (isReadable && regionSize > 0 && regionSize <= 20 * 1024 * 1024)
                    {
                        byte[] buffer = new byte[Math.Min(regionSize, 2 * 1024 * 1024)];
                        if (ReadProcessMemory(hProcess, mbi.BaseAddress, buffer, buffer.Length, out IntPtr bytesRead))
                        {
                            int readLen = (int)bytesRead;
                            for (int i = 0; i <= readLen - 8; i += 4)
                            {
                                int candidateDbfId = BitConverter.ToInt32(buffer, i);
                                if (validDbfIds.Contains(candidateDbfId))
                                {
                                    int countCandidate = BitConverter.ToInt32(buffer, i + 4);
                                    if (countCandidate >= 1 && countCandidate <= 9)
                                    {
                                        if (result.TryGetValue(candidateDbfId, out int existing))
                                        {
                                            result[candidateDbfId] = Math.Max(existing, countCandidate);
                                        }
                                        else
                                        {
                                            result[candidateDbfId] = countCandidate;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    address = new IntPtr((long)mbi.BaseAddress + regionSize);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RAM scan exception: " + ex.Message);
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return result;
        }
    }
}
