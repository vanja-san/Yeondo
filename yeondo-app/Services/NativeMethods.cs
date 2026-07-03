using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Yeondo.Services;

/// <summary>
/// Win32 P/Invoke declarations для создания символьных ссылок
/// </summary>
public static partial class NativeMethods
{
  public const int SYMBOLIC_LINK_FLAG_FILE = 0;
  public const int SYMBOLIC_LINK_FLAG_DIRECTORY = 1;
  public const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2;

  // Константы для junction (reparse point)
  private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
  private const uint FSCTL_SET_REPARSE_POINT = 0x000900A4;
  private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
  private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
  private const uint OPEN_EXISTING = 3;
  private const uint FILE_READ_EA = 0x0008;
  private const uint FILE_WRITE_EA = 0x0010;
  private const uint FILE_SHARE_READ = 0x00000001;
  private const uint FILE_SHARE_WRITE = 0x00000002;

  [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "CreateSymbolicLinkW")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool CreateSymbolicLinkNative(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

  [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "CreateHardLinkW")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool CreateHardLinkNative(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

  [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "CreateFileW")]
  private static partial SafeFileHandle CreateFileNative(
      string lpFileName,
      uint dwDesiredAccess,
      uint dwShareMode,
      IntPtr lpSecurityAttributes,
      uint dwCreationDisposition,
      uint dwFlagsAndAttributes,
      IntPtr hTemplateFile);

  [LibraryImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool DeviceIoControl(
      SafeFileHandle hDevice,
      uint dwIoControlCode,
      IntPtr lpInBuffer,
      uint nInBufferSize,
      IntPtr lpOutBuffer,
      uint nOutBufferSize,
      out uint lpBytesReturned,
      IntPtr lpOverlapped);

  public static (bool success, string? error) CreateSymbolicLink(string link, string target, int flags)
  {
    bool result = CreateSymbolicLinkNative(link, target, flags);
    if (!result)
    {
      int error = Marshal.GetLastWin32Error();
      return (false, new Win32Exception(error).Message);
    }
    return (true, null);
  }

  public static (bool success, string? error) CreateHardLink(string link, string target)
  {
    bool result = CreateHardLinkNative(link, target, IntPtr.Zero);
    if (!result)
    {
      int error = Marshal.GetLastWin32Error();
      return (false, new Win32Exception(error).Message);
    }
    return (true, null);
  }

  /// <summary>
  /// Создание NTFS Junction (reparse point) через DeviceIoControl с FSCTL_SET_REPARSE_POINT.
  /// </summary>
  public static (bool success, string? error) CreateJunction(string junctionPath, string targetPath)
  {
    // Открываем директорию, где будет создана junction
    SafeFileHandle? hDir = null;
    IntPtr reparseBuffer = IntPtr.Zero;
    try
    {
      hDir = CreateFileNative(
          junctionPath,
          FILE_READ_EA | FILE_WRITE_EA,
          FILE_SHARE_READ | FILE_SHARE_WRITE,
          IntPtr.Zero,
          OPEN_EXISTING,
          FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
          IntPtr.Zero);

      if (hDir.IsInvalid)
      {
        int error = Marshal.GetLastWin32Error();
        return (false, new Win32Exception(error).Message);
      }

      // Формируем REPARASE_DATA_BUFFER для Junction
      // Пути в формате NT: \??\C:\path\to\target
      string ntTargetPath = @"\??\" + Path.GetFullPath(targetPath);
      string ntPrintPath = Path.GetFullPath(targetPath);

      // Внимание: все длины в байтах, не в символах!
      int substituteNameBytes = (ntTargetPath.Length + 1) * 2; // +1 для терминирующего null
      int printNameBytes = (ntPrintPath.Length + 1) * 2;

      // REPARSE_DATA_BUFFER layout:
      //   ReparseTag (4)
      //   ReparseDataLength (2)
      //   Reserved (2)
      //   MountPointReparseBuffer:
      //     SubstituteNameOffset (2)
      //     SubstituteNameLength (2)
      //     PrintNameOffset (2)
      //     PrintNameLength (2)
      //     PathBuffer (variable)
      const ushort reparseDataHeaderSize = 8; // ReparseTag + ReparseDataLength + Reserved
      const ushort mountPointHeaderSize = 8;  // SubstituteNameOffset + SubstituteNameLength + PrintNameOffset + PrintNameLength
      ushort pathBufferSize = (ushort)(substituteNameBytes + printNameBytes);
      ushort reparseDataLength = (ushort)(mountPointHeaderSize + pathBufferSize);
      uint totalSize = (uint)(reparseDataHeaderSize + reparseDataLength);

      reparseBuffer = Marshal.AllocHGlobal((int)totalSize);
      try
      {
        // Обнуляем буфер
        for (int i = 0; i < (int)totalSize; i++)
          Marshal.WriteByte(reparseBuffer, i, 0);

        // Fill REPARSE_DATA_BUFFER
        int offset = 0;

        // ReparseTag (4 байта)
        Marshal.WriteInt32(reparseBuffer, offset, unchecked((int)IO_REPARSE_TAG_MOUNT_POINT));
        offset += 4;

        // ReparseDataLength (2 байта)
        Marshal.WriteInt16(reparseBuffer, offset, (short)reparseDataLength);
        offset += 2;

        // Reserved (2 байта) - пропускаем
        offset += 2;

        // SubstituteNameOffset (2 байта)
        offset += 2; // Skip - will set later

        // SubstituteNameLength (2 байта) - будет установлено позже
        offset += 2;

        // PrintNameOffset (2 байта) - будет установлено позже
        offset += 2;

        // PrintNameLength (2 байта) - будет установлено позже
        offset += 2;

        // Now write PathBuffer
        int pathBufferStart = offset;

        // Устанавливаем SubstituteNameOffset и SubstituteNameLength
        ushort subNameOffset = 0; // сразу после MountPointReparseBuffer
        ushort subNameLength = (ushort)(ntTargetPath.Length * 2); // без null

        // Write SubstituteNameOffset
        Marshal.WriteInt16(reparseBuffer, 8, (short)subNameOffset);
        // Write SubstituteNameLength
        Marshal.WriteInt16(reparseBuffer, 10, (short)subNameLength);

        // Write PrintNameOffset
        ushort printNameOffset = (ushort)(subNameOffset + subNameLength + 2); // после substitute name + null
        Marshal.WriteInt16(reparseBuffer, 12, (short)printNameOffset);
        // Write PrintNameLength
        ushort printNameLength = (ushort)(ntPrintPath.Length * 2);
        Marshal.WriteInt16(reparseBuffer, 14, (short)printNameLength);

        // Write substitute name + null terminator (UTF-16)
        byte[] subNameBytes = System.Text.Encoding.Unicode.GetBytes(ntTargetPath + '\0');
        for (int i = 0; i < subNameBytes.Length; i++)
          Marshal.WriteByte(reparseBuffer, pathBufferStart + i, subNameBytes[i]);

        // Write print name + null terminator
        int printNameStart = pathBufferStart + subNameBytes.Length;
        byte[] printNameBytesArr = System.Text.Encoding.Unicode.GetBytes(ntPrintPath + '\0');
        for (int i = 0; i < printNameBytesArr.Length; i++)
          Marshal.WriteByte(reparseBuffer, printNameStart + i, printNameBytesArr[i]);

        // Вызываем DeviceIoControl
        bool ioctlResult = DeviceIoControl(
            hDir,
            FSCTL_SET_REPARSE_POINT,
            reparseBuffer,
            totalSize,
            IntPtr.Zero,
            0,
            out _,
            IntPtr.Zero);

        if (!ioctlResult)
        {
          int error = Marshal.GetLastWin32Error();
          return (false, new Win32Exception(error).Message);
        }

        return (true, null);
      }
      finally
      {
        Marshal.FreeHGlobal(reparseBuffer);
      }
    }
    finally
    {
      hDir?.Dispose();
    }
  }
}
