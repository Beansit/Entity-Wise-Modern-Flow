using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace CloudFlowTracker
{
    // Do not forget to update version number and author (company attribute) in AssemblyInfo.cs class
    // To generate Base64 string for Images below, you can use https://www.base64-image.de/
    [Export(typeof(IXrmToolBoxPlugin)),
        ExportMetadata("Name", "Entity Wise Modern Flow"),
        ExportMetadata("Description", "This tool can be used to list down all the power automate flows according to entity."),
        // Please specify the base64 content of a 32x32 pixels image
        ExportMetadata("SmallImageBase64", "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"),
        // Please specify the base64 content of a 80x80 pixels image
        ExportMetadata("BigImageBase64", "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"),
        ExportMetadata("BackgroundColor", "Lavender"),
        ExportMetadata("PrimaryFontColor", "Black"),
        ExportMetadata("SecondaryFontColor", "Gray")]
    public class MyPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new MyPluginControl();
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        public MyPlugin()
        {
            // If you have external assemblies that you need to load, uncomment the following to 
            // hook into the event that will fire when an Assembly fails to resolve
            // AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEventHandler);
        }

        /// <summary>
        /// Event fired by CLR when an assembly reference fails to load
        /// Assumes that related assemblies will be loaded from a subfolder named the same as the Plugin
        /// For example, a folder named Sample.XrmToolBox.MyPlugin 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly loadAssembly = null;
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            // base name of the assembly that failed to resolve
            var argName = args.Name.Substring(0, args.Name.IndexOf(","));

            // check to see if the failing assembly is one that we reference.
            List<AssemblyName> refAssemblies = currAssembly.GetReferencedAssemblies().ToList();
            var refAssembly = refAssemblies.Where(a => a.Name == argName).FirstOrDefault();

            // if the current unresolved assembly is referenced by our plugin, attempt to load
            if (refAssembly != null)
            {
                // load from the path to this plugin assembly, not host executable
                string dir = Path.GetDirectoryName(currAssembly.Location).ToLower();
                string folder = Path.GetFileNameWithoutExtension(currAssembly.Location);
                dir = Path.Combine(dir, folder);

                var assmbPath = Path.Combine(dir, $"{argName}.dll");

                if (File.Exists(assmbPath))
                {
                    loadAssembly = Assembly.LoadFrom(assmbPath);
                }
                else
                {
                    throw new FileNotFoundException($"Unable to locate dependency: {assmbPath}");
                }
            }

            return loadAssembly;
        }
    }
}