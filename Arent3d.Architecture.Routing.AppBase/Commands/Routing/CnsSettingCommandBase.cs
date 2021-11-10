using System.Linq;
using System.Threading;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Revit;
using Arent3d.Revit.UI;
using Arent3d.Revit.UI.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
    public abstract class CnsSettingCommandBase : IExternalCommand
    {
        protected UIDocument UiDocument { get; private set; } = null!;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UiDocument = commandData.Application.ActiveUIDocument;
            Document document = UiDocument.Document;

            // get data of Cns Category from snoop DB
            CnsSettingStorable cnsStorables = document.GetCnsSettingStorable();
            CnsSettingViewModel viewModel = new CnsSettingViewModel(cnsStorables);
            var dialog = new CnsSettingDialog(viewModel);

            dialog.ShowDialog();
            if (dialog.DialogResult ?? false)
            {
                return document.Transaction("TransactionName.Commands.Routing.CnsSetting", _ =>
                {
                    foreach (var item in cnsStorables.CnsSettingData.ToList())
                    {
                        if (string.IsNullOrWhiteSpace(item.CategoryName.Trim()))
                        {
                            cnsStorables.CnsSettingData.Remove(item);
                        }
                    }

                    if (ShouldSaveCnsList(document, cnsStorables))
                    {
                        var tokenSource = new CancellationTokenSource();
                        using var progress = ProgressBar.ShowWithNewThread(tokenSource);
                        progress.Message = "Importing CNS...";
                        using (progress?.Reserve(0.5))
                        {
                            SaveCnsList(document, cnsStorables);
                        }
                    }

                    return Result.Succeeded;
                });
            }
            else
            {
                return Result.Cancelled;
            }
        }

        private static void SaveCnsList(Document document, CnsSettingStorable list)
        {
            list.Save();
        }

        private static bool ShouldSaveCnsList(Document document, CnsSettingStorable newSettings)
        {
            var old = document.GetAllStorables<CnsSettingStorable>().FirstOrDefault(); // generates new instance from document
            return old == null || !newSettings.Equals(old);
        }
    }
}
