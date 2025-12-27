using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Cool1Windows.Services
{
    public static class ShortcutService
    {
        public static (string Path, string Arguments) ResolveShortcutDetailed(string shortcutPath)
        {
            if (string.IsNullOrEmpty(shortcutPath) || !File.Exists(shortcutPath)) return (shortcutPath, "");
            if (!shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) return (shortcutPath, "");

            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return (shortcutPath, "");

                dynamic shell = Activator.CreateInstance(shellType)!;
                var shortcut = shell.CreateShortcut(shortcutPath);
                string target = shortcut.TargetPath;
                string args = shortcut.Arguments;
                
                return (target ?? shortcutPath, args ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shortcut] Resolve Error: {ex.Message}");
            }
            return (shortcutPath, "");
        }

        public static string ResolveShortcut(string shortcutPath)
        {
            return ResolveShortcutDetailed(shortcutPath).Path;
        }
    }
}
