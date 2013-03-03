using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace HWTokenLicenseChecker
{
    public static class Utilities
    {
        // Gets the %APPDATA% folder ...
        public readonly static String ApplicationDataDir = Path.Combine(
            Environment.GetEnvironmentVariable("APPDATA"),
            Path.GetFileNameWithoutExtension(Application.ExecutablePath));

        // Gets the TMP folder ...
        public readonly static String TempDir = Environment.GetEnvironmentVariable("TMP");
    }
}
