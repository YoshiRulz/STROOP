using STROOP.Structs;
using STROOP.Structs.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STROOP.Utilities
{
    public class ProcessStream : IDisposable
    {   IEmuRamIO _io;
        public IEmuRamIO IO => _io;

        ConcurrentQueue<double> _fpsTimes = new ConcurrentQueue<double>();
        byte[] _ram;
        bool _lastUpdateBeforePausing = false;
        object _enableLocker = new object();
        object _mStreamProcess = new object();

        public event EventHandler OnUpdate;
        public event EventHandler OnDisconnect;
        public event EventHandler FpsUpdated;
        public event EventHandler WarnReadonlyOff;

        public bool Readonly { get; set; } = false;
        public bool ShowWarning { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public bool IsRunning { get; private set; } = false;

        public byte[] Ram => _ram;
        public string ProcessName => _io?.Name ?? "(No Emulator)";
        public bool IsSuspended => _io?.IsSuspended ?? false;
        public double FpsInPractice => _fpsTimes.Count == 0 ? 0 : 1000 / _fpsTimes.Average();
        Task _mainTask;

        public ProcessStream()
        {
            _ram = new byte[Config.RamSize];
            _mainTask = Task.Run(() => ProcessUpdate());
        }

        /// <summary>
        /// Workaround for WinForms Threading
        /// </summary>
        public async Task WaitForDispose()
        {
            await _mainTask;
        }

        private void LogException(Exception e)
        {
            try
            {
                var log = String.Format("{0}\n{1}\n{2}\n", e.Message, e.TargetSite.ToString(), e.StackTrace);
                File.AppendAllText("error.txt", log);
            }
            catch (Exception) { }
        }

        private void ExceptionHandler(Task obj)
        {
            LogException(obj.Exception);
            throw obj.Exception;
        }

        private readonly Dictionary<Type, Func<Process, Emulator, IEmuRamIO>> _ioCreationTable = new Dictionary<Type, Func<Process, Emulator, IEmuRamIO>>()
        {
            { typeof(WindowsProcessRamIO),  (p, e) => new WindowsProcessRamIO(p, e) },
            { typeof(DolphinProcessIO),     (p, e) => new DolphinProcessIO(p, e) },
        };

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        public void FocusOnEmulator()
        {
            Process process = _io.Process;
            if (process == null) return;
            SetForegroundWindow(process.MainWindowHandle);
        }

        public bool SwitchIO(IEmuRamIO newIO)
        {
            lock (_mStreamProcess)
            {
                IsRunning = false;

                // Dipose of old process
                (_io as IDisposable)?.Dispose();
                if (_io != null)
                    _io.OnClose -= ProcessClosed;

                // Check for no process
                if (newIO == null)
                    goto Error;

                try
                {
                    // Open and set new process
                    _io = newIO;
                    _io.OnClose += ProcessClosed;
                }
                catch (Exception) // Failed to create process
                {
                    goto Error;
                }

                IsEnabled = true;
                IsRunning = true;

                return true;

                Error:
                _io = null;
                return false;
            }
        }

        public bool OpenSTFile(string fileName)
        {
            StFileIO fileIO = new StFileIO(fileName);
            return SwitchIO(fileIO);
        }

        public bool SwitchProcess(Process newProcess, Emulator emulator)
        {
            IEmuRamIO newIo = null;
            try
            {
                newIo = newProcess != null ? _ioCreationTable[emulator.IOType](newProcess, emulator) : null;
            }
            catch (DolphinProcessIO.DolphinProcessException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return SwitchIO(newIo);
        }

        public void Suspend()
        {
            _lastUpdateBeforePausing = true;
            _io?.Suspend();
        }

        public void Resume()
        {
            _io?.Resume();
        }

        private void ProcessClosed(object sender, EventArgs e)
        {
            IsEnabled = false;
            OnDisconnect?.Invoke(this, new EventArgs());
        }

        public UIntPtr GetAbsoluteAddress(uint relativeAddress, int size = 0)
        {
            return _io?.GetAbsoluteAddress(relativeAddress, size) ?? new UIntPtr(0);
        }

        public uint GetRelativeAddress(UIntPtr relativeAddress, int size)
        {
            return _io?.GetRelativeAddress(relativeAddress, size) ?? 0;
        }

        public object GetValue(Type type, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (type == typeof(byte)) return GetByte(address, absoluteAddress, mask, shift);
            if (type == typeof(sbyte)) return GetSByte(address, absoluteAddress, mask, shift);
            if (type == typeof(short)) return GetShort(address, absoluteAddress, mask, shift);
            if (type == typeof(ushort)) return GetUShort(address, absoluteAddress, mask, shift);
            if (type == typeof(int)) return GetInt(address, absoluteAddress, mask, shift);
            if (type == typeof(uint)) return GetUInt(address, absoluteAddress, mask, shift);
            if (type == typeof(float)) return GetFloat(address, absoluteAddress, mask, shift);
            if (type == typeof(double)) return GetDouble(address, absoluteAddress, mask, shift);

            throw new ArgumentOutOfRangeException("Cannot call ProcessStream.GetValue with type " + type);
        }

        public byte GetByte(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(byte), mask, shift);
            byte? parsedValue = ParsingUtilities.ParseByteRoundingWrapping(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            byte value = ReadRam((UIntPtr)address, 1, EndiannessType.Little, absoluteAddress)[0];
            if (mask.HasValue) value = (byte)(value & mask.Value);
            if (shift.HasValue) value = (byte)(value >> shift.Value);
            return value;
        }

        public sbyte GetSByte(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(sbyte), mask, shift);
            sbyte? parsedValue = ParsingUtilities.ParseSByteRoundingWrapping(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            sbyte value = (sbyte)ReadRam((UIntPtr)address, 1, EndiannessType.Little, absoluteAddress)[0];
            if (mask.HasValue) value = (sbyte)(value & mask.Value);
            if (shift.HasValue) value = (sbyte)(value >> shift.Value);
            return value;
        }

        public short GetShort(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(short), mask, shift);
            short? parsedValue = ParsingUtilities.ParseShortRoundingWrapping(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            short value = BitConverter.ToInt16(ReadRam((UIntPtr)address, 2, EndiannessType.Little, absoluteAddress), 0);
            if (mask.HasValue) value = (short)(value & mask.Value);
            if (shift.HasValue) value = (short)(value >> shift.Value);
            return value;
        }

        public ushort GetUShort(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(ushort), mask, shift);
            ushort? parsedValue = ParsingUtilities.ParseUShortRoundingWrapping(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            ushort value = BitConverter.ToUInt16(ReadRam((UIntPtr)address, 2, EndiannessType.Little, absoluteAddress), 0);
            if (mask.HasValue) value = (ushort)(value & mask.Value);
            if (shift.HasValue) value = (ushort)(value >> shift.Value);
            return value;
        }

        public int GetInt(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(int), mask, shift);
            int? parsedValue = ParsingUtilities.ParseIntRoundingWrapping(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            int value = BitConverter.ToInt32(ReadRam((UIntPtr)address, 4, EndiannessType.Little, absoluteAddress), 0);
            if (mask.HasValue) value = (int)(value & mask.Value);
            if (shift.HasValue) value = (int)(value >> shift.Value);
            return value;
        }

        public uint GetUInt(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(uint), mask, shift);
            uint? parsedValue = ParsingUtilities.ParseUIntRoundingWrapping(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            uint value = BitConverter.ToUInt32(ReadRam((UIntPtr)address, 4, EndiannessType.Little, absoluteAddress), 0);
            if (mask.HasValue) value = (uint)(value & mask.Value);
            if (shift.HasValue) value = (uint)(value >> shift.Value);
            return value;
        }

        public float GetFloat(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(float), mask, shift);
            float? parsedValue = ParsingUtilities.ParseFloatNullable(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            float value = BitConverter.ToSingle(ReadRam((UIntPtr)address, 4, EndiannessType.Little, absoluteAddress), 0);
            return value;
        }

        public double GetDouble(uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            object lockValue = Config.LockManager.GetMemoryLockValue(address, typeof(double), mask, shift);
            double? parsedValue = ParsingUtilities.ParseDoubleNullable(lockValue);
            if (parsedValue.HasValue) return parsedValue.Value;

            double value = BitConverter.ToDouble(ReadRam((UIntPtr)address, 8, EndiannessType.Little, absoluteAddress), 0);
            return value;
        }

        public byte[] ReadRam(uint address, int length, EndiannessType endianness, bool absoluteAddress = false)
        {
             return ReadRam((UIntPtr) address, length, endianness, absoluteAddress);
        }
        
        public byte[] ReadRam(UIntPtr address, int length, EndiannessType endianness, bool absoluteAddress = false)
        {
            byte[] readBytes = new byte[length];

            // Get local address
            uint localAddress;
            if (absoluteAddress)
                localAddress = _io?.GetRelativeAddress(address, length) ?? 0;
            else
                localAddress = address.ToUInt32();
            localAddress &= ~0x80000000;

            if (EndiannessUtilities.DataIsMisaligned(address, length, EndiannessType.Big))
                return readBytes;
            
            /// Fix endianness
            switch (endianness)
            {
                case EndiannessType.Little:
                    // Address is not little endian, fix:
                    localAddress = EndiannessUtilities.SwapAddressEndianness(localAddress, length);

                    if (localAddress + length > _ram.Length)
                        break;

                    Buffer.BlockCopy(_ram, (int)localAddress, readBytes, 0, length);
                    break;

                case EndiannessType.Big:
                    // Read padded if misaligned address
                    byte[] swapBytes;
                    uint alignedAddress = EndiannessUtilities.AlignedAddressFloor(localAddress);

                    int alignedReadByteCount = (readBytes.Length / 4) * 4 + 8;
                    swapBytes = new byte[alignedReadByteCount];

                    // Read memory
                    Buffer.BlockCopy(_ram, (int)alignedAddress, swapBytes, 0, swapBytes.Length);
                    swapBytes = EndiannessUtilities.SwapByteEndianness(swapBytes);

                    // Copy memory
                    Buffer.BlockCopy(swapBytes, (int)(localAddress - alignedAddress), readBytes, 0, readBytes.Length);

                    break;
            }


            return readBytes;
        }

        public bool ReadProcessMemory(UIntPtr address, byte[] buffer, EndiannessType endianness)
        {
            return _io?.ReadAbsolute(address, buffer, endianness) ?? false;
        }

        public bool CheckReadonlyOff()
        {
            if (ShowWarning)
                WarnReadonlyOff?.Invoke(this, new EventArgs());

            return Readonly;
        }

        public bool SetValueRoundingWrapping (
            Type type, object value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            // Allow short circuiting if object is already of type
            if (type == typeof(byte) && value is byte byteValue) return SetValue(byteValue, address, absoluteAddress, mask, shift);
            if (type == typeof(sbyte) && value is sbyte sbyteValue) return SetValue(sbyteValue, address, absoluteAddress, mask, shift);
            if (type == typeof(short) && value is short shortValue) return SetValue(shortValue, address, absoluteAddress, mask, shift);
            if (type == typeof(ushort) && value is ushort ushortValue) return SetValue(ushortValue, address, absoluteAddress, mask, shift);
            if (type == typeof(int) && value is int intValue) return SetValue(intValue, address, absoluteAddress, mask, shift);
            if (type == typeof(uint) && value is uint uintValue) return SetValue(uintValue, address, absoluteAddress, mask, shift);
            if (type == typeof(float) && value is float floatValue) return SetValue(floatValue, address, absoluteAddress, mask, shift);
            if (type == typeof(double) && value is double doubleValue) return SetValue(doubleValue, address, absoluteAddress, mask, shift);

            value = ParsingUtilities.ParseDoubleNullable(value);
            if (value == null) return false;

            if (type == typeof(byte)) value = ParsingUtilities.ParseByteRoundingWrapping(value);
            if (type == typeof(sbyte)) value = ParsingUtilities.ParseSByteRoundingWrapping(value);
            if (type == typeof(short)) value = ParsingUtilities.ParseShortRoundingWrapping(value);
            if (type == typeof(ushort)) value = ParsingUtilities.ParseUShortRoundingWrapping(value);
            if (type == typeof(int)) value = ParsingUtilities.ParseIntRoundingWrapping(value);
            if (type == typeof(uint)) value = ParsingUtilities.ParseUIntRoundingWrapping(value);

            return SetValue(type, value.ToString(), address, absoluteAddress, mask, shift);
        }

        public bool SetValue(Type type, object value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (value is string)
            {
                if (type == typeof(byte)) value = ParsingUtilities.ParseByteNullable(value);
                if (type == typeof(sbyte)) value = ParsingUtilities.ParseSByteNullable(value);
                if (type == typeof(short)) value = ParsingUtilities.ParseShortNullable(value);
                if (type == typeof(ushort)) value = ParsingUtilities.ParseUShortNullable(value);
                if (type == typeof(int)) value = ParsingUtilities.ParseIntNullable(value);
                if (type == typeof(uint)) value = ParsingUtilities.ParseUIntNullable(value);
                if (type == typeof(float)) value = ParsingUtilities.ParseFloatNullable(value);
                if (type == typeof(double)) value = ParsingUtilities.ParseDoubleNullable(value);
            }

            if (value == null) return false;

            if (type == typeof(byte)) return SetValue((byte)value, address, absoluteAddress, mask, shift);
            if (type == typeof(sbyte)) return SetValue((sbyte)value, address, absoluteAddress, mask, shift);
            if (type == typeof(short)) return SetValue((short)value, address, absoluteAddress, mask, shift);
            if (type == typeof(ushort)) return SetValue((ushort)value, address, absoluteAddress, mask, shift);
            if (type == typeof(int)) return SetValue((int)value, address, absoluteAddress, mask, shift);
            if (type == typeof(uint)) return SetValue((uint)value, address, absoluteAddress, mask, shift);
            if (type == typeof(float)) return SetValue((float)value, address, absoluteAddress, mask, shift);
            if (type == typeof(double)) return SetValue((double)value, address, absoluteAddress, mask, shift);

            throw new ArgumentOutOfRangeException("Cannot call ProcessStream.SetValue with type " + type);
        }

        public bool SetValue(byte value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (shift.HasValue)
            {
                value = (byte)(value << shift.Value);
            }
            if (mask.HasValue)
            {
                byte oldValue = GetByte(address, absoluteAddress);
                value = (byte)((oldValue & ~mask.Value) | (value & mask.Value));
            }
            bool returnValue = WriteRam(new byte[] { value }, (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(byte), mask, shift);
            return returnValue;
        }

        public bool SetValue(sbyte value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (shift.HasValue)
            {
                value = (sbyte)(value << shift.Value);
            }
            if (mask.HasValue)
            {
                sbyte oldValue = GetSByte(address, absoluteAddress);
                value = (sbyte)((oldValue & ~mask.Value) | (value & mask.Value));
            }
            bool returnValue = WriteRam(new byte[] { (byte)value }, (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(sbyte), mask, shift);
            return returnValue;
        }

        public bool SetValue(Int16 value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (shift.HasValue)
            {
                value = (short)(value << shift.Value);
            }
            if (mask.HasValue)
            {
                short oldValue = GetShort(address, absoluteAddress);
                value = (short)((oldValue & ~mask.Value) | (value & mask.Value));
            }
            bool returnValue = WriteRam(BitConverter.GetBytes(value), (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(short), mask, shift);
            return returnValue;
        }

        public bool SetValue(UInt16 value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (shift.HasValue)
            {
                value = (ushort)(value << shift.Value);
            }
            if (mask.HasValue)
            {
                ushort oldValue = GetUShort(address, absoluteAddress);
                value = (ushort)((oldValue & ~mask.Value) | (value & mask.Value));
            }
            bool returnValue = WriteRam(BitConverter.GetBytes(value), (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(ushort), mask, shift);
            return returnValue;
        }

        public bool SetValue(Int32 value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (shift.HasValue)
            {
                value = (int)(value << shift.Value);
            }
            if (mask.HasValue)
            {
                int oldValue = GetInt(address, absoluteAddress);
                value = (int)((oldValue & ~mask.Value) | (value & mask.Value));
            }
            bool returnValue = WriteRam(BitConverter.GetBytes(value), (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(int), mask, shift);
            return returnValue;
        }

        public bool SetValue(UInt32 value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            if (shift.HasValue)
            {
                value = (uint)(value << shift.Value);
            }
            if (mask.HasValue)
            {
                uint oldValue = GetUInt(address, absoluteAddress);
                value = (uint)((oldValue & ~mask.Value) | (value & mask.Value));
            }
            bool returnValue = WriteRam(BitConverter.GetBytes(value), (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(uint), mask, shift);
            return returnValue;
        }

        public bool SetValue(float value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            bool returnValue = WriteRam(BitConverter.GetBytes(value), (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(float), mask, shift);
            return returnValue;
        }

        public bool SetValue(double value, uint address, bool absoluteAddress = false, uint? mask = null, int? shift = null)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] bytes1 = bytes.Take(4).ToArray();
            byte[] bytes2 = bytes.Skip(4).Take(4).ToArray();
            byte[] bytesSwapped = bytes2.Concat(bytes1).ToArray();

            bool returnValue = WriteRam(bytesSwapped, (UIntPtr)address, EndiannessType.Little, absoluteAddress);
            if (returnValue && !Config.LockManager.IsInvokingLocks) Config.LockManager.UpdateMemoryLockValue(value, address, typeof(double), mask, shift);
            return returnValue;
        }

        public bool WriteRam(byte[] buffer, uint address, EndiannessType endianness,
           int bufferStart = 0, int? length = null, bool safeWrite = true)
        {
            return WriteRam(buffer, (UIntPtr)address, endianness, false, bufferStart, length, safeWrite);
        }

        public bool WriteRam(byte[] buffer, UIntPtr address, EndiannessType endianness, bool absoluteAddress = false, 
            int bufferStart = 0, int? length = null, bool safeWrite = true)
        {
            if (length == null)
                length = buffer.Length - bufferStart;

            if (CheckReadonlyOff())
                return false;

            byte[] writeBytes = new byte[length.Value];
            Array.Copy(buffer, bufferStart, writeBytes, 0, length.Value);

            // Attempt to pause the game before writing 
            bool preSuspended = _io?.IsSuspended ?? false;
            if (safeWrite)
                _io?.Suspend();

            if (EndiannessUtilities.DataIsMisaligned(address, length.Value, EndiannessType.Big))
                throw new Exception("Misaligned data");

            // Write memory to game/process
            bool result;
            if (absoluteAddress)
                result = _io?.WriteAbsolute(address, writeBytes, endianness) ?? false;
            else
                result = _io?.WriteRelative(address.ToUInt32(), writeBytes, endianness) ?? false;

            // Resume stream 
            if (safeWrite && !preSuspended)
                _io?.Resume();

            //FocusOnEmulator();
            return result;
        }

        public bool RefreshRam()
        {
            lock (_ram)
            {
                try
                {
                    // Read whole ram value to buffer
                    if (_ram.Length != _io?.RamSize)
                        _ram = new byte[_io.RamSize];

                    return _io?.ReadRelative(0, _ram, EndiannessType.Little) ?? false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private void ProcessUpdate()
        {
            Stopwatch frameStopwatch = Stopwatch.StartNew();

            while (!disposedValue)
            {
                try {
                    int timeToWait;
                    lock (_mStreamProcess)
                    {

                        frameStopwatch.Restart();
                        if ((!IsEnabled || !IsRunning) && !_lastUpdateBeforePausing)
                            goto FrameLimitStreamUpdate;

                        _lastUpdateBeforePausing = false;

                        if (!RefreshRam())
                            goto FrameLimitStreamUpdate;

                        OnUpdate?.Invoke(this, new EventArgs());

                        FrameLimitStreamUpdate:

                        // Calculate delay to match correct FPS
                        frameStopwatch.Stop();
                        timeToWait = (int)RefreshRateConfig.RefreshRateInterval - (int)frameStopwatch.ElapsedMilliseconds;
                        timeToWait = Math.Max(timeToWait, 0);

                        // Calculate Fps
                        if (_fpsTimes.Count() >= 10)
                        {
                            double garbage;
                            _fpsTimes.TryDequeue(out garbage);
                        }
                        _fpsTimes.Enqueue(frameStopwatch.ElapsedMilliseconds + timeToWait);
                        FpsUpdated?.Invoke(this, new EventArgs());
                    }

                    if (timeToWait > 0)
                        Thread.Sleep(timeToWait);
                    else
                        Thread.Yield();
                }
                catch (Exception)
                {
                    Monitor.Exit(_mStreamProcess);
                    Debugger.Break();
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_io != null)
                    {
                        _io.OnClose -= ProcessClosed;
                        (_io as IDisposable)?.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
