using System;
using System.IO;
using System.Reflection;

namespace ClientPlugin
{
    internal static class AssemblyResolver
    {
        private const string PrivateDependencyFolder = "libs";
        private static bool registered;

        public static void Register()
        {
            if (registered)
                return;

            registered = true;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromPluginFolder;
        }

        private static Assembly ResolveFromPluginFolder(object sender, ResolveEventArgs args)
        {
            var requestedName = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(requestedName))
                return null;

            var pluginPath = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrWhiteSpace(pluginPath))
                return null;

            var pluginDirectory = Path.GetDirectoryName(pluginPath);
            if (string.IsNullOrWhiteSpace(pluginDirectory))
                return null;

            var besidePlugin = Path.Combine(pluginDirectory, requestedName + ".dll");
            if (File.Exists(besidePlugin))
                return Assembly.LoadFrom(besidePlugin);

            var privatePath = Path.Combine(pluginDirectory, PrivateDependencyFolder, requestedName + ".dll");
            if (File.Exists(privatePath))
                return Assembly.LoadFrom(privatePath);

            return null;
        }
    }
}
