using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using EnvDTE;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;

namespace MadsKristensen.ShortcutExporter
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidShortcutExporterPkgString)]
    public sealed class ShortcutExporterPackage : Package
    {
        private DTE2 _dte;

        protected override void Initialize()
        {
            base.Initialize();

            _dte = GetGlobalService(typeof(DTE)) as DTE2;

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                var menuCommandID = new CommandID(GuidList.guidShortcutExporterCmdSet, (int)PkgCmdIDList.cmdExportShortcuts);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                var menuCommandIDJson = new CommandID(GuidList.guidShortcutExporterCmdSet, (int)PkgCmdIDList.cmdExportShortcutsJSON);
                var menuItemJson = new MenuCommand(MenuItemJsonCallback, menuCommandIDJson);
                mcs.AddCommand(menuItemJson);
            }
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".xml",
                FileName = GetVersion() + ".xml",
                CheckPathExists = true,
                Filter = @"XML Files (*.xml)|*.xml|All files (*.*)|*.*",
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                WriteDocument(dialog.FileName);
            }
        }

        private void MenuItemJsonCallback(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".json",
                FileName = GetVersion() + ".json",
                CheckPathExists = true,
                Filter = @"Json Files (*.json)|*.json|All files (*.*)|*.*",
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                WriteJsonDocument(dialog.FileName);
            }
        }

        private string GetVersion()
        {
            switch (_dte.Version)
            {
                case "11.0":
                    return "2012";
                case "12.0":
                    return "2013";
                case "14.0":
                    return "2015";
                case "15.0":
                    return "2017";
            }

            return "2013";
        }

        private void WriteDocument(string fileName)
        {
            using (XmlWriter writer = XmlWriter.Create(fileName))
            {
                writer.WriteStartDocument(true);
                writer.WriteStartElement("commands");

                WriteCommands(writer);

                writer.WriteEndElement();
            }
        }

        private void WriteJsonDocument(string fileName)
        {
            var data = new List<JsonData>();

            IEnumerable<Command> commands = GetCommands();
            foreach (var command in commands.OrderBy(c => c.Name))
            {
                var bindings = command.Bindings as object[];

                if (bindings != null && bindings.Length > 0)
                {
                    var shortcuts = GetBindings(bindings);

                    data.AddRange(shortcuts.Select(shortcut => new JsonData
                    {
                        Name = command.Name,
                        Shortcut = shortcut
                    }));
                }
            }

            var json = JsonConvert.SerializeObject(data.ToArray());
            System.IO.File.WriteAllText(fileName,json);
        }

        

        private void WriteCommands(XmlWriter writer)
        {
            IEnumerable<Command> commands = GetCommands();

            foreach (var command in commands.OrderBy(c => c.Name))
            {
                var bindings = command.Bindings as object[];

                if (bindings != null && bindings.Length > 0)
                {
                    var shortcuts = GetBindings(bindings);

                    foreach (string shortcut in shortcuts)
                    {
                        writer.WriteStartElement("command");
                        writer.WriteAttributeString("shortcut", shortcut);
                        writer.WriteAttributeString("name", command.Name);
                        writer.WriteEndElement();
                    }
                }
            }
        }

        private IEnumerable<Command> GetCommands()
        {
            return _dte.Commands.Cast<Command>().Where(command => !string.IsNullOrEmpty(command.Name)).ToList();
        }

        private static IEnumerable<string> GetBindings(IEnumerable<object> bindings)
        {
            var result = bindings.Select(binding => binding.ToString().IndexOf("::", StringComparison.Ordinal) >= 0
                ? binding.ToString().Substring(binding.ToString().IndexOf("::", StringComparison.Ordinal) + 2)
                : binding.ToString()).Distinct();


            return result;
        }
    }

    public class JsonData
    {
        public string Shortcut { get; set; }
        public string Name { get; set; }
    }
}
