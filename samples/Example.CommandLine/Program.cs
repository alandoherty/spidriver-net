using SpiDriver;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Example.CommandLine
{
    class Program
    {
        static void Main(string[] argv) {
            // the device
            Device device = null;

            // loop through accepting commands
            string line;

            while (true) {
                // read next line
                Console.Write("> ");
                line = Console.ReadLine().Trim();

                // parse the line
                string[] components = line.Split(' ');
                string[] args = components.Length == 0 ? new string[0] : new string[components.Length - 1];
                string cmd = components.Length == 0 ? null : components[0];

                if (args.Length > 0)
                    Array.Copy(components, 1, args, 0, args.Length);

                // ignore empty lines
                if (cmd == null)
                    continue;

                // process command
                if (cmd.Equals("connect", StringComparison.CurrentCultureIgnoreCase)) {
                    if (args.Length == 0) {
                        Console.WriteLine("connect <port>");
                        continue;
                    }

                    try {
                        device = new Device(args[0]);
                        device.Connect();
                        Console.WriteLine("Connected successfully");
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                    }
                } else if (cmd.Equals("a", StringComparison.CurrentCultureIgnoreCase) || cmd.Equals("b", StringComparison.CurrentCultureIgnoreCase)
                    || cmd.Equals("cs", StringComparison.CurrentCultureIgnoreCase)) {
                    // check device is connected
                    if (device == null) {
                        Console.WriteLine("Not connected to device");
                        continue;
                    }

                    // determine output
                    Output output = Output.A;

                    if (cmd.Equals("b", StringComparison.CurrentCultureIgnoreCase))
                        output = Output.B;
                    else if (cmd.Equals("cs", StringComparison.CurrentCultureIgnoreCase))
                        output = Output.CS;

                    try {
                        if (args.Length == 0) {
                            bool value = device.GetOutput(output);
                            device.SetOutput(output, !value);
                            Console.WriteLine($"{output}: {!value}");
                        } else {
                            if (args[0].Equals("yes", StringComparison.CurrentCultureIgnoreCase) || args[0].Equals("on", StringComparison.CurrentCultureIgnoreCase)) {
                                device.SetOutput(output, true);
                            } else {
                                device.SetOutput(output, false);
                            }
                        }
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                    }
                } else if (cmd.Equals("status", StringComparison.CurrentCultureIgnoreCase)) {
                    if (device == null) {
                        Console.WriteLine("Not connected to device");
                        continue;
                    }

                    try {
                        DeviceStatus status = device.GetStatus();

                        Console.WriteLine($"Model: {status.Model}");
                        Console.WriteLine($"Serial Number: {status.Serial}");
                        Console.WriteLine($"Uptime: {status.Uptime}");
                        Console.WriteLine($"Voltage: {status.Voltage}V");
                        Console.WriteLine($"Current: {status.Current}A");
                        Console.WriteLine($"Temperature: {status.Temperature}°C");
                        Console.WriteLine($"A: {status.A}");
                        Console.WriteLine($"B: {status.B}");
                        Console.WriteLine($"CS: {status.ChipSelect}");
                        Console.WriteLine($"CRC: 0x{status.Crc.ToString("x4")}");
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                    }
                } else if (cmd.Equals("writef", StringComparison.CurrentCultureIgnoreCase)) {
                    string path = string.Join(' ', args);

                    if (device == null) {
                        Console.WriteLine("Not connected to device");
                        continue;
                    } else if (args.Length == 0) {
                        Console.WriteLine("No data in arguments");
                        continue;
                    } else if (!File.Exists(path)) {
                        Console.WriteLine("File does not exist");
                        continue;
                    }

                    // create stopwatch to time transfer
                    Stopwatch stopwatch = new Stopwatch();

                    try {
                        byte[] fb = File.ReadAllBytes(path);

                        stopwatch.Start();
                        device.Write(fb, 0, fb.Length);

                        Console.WriteLine($"Wrote {fb.Length} bytes in {Math.Round(stopwatch.Elapsed.TotalSeconds, 3)}s");
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                    }
                } else if (cmd.Equals("read", StringComparison.CurrentCultureIgnoreCase)) {
                    int count = 0;

                    if (device == null) {
                        Console.WriteLine("Not connected to device");
                        continue;
                    } else if (args.Length == 0) {
                        Console.WriteLine("No data in arguments");
                        continue;
                    } else if (!int.TryParse(args[0], out count)) {
                        Console.WriteLine("Invalid count to read");
                        continue;
                    }

                    try {
                        byte[] data = new byte[count];
                        device.Read(data, 0, data.Length);

                        Console.WriteLine(BitConverter.ToString(data));
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                    }
                } else if (cmd.Equals("help", StringComparison.CurrentCultureIgnoreCase)) {
                    Console.WriteLine("connect <port> - connect to serial port");
                    Console.WriteLine("a [on/off] - toggle or set A output");
                    Console.WriteLine("b [on/off] - toggle or set B output");
                    Console.WriteLine("cs [on/off] - toggle or set CS output");
                    Console.WriteLine("status - get device status");
                    Console.WriteLine("writef <path> - write file to SPI");
                } else if (line != "quit" && line != "q" && line != "exit") {
                    return;
                }
            }
        }
    }
}
