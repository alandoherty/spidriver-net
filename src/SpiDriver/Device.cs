using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpiDriver
{
    /// <summary>
    /// Provides functionality to communicate with SPIDriver devices.
    /// </summary>
    public class Device
    {
        internal SerialPort _port;
        private SemaphoreSlim _portSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets or sets the read timeout, the default is infinite.
        /// </summary>
        public TimeSpan ReadTimeout {
            get {
                return TimeSpan.FromMilliseconds(_port.ReadTimeout);
            } set {
                _port.ReadTimeout = (int)value.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Gets or sets the write timeout, the default is infinite.
        /// </summary>
        public TimeSpan WriteTimeout {
            get {
                return TimeSpan.FromMilliseconds(_port.WriteTimeout);
            }
            set {
                _port.WriteTimeout = (int)value.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Gets the serial port, direct access is not thread safe.
        /// </summary>
        public SerialPort Port {
            get {
                return _port;
            }
        }

        /// <summary>
        /// Gets if the connection to the device is open.
        /// </summary>
        public bool IsOpen {
            get {
                return _port.IsOpen;
            }
        }

        private void ReadWire(byte[] buffer, int offset, int count) {
            int dataRemaining = count;

            while (dataRemaining > 0) {
                int dataRead = _port.Read(buffer, count - dataRemaining, dataRemaining);
                dataRemaining -= dataRead;
            }
        }

        /// <summary>
        /// Closes the device.
        /// </summary>
        public void Close() {
            _port.Close();
        }

        /// <summary>
        /// Gets a <see cref="Stream"/> which provides read/write access to the underlying SPI data stream.
        /// </summary>
        /// <returns></returns>
        public Stream GetStream() {
            return new DataStream(this);
        }

        /// <summary>
        /// Gets the current output state by querying the device status.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <returns>The output value.</returns>
        public bool GetOutput(Output output) {
            DeviceStatus status = GetStatus();

            if (output == Output.A)
                return status.A;
            else if (output == Output.B)
                return status.B;
            else if (output == Output.CS)
                return status.ChipSelect;
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the current output state by querying the device status asynchronously.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <returns>The output value.</returns>
        public async Task<bool> GetOutputAsync(Output output) {
            DeviceStatus status = await GetStatusAsync().ConfigureAwait(false);

            if (output == Output.A)
                return status.A;
            else if (output == Output.B)
                return status.B;
            else if (output == Output.CS)
                return status.ChipSelect;
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the status of the device.
        /// </summary>
        /// <returns>The status.</returns>
        public DeviceStatus GetStatus() {
            if (!_port.IsOpen)
                throw new InvalidOperationException("The serial port is not open");

            _portSemaphore.Wait();

            try {
                // send request
                _port.Write(new byte[] { (byte)'?' }, 0, 1);

                // get response
                byte[] data = new byte[80];
                ReadWire(data, 0, 80);

                // convert to string
                string dataStr = Encoding.ASCII.GetString(data)
                    .TrimStart('[')
                    .TrimEnd(']');

                // split components
                string[] dataComponents = dataStr.Split(' ');

                // fill status object
                DeviceStatus status = new DeviceStatus();
                status.Model = dataComponents[0];
                status.Serial = dataComponents[1];
                status.Uptime = TimeSpan.FromSeconds((double)ulong.Parse(dataComponents[2]));
                status.Voltage = float.Parse(dataComponents[3]);
                status.Current = float.Parse(dataComponents[4]);
                status.Temperature = float.Parse(dataComponents[5]);
                status.A = int.Parse(dataComponents[6]) == 1;
                status.B = int.Parse(dataComponents[7]) == 1;
                status.ChipSelect = int.Parse(dataComponents[8]) == 1;
                status.Crc = ushort.Parse(dataComponents[6], NumberStyles.HexNumber);

                return status;
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets the status of the device asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<DeviceStatus> GetStatusAsync() {
            if (!_port.IsOpen)
                throw new InvalidOperationException("The serial port is not open");

            await _portSemaphore.WaitAsync().ConfigureAwait(false);

            try {
                return await Task.Run(() => {
                    // send request
                    _port.Write(new byte[] { (byte)'?' }, 0, 1);

                    // get response
                    byte[] data = new byte[80];
                    ReadWire(data, 0, 80);

                    // convert to string
                    string dataStr = Encoding.ASCII.GetString(data)
                        .TrimStart('[')
                        .TrimEnd(']');

                    // split components
                    string[] dataComponents = dataStr.Split(' ');

                    // fill status object
                    DeviceStatus status = new DeviceStatus();
                    status.Model = dataComponents[0];
                    status.Serial = dataComponents[1];
                    status.Uptime = TimeSpan.FromSeconds((double)ulong.Parse(dataComponents[2]));
                    status.Voltage = float.Parse(dataComponents[3]);
                    status.Current = float.Parse(dataComponents[4]);
                    status.Temperature = float.Parse(dataComponents[5]);
                    status.A = int.Parse(dataComponents[6]) == 1;
                    status.B = int.Parse(dataComponents[7]) == 1;
                    status.ChipSelect = int.Parse(dataComponents[8]) == 1;
                    status.Crc = ushort.Parse(dataComponents[6], NumberStyles.HexNumber);

                    return status;
                }).ConfigureAwait(false);
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Set the specified output pin on the device.
        /// </summary>
        /// <param name="output">The output pin.</param>
        /// <param name="enable">The enable.</param>
        public void SetOutput(Output output, bool enable) {
            if (!_port.IsOpen)
                throw new InvalidOperationException("The serial port is not open");

            _portSemaphore.Wait();

            try {
                if (output == Output.A) {
                    _port.Write(new byte[] { (byte)'a', enable ? (byte)1 : (byte)0 }, 0, 2);
                } else if (output == Output.B) {
                    _port.Write(new byte[] { (byte)'b', enable ? (byte)1 : (byte)0 }, 0, 2);
                } else if (output == Output.CS) {
                    _port.Write(new byte[] { enable ? (byte)'s' : (byte)'u'}, 0, 2);
                } else {
                    throw new NotImplementedException();
                }
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Set the specified output pin on the device asynchronously.
        /// </summary>
        /// <param name="output">The output pin.</param>
        /// <param name="enable">The enable.</param>
        public async Task SetOutputAsync(Output output, bool enable) {
            if (!_port.IsOpen)
                throw new InvalidOperationException("The serial port is not open");

            await _portSemaphore.WaitAsync().ConfigureAwait(false);

            try {
                await Task.Run(() => {
                    if (output == Output.A) {
                        _port.Write(new byte[] { (byte)'a', enable ? (byte)1 : (byte)0 }, 0, 2);
                    } else if (output == Output.B) {
                        _port.Write(new byte[] { (byte)'b', enable ? (byte)1 : (byte)0 }, 0, 2);
                    } else if (output == Output.CS) {
                        _port.Write(new byte[] { enable ? (byte)'s' : (byte)'u' }, 0, 2);
                    } else {
                        throw new NotImplementedException();
                    }
                }).ConfigureAwait(false);
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Connects to the serial port and performs connection tests.
        /// </summary>
        public void Connect() {
            _portSemaphore.Wait();

            try {
                if (!_port.IsOpen)
                    _port.Open();

                // write init message
                _port.Write(Encoding.ASCII.GetBytes(new string('@', 64)), 0, 64);

                // write tests
                byte[] tests = new byte[] { (byte)'A', (byte)'\r', (byte)'\n', 0xFF };
                byte[] writeCmd = new byte[] { (byte)'e', 0 };
                byte[] readCmd = new byte[] { 0 };

                for (int i = 0; i < 4; i++) {
                    writeCmd[1] = tests[i];

                    // write test byte
                    _port.Write(writeCmd, 0, writeCmd.Length);

                    // read test byte
                    ReadWire(readCmd, 0, 1);

                    if (readCmd[0] != tests[i])
                        throw new Exception("Response invalid during connection test");
                }
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Connects to the serial port and performs connection tests asynchronously.
        /// </summary>
        public async Task ConnectAsync() {
            await _portSemaphore.WaitAsync().ConfigureAwait(false);

            try {
                await Task.Run(() => {
                    if (!_port.IsOpen)
                        _port.Open();

                    // write init message
                    _port.Write(Encoding.ASCII.GetBytes(new string('@', 64)), 0, 64);

                    // write tests
                    byte[] tests = new byte[] { (byte)'A', (byte)'\r', (byte)'\n', 0xFF };
                    byte[] writeCmd = new byte[] { (byte)'e', 0 };
                    byte[] readCmd = new byte[] { 0 };

                    for (int i = 0; i < 4; i++) {
                        writeCmd[1] = tests[i];

                        // write test byte
                        _port.Write(writeCmd, 0, writeCmd.Length);

                        // read test byte
                        ReadWire(readCmd, 0, 1);

                        if (readCmd[0] != tests[i])
                            throw new Exception("Response invalid during connection test");
                    }
                }).ConfigureAwait(false);
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Writes data to the SPI device.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <param name="offset">The offset in the byte array to begin writing.</param>
        /// <param name="count">The number of bytes to write.</param>
        public void Write(byte[] bytes, int offset, int count) {
            if (count == 0)
                return;

            _portSemaphore.Wait();

            try {
                byte[] cmd = new byte[65];

                for (int i = 0; i < count; i += 64) {
                    int len = ((count - i) < 64) ? (count - i) : 64;

                    for (int j = 0; j < cmd.Length; j++) {
                        cmd[j] = (byte)(0xC0 + len - 1);
                    }

                    Buffer.BlockCopy(bytes, offset + i, cmd, 1, len);
                    _port.Write(cmd, 0, 1 + len);
                }
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Writes data to the SPI device asynchronously.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <param name="offset">The offset in the byte array to begin writing.</param>
        /// <param name="count">The number of bytes to write.</param>
        public async Task WriteAsync(byte[] bytes, int offset, int count) {
            if (count == 0)
                return;

            await _portSemaphore.WaitAsync().ConfigureAwait(false);

            try {
                await Task.Run(() => {
                    byte[] cmd = new byte[65];

                    for (int i = 0; i < count; i += 64) {
                        int len = ((count - i) < 64) ? (count - i) : 64;

                        for (int j = 0; j < cmd.Length; j++) {
                            cmd[j] = (byte)(0xC0 + len - 1);
                        }

                        Buffer.BlockCopy(bytes, offset + i, cmd, 1, len);
                        _port.Write(cmd, 0, 1 + len);
                    }
                }).ConfigureAwait(false);
            } finally {
                _portSemaphore.Release();
            }
        }

        /// <summary>
        /// Reads data from the SPI device.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <param name="offset">The offset in the byte array to begin reading.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public int Read(byte[] bytes, int offset, int count) {
            if (count == 0)
                return 0;

            _portSemaphore.Wait();

            try { 
                byte[] cmd = new byte[65];

                for (int i = 0; i < count; i += 64) {
                    int len = ((count - i) < 64) ? (count - i) : 64;
                    cmd[0] = (byte)(0x80 + len - 1);
                    _port.Write(cmd, 0, len + 1);
                    ReadWire(bytes, i + offset, len);
                }
            } finally {
                _portSemaphore.Release();
            }

            return count;
        }

        /// <summary>
        /// Reads data from the SPI device.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <param name="offset">The offset in the byte array to begin reading.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public async Task<int> ReadAsync(byte[] bytes, int offset, int count) {
            if (count == 0)
                return 0;

            await _portSemaphore.WaitAsync().ConfigureAwait(false);

            try {
                await Task.Run(() => {
                    byte[] cmd = new byte[65];

                    for (int i = 0; i < count; i += 64) {
                        int len = ((count - i) < 64) ? (count - i) : 64;
                        cmd[0] = (byte)(0x80 + len - 1);
                        _port.Write(cmd, 0, len + 1);
                        ReadWire(bytes, i + offset, len);
                    }
                }).ConfigureAwait(false);
            } finally {
                _portSemaphore.Release();
            }

            return count;
        }

        /// <summary>
        /// Reads and writes data to and from the SPI device.
        /// </summary>
        /// <param name="inBytes">The input byte array.</param>
        /// <param name="inOffset">The offset in the byte array to begin reading.</param>
        /// <param name="outBytes">The output byte array.</param>
        /// <param name="outOffset">The offset in the byte array to begin writing.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes written and read.</returns>
        public int ReadWrite(byte[] inBytes, int inOffset, byte[] outBytes, int outOffset, int count) {
            if (count == 0)
                return 0;

            _portSemaphore.Wait();

            try {
                byte[] cmd = new byte[65];

                for (int i = 0; i < count; i += 64) {
                    int len = ((count - i) < 64) ? (count - i) : 64;
                    cmd[0] = (byte)(0x80 + len - 1);
                    Buffer.BlockCopy(outBytes, outOffset + i, cmd, 1, len);
                    _port.Write(cmd, 0, len + 1);
                    ReadWire(inBytes, i + inOffset, len);
                }
            } finally {
                _portSemaphore.Release();
            }

            return count;
        }

        /// <summary>
        /// Reads and writes data to and from the SPI device.
        /// </summary>
        /// <param name="inBytes">The input byte array.</param>
        /// <param name="inOffset">The offset in the byte array to begin reading.</param>
        /// <param name="outBytes">The output byte array.</param>
        /// <param name="outOffset">The offset in the byte array to begin writing.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes written and read.</returns>
        public async Task<int> ReadWriteAsync(byte[] inBytes, int inOffset, byte[] outBytes, int outOffset, int count) {
            if (count == 0)
                return 0;

            await _portSemaphore.WaitAsync().ConfigureAwait(false);

            try {
                await Task.Run(() => {
                    byte[] cmd = new byte[65];

                    for (int i = 0; i < count; i += 64) {
                        int len = ((count - i) < 64) ? (count - i) : 64;
                        cmd[0] = (byte)(0x80 + len - 1);
                        Buffer.BlockCopy(outBytes, outOffset + i, cmd, 1, len);
                        _port.Write(cmd, 0, len + 1);
                        ReadWire(inBytes, i + inOffset, len);
                    }
                }).ConfigureAwait(false);
            } finally {
                _portSemaphore.Release();
            }

            return count;
        }

        /// <summary>
        /// Creates a new device with the specified serial port. The port must be configured to a baud rate of 460800, parity none, and stop bits one.
        /// </summary>
        /// <param name="port">The port.</param>
        public Device(SerialPort port) {
            _port = port;
        }

        /// <summary>
        /// Creates a new device with the specified serial port.
        /// </summary>
        /// <param name="portName">The port name.</param>
        public Device(string portName) {
            _port = new SerialPort(portName, 460800, Parity.None, 8, StopBits.One);
        }
    }
}
