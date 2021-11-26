﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Threading;
using System.Windows.Forms.VisualStyles;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Arent3d.Revit.UI;
using Arent3d.Revit.UI.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

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
                    DataProcessBeforeSave(cnsStorables);
                    if (ShouldSaveCnsList(document, cnsStorables))
                    {
                        var tokenSource = new CancellationTokenSource();
                        using var progress = ProgressBar.ShowWithNewThread(tokenSource);
                        progress.Message = "Saving CNS Setting...";
                        using (progress?.Reserve(0.5))
                        {
                            SaveCnsList(document, cnsStorables);
                        }
                    }

                    if (!string.IsNullOrEmpty(viewModel.ApplyToSymbolsText))
                    {
                        var selectedElements = UiDocument.Selection.PickObjects(ObjectType.Element);
                        foreach (var item in selectedElements)
                        {
                            var element = document.GetElement(item.ElementId);
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

        private static void DataProcessBeforeSave(CnsSettingStorable cnsSettings)
        {
            bool hadUpdating = false;
            // Remove empty row
            foreach (var item in cnsSettings.CnsSettingData.ToList())
            {
                if (string.IsNullOrWhiteSpace(item.CategoryName.Trim()))
                {
                    cnsSettings.CnsSettingData.Remove(item);
                    hadUpdating = true;
                }
            }

            if (cnsSettings.CnsSettingData.Count == 0)
            {
                // Add default value if list empty
                cnsSettings.CnsSettingData.Add(new CnsSettingModel(sequence: 1, categoryName: "未設定"));
            }
            else if (hadUpdating)
            {
                // Set sequence if list was changed
                for (int i = 0; i < cnsSettings.CnsSettingData.Count; i++)
                {
                    cnsSettings.CnsSettingData[i].Sequence = i + 1;
                }
            }
        }
    }
}