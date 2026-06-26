using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.IO;


namespace POE2P
{
    public static class VoicePlayer
    {
        public static void PlayGreeting()
        {
            string path = "greeting.wav";

            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                });
            }
        }
    }
}