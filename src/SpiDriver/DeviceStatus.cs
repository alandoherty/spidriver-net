using System;
using System.Collections.Generic;
using System.Text;

namespace SpiDriver
{
    /// <summary>
    /// Represents status information for the driver.
    /// </summary>
    public class DeviceStatus
    {
        /// <summary>
        /// Gets the model.
        /// </summary>
        public string Model { get; internal set; }

        /// <summary>
        /// Gets the serial number.
        /// </summary>
        public string Serial { get; internal set; }

        /// <summary>
        /// Gets the uptime.
        /// </summary>
        public TimeSpan Uptime { get; internal set; }

        /// <summary>
        /// Gets the voltage of the USB line.
        /// </summary>
        public float Voltage { get; internal set; }

        /// <summary>
        /// Gets the current used by the target SPI device.
        /// </summary>
        public float Current { get; internal set; }

        /// <summary>
        /// Gets the device temperature.
        /// </summary>
        public float Temperature { get; internal set; }

        /// <summary>
        /// Gets the A state.
        /// </summary>
        public bool A { get; internal set; }

        /// <summary>
        /// Gets the B state.
        /// </summary>
        public bool B { get; internal set; }

        /// <summary>
        /// Gets the chip select state.
        /// </summary>
        public bool ChipSelect { get; internal set; }

        /// <summary>
        /// Gets thge current CRC value.
        /// </summary>
        public ushort Crc { get; internal set; }
    }
}
