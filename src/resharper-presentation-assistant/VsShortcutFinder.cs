using System;
using System.Linq;
using EnvDTE;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Threading;
using JetBrains.UI.ActionsRevised;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.Util;
using JetBrains.VsIntegration.Shell.ActionManagement;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class VsShortcutFinder
    {
        private readonly DTE dte;
        private readonly VsBindingsConverter bindingsConverter;
        private readonly IThreading threading;

        // TODO: Optional components for when VS isn't available
        public VsShortcutFinder(DTE optionalDte, VsBindingsConverter optionalBindingsConverter, 
                                IThreading threading)
        {
            dte = optionalDte;
            bindingsConverter = optionalBindingsConverter;
            this.threading = threading;
        }

        public ActionShortcut GetOverriddenShortcut(IActionDefWithId def)
        {
            if (dte == null || bindingsConverter == null || def.CommandId == null)
                return null;

            // Not sure if we're ever called on a non-UI thread, but better safe than sorry
            if (!threading.Dispatcher.CheckAccess())
                return null;

            PartCatalogueAttribute attribute;
            if (!TryGetAttribute<VsOverrideActionAttribute>(def, out attribute))
                return null;

            string commandId;
            if (!TryGetConstructorValue(attribute, 0, out commandId))
                return null;

            var cid = VsCommandIDConverter.ConvertFromInvariantString(commandId);
            var command = dte.Commands.Item(cid.Guid, cid.ID);
            if (command == null)
                return null;

            var binding = GetFirstBinding(command.Bindings);
            if (binding != null)
            {
                // Can use ShortcutUtil.BindingsToShortcut
                // But would need to globalize the string?
                var actionShortcut = bindingsConverter.ToActionShortcut(binding);
                if (actionShortcut.Second != null)
                    return actionShortcut.Second;
            }

            return null;
        }

        private static bool TryGetAttribute<T>(IActionDefWithId def, out PartCatalogueAttribute attribute)
            where T : Attribute
        {
            attribute = null;

            var attributes = def.Part.GetAttributes<T>();
            if (attributes != null)
            {
                attribute = attributes.FirstOrDefault();
                if (attribute != null)
                    return true;
            }
            return false;
        }

        private bool TryGetConstructorValue<T>(PartCatalogueAttribute attribute, int index, out T ctorValue)
            where T : class
        {
            ctorValue = default(T);


            var properties = attribute.GetProperties();
            if (properties == null || index >= properties.Count)
                return false;

            var property = properties.Skip(index - 1).Take(1).FirstOrDefault();
            if (property != null)
            {
                ctorValue = property.Value as T;
                if (ctorValue != null)
                    return true;
            }
            return false;
        }

        private string GetFirstBinding(object o)
        {
            var s = o as object[];
            if (s != null && s.Length > 0)
                return s[0] as string;

            return null;
        }
    }
}