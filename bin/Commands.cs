using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerWakeBot {
    public sealed class CommandModule : ModuleBase {
        private bool TestMac(string macAddress) {
            return Regex.IsMatch(macAddress, "^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
        }

        /// <summary>
        /// Tests a MAC address.
        /// </summary>
        [Command("testmac")]
        private async Task TestMacAsync(string macAddress) {
            if (TestMac(macAddress))
                await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` is valid!");
            else
                await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` is not valid.");
        }

        /// <summary>
        /// Adds a MAC address.
        /// </summary>
        [Command("addmac")]
        private async Task AddMacAsync(string name, string macAddress) {
            if (!TestMac(macAddress)) {
                await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` is not valid.");
                return;
            }

            // Get dictionary for User.
            var dict = new Dictionary<string, string>();
            Console.WriteLine("hmm");
            if (File.Exists(Context.User.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.User.Id.ToString() + Program.MacsFileName));

            // Add to dictionary.
            dict[name] = macAddress.ToUpperInvariant();

            // Save settings.
            File.WriteAllText(Context.User.Id.ToString() + Program.MacsFileName, JsonConvert.SerializeObject(dict));
            await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` added for `{name}`.");
        }

        /// <summary>
        /// Deletes a MAC address.
        /// </summary>
        [Command("delmac")]
        private async Task DelMacAsync(string name) {
            // Get dictionary for User.
            var dict = new Dictionary<string, string>();
            if (File.Exists(Context.User.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.User.Id.ToString() + Program.MacsFileName));

            // Delete from dictionary.
            dict.Remove(name);

            // Save settings.
            File.WriteAllText(Context.User.Id.ToString() + Program.MacsFileName, JsonConvert.SerializeObject(dict));
            await ReplyAsync($"MAC address for `{name}` is deleted.");
        }

        /// <summary>
        /// Gets a MAC address.
        /// </summary>
        [Command("getmac")]
        private async Task GetMacAsync(string name) {
            // Get dictionary for User.
            var dict = new Dictionary<string, string>();
            if (File.Exists(Context.User.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.User.Id.ToString() + Program.MacsFileName));

            // Get from dictionary.
            string macAddress;
            if (!dict.TryGetValue(name, out macAddress)) {
                await ReplyAsync($"No MAC exists for `{name}`.");
                return;
            }

            await ReplyAsync($"MAC address for `{name}` is `{macAddress.ToUpperInvariant()}`.");
        }

        /// <summary>
        /// Wakes a MAC address.
        /// </summary>
        [Command("wakemac")]
        private async Task WakeMacAsync(string name) {
            // Get dictionary for User.
            var dict = new Dictionary<string, string>();
            if (File.Exists(Context.User.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.User.Id.ToString() + Program.MacsFileName));

            // Get from dictionary.
            string macAddress;
            if (!dict.TryGetValue(name, out macAddress)) {
                await ReplyAsync($"No MAC exists for `{name}`.");
                return;
            }

            await ReplyAsync($"Waking up host `{macAddress.ToUpperInvariant()}`...");

            // Create UdpClient.
            var wolClient = new UdpClient(1900);

            // Create buffer.
            int counter = 0;
            var bytes = new byte[102];

            // First 6 bytes are 0xFF.
            for (int i = 0; i < 6; i++)
                bytes[counter++] = 0xFF;

            // Repeate MAC 16 times.
            for (int i = 0; i < 16; i++) {
                int z = 0;
                for (int j = 0; j < 6; j++) {
                    bytes[counter++] = byte.Parse(macAddress.Substring(z, 2), NumberStyles.HexNumber);
                    z += 3;
                }
            }

            // Print to console.
            int zz = 0;
            for (int i = 0; i < bytes.Length; i++) {
                Console.Write(bytes[i] + " ");
                zz++;
                if (zz >= 6) {
                    Console.WriteLine();
                    zz = 0;
                }
            }

            wolClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse(Program.BroadcastMac),1900));
            wolClient.Close();

        }
    }
}
