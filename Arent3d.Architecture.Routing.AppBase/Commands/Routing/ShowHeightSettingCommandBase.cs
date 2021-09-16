using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Revit;
using Arent3d.Revit.UI;
using Arent3d.Revit.UI.Forms;
using Arent3d.Utility;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ShowHeightSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UIDocument uIDocument = commandData.Application.ActiveUIDocument;
      Document document = uIDocument.Document;

      // get data of height setting from snoop DB
      HeightSettingStorable settingStorables = document.GetAllStorables<HeightSettingStorable>()
                                                       .AsEnumerable()
                                                       .DefaultIfEmpty(new HeightSettingStorable(document))
                                                       .First();


      var viewModel = new ViewModel.HeightSettingViewModel(settingStorables);
      var dialog = new HeightSettingDialog(viewModel);

      dialog.ShowDialog();

      if (dialog.DialogResult ?? false)
      {
        var tokenSource = new CancellationTokenSource();
        using var progress = ProgressBar.ShowWithNewThread(tokenSource);
        progress.Message = "Height Setting...";

        var newStorage = viewModel.SettingStorable;
        using (progress?.Reserve(0.5))
        {
          var resultSaving = SaveSetting(document, settingStorables);
          if (resultSaving == Result.Failed || resultSaving == Result.Cancelled)
          {
            return resultSaving;
          }
        }
        

        return ApplySetting(document, newStorage, progress?.Reserve(0.5));
      }
      else
      {
        return Result.Cancelled;
      }

    }

    public Result ApplySetting( Document document, HeightSettingStorable settingStorables, IProgressData? progressData = null )
    {
      return document.Transaction("TransactionName.Commands.Routing.HeightSetting.ApplyingSettings", _ =>
      {
        if (settingStorables == null)
          return Result.Failed;

        var allConnectors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance))
                                                               .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                                                               .AsEnumerable();
        var connectors = allConnectors.GroupBy(x => x.LevelId).ToDictionary(g => g.Key, g => g.ToList());


        var allConduits = new FilteredElementCollector(document).OfClass(typeof(Conduit))
                                                             .OfCategory(BuiltInCategory.OST_Conduit)
                                                             .AsEnumerable()
                                                             .OfType<Conduit>();
        var conduits = allConduits.GroupBy(x => x.ReferenceLevel.Id).ToDictionary(g => g.Key, g => g.ToList());

        var totalProgress = 0.00 + allConnectors.Count() + allConduits.Count();
        totalProgress = totalProgress == 0 ? 1.00 : totalProgress;

        foreach (Level level in settingStorables.Levels)
        {
          var heightConnector = settingStorables[level].HeightOfConnectors.MillimetersToRevitUnits();
          // Set Elevation from floor for all connector on this floor
          if (connectors.ContainsKey(level.Id))
          {
            using (var p = progressData?.Reserve(connectors[level.Id].Count / totalProgress))
            {
              p.ForEach(connectors[level.Id].Count, connectors[level.Id], connector =>
              {
                var family = (connector as FamilyInstance)!;
                var elevationFromFloor = family.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                if (elevationFromFloor != heightConnector)
                {
                  connector.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(heightConnector);
                }
              });
            }
          }
          // Set Top Elevation for conduit
          if (conduits.ContainsKey(level.Id))
          {
            using (var p = progressData?.Reserve(conduits[level.Id].Count / totalProgress))
            {
              p.ForEach(conduits[level.Id].Count, conduits[level.Id], conduit =>
              {
                var elevationFromFloor = conduit.get_Parameter(BuiltInParameter.RBS_CTC_BOTTOM_ELEVATION).AsDouble();
                if (elevationFromFloor != heightConnector)
                {
                  conduit.get_Parameter(BuiltInParameter.RBS_CTC_BOTTOM_ELEVATION).Set(heightConnector);
                }
              });
            }
          }

          // Set Elevation for level
          if (level.Elevation != settingStorables[level].Elevation.MillimetersToRevitUnits())
          {
            level.Elevation = settingStorables[level].Elevation.MillimetersToRevitUnits();
          }
        }

        return Result.Succeeded;
      });

    }

    public Result SaveSetting( Document document, HeightSettingStorable settingStorables )
    {
      var old = document.GetAllStorables<HeightSettingStorable>()
                                                   .AsEnumerable()
                                                   .DefaultIfEmpty(new HeightSettingStorable(document))
                                                   .First();
      var result = Result.Failed;
      if (!settingStorables.Equals(old))
      {
        result = document.Transaction("TransactionName.Commands.Routing.HeightSetting.SavingSettings", _ =>
        {
          settingStorables.Save();
          return Result.Succeeded;
        });
      }

      return result;

    }
  }
}