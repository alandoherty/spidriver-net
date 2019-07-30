using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpiDriver
{
    /// <summary>
    /// Provides functionality to write/read data to the SPI component as a stream.
    /// </summary>
    public class DataStream : Stream
    {
        private Device _device;

        /// <summary>
        /// Gets if this stream can read data.
        /// </summary>
        public override bool CanRead {
            get {
                return true;
            }
        }

        /// <summary>
        /// Gets if this stream can seek.
        /// </summary>
        public override bool CanSeek {
            get {
                return false;
            }
        }

        /// <summary>
        /// Gets if this stream can write data.
        /// </summary>
        public override bool CanWrite {
            get {
                return true;
            }
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override long Length {
            get {
                return 0;
            }
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override long Position {
            get {
                throw new NotImplementedException();
            } set {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Flushes the stream.
        /// </summary>
        public override void Flush() {
        }

        /// <summary>
        /// Reads data from the SPI device.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset in the byte array to begin reading.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            return _device.Read(buffer, offset, count);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes the byte array to the SPI device.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public override void Write(byte[] buffer, int offset, int count) {
            _device.Write(buffer, offset, count);
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
            return _device.ReadWrite(inBytes, inOffset, outBytes, outOffset, count);
        }

        /// <summary>
        /// Creates a new data stream.
        /// </summary>
        /// <param name="device">The device.</param>
        internal DataStream(Device device) {
            _device = device;
        }
    }
}
