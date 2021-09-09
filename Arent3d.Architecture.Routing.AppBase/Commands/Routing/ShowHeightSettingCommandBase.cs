using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Revit;
using Arent3d.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ShowHeightSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UIDocument uIDocument = commandData.Application.ActiveUIDocument;
      Document document = uIDocument.Document;
      return document.Transaction("TransactionName.Commands.Routing.HeightSetting", _ =>
      {
        // TODO get data of height setting from snoop DB
        HeightSettingStorable settingStorables = document.GetAllStorables<HeightSettingStorable>()
                                                           .ToArray()
                                                           .DefaultIfEmpty(new HeightSettingStorable(document))
                                                           .First()
                                                           ;


        var dialog = new HeightSettingDialog();
        // Set default value for dialog
        dialog.ElevationOfLv1.Text = settingStorables.ElevationOfLevels[HeightSettingStorable.LEVEL1_NAME].ToString();
        dialog.ElevationOfLv2.Text = settingStorables.ElevationOfLevels[HeightSettingStorable.LEVEL2_NAME].ToString();

        dialog.HeightOfLv1.Text = settingStorables.HeightOfLevels[HeightSettingStorable.LEVEL1_NAME].ToString();
        dialog.HeightOfLv2.Text = settingStorables.HeightOfLevels[HeightSettingStorable.LEVEL2_NAME].ToString();

        dialog.HeightOfConectorsLv1.Text = settingStorables.HeightOfConnectorsByLevel[HeightSettingStorable.LEVEL1_NAME].ToString();
        dialog.HeightOfConectorsLv2.Text = settingStorables.HeightOfConnectorsByLevel[HeightSettingStorable.LEVEL2_NAME].ToString();

        dialog.ShowDialog();

        if (dialog.DialogResult ?? false)
        {
          double heightOfConector = double.Parse(dialog.HeightOfConectorsLv1.Text);

          settingStorables.ElevationOfLevels[HeightSettingStorable.LEVEL1_NAME] = double.Parse(dialog.ElevationOfLv1.Text);
          settingStorables.ElevationOfLevels[HeightSettingStorable.LEVEL2_NAME] = double.Parse(dialog.ElevationOfLv2.Text);

          settingStorables.HeightOfLevels[HeightSettingStorable.LEVEL1_NAME] = double.Parse(dialog.HeightOfLv1.Text);
          settingStorables.HeightOfLevels[HeightSettingStorable.LEVEL2_NAME] = double.Parse(dialog.HeightOfLv2.Text);

          settingStorables.HeightOfConnectorsByLevel[HeightSettingStorable.LEVEL1_NAME] = double.Parse(dialog.HeightOfConectorsLv1.Text);
          settingStorables.HeightOfConnectorsByLevel[HeightSettingStorable.LEVEL2_NAME] = double.Parse(dialog.HeightOfConectorsLv2.Text);

          settingStorables.Save();


          Dictionary<ElementId, List<Element>> connectors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance))
                                              .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                                              .ToElements()
                                              .GroupBy(x => x.LevelId)
                                              .ToDictionary(g => g.Key, g => g.ToList())
                                              ;


          ICollection<Element> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).ToElements();

          foreach (var lvElement in levels)
          {
            var lv = (Level)lvElement;

            // Set Elevation from floor for all connector on this floor
            if (settingStorables.HeightOfConnectorsByLevel.ContainsKey(lv.Name) && connectors.ContainsKey(lv.Id))
            {
              foreach (var item in connectors[lv.Id])
              {
                item.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM)
                    .Set(settingStorables.HeightOfConnectorsByLevel[lv.Name].MillimetersToRevitUnits());
              }
            }

            // Set Elevation for level
            if (settingStorables.HeightOfLevels.ContainsKey(lv.Name))
            {
              lv.Elevation = settingStorables.ElevationOfLevels[lv.Name].MillimetersToRevitUnits();
            }

          }
        }

        return Result.Succeeded;

      });
    }

  }
}