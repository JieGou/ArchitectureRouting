using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.AppBase.Model;
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

      HeightSettingModel heightSettingModel = new HeightSettingModel();
      // TODO get data of height setting from snoop DB


      var x = new HeightSettingDialog();
      x.HeightOfLv1.Text = heightSettingModel.HeightOfLevels[0].ToString() ?? "4000";
      x.HeightOfLv2.Text = heightSettingModel.HeightOfLevels[1].ToString() ?? "8000";
      x.HeightOfConectorsLv1.Text = heightSettingModel.HeightOfConnectorsByLevel[0].ToString() ?? "2000";
      x.HeightOfConectorsLv2.Text = heightSettingModel.HeightOfConnectorsByLevel[1].ToString() ?? "2000";
      x.ShowDialog();


      if (x.DialogResult ?? false)
      {
        double heightOfConector = double.Parse(x.HeightOfConectorsLv1.Text);

        return document.Transaction("TransactionName.Commands.Routing.HeightSetting", _ =>
        {
          heightSettingModel.HeightOfLevels[0] = double.Parse(x.HeightOfLv1.Text);
          heightSettingModel.HeightOfLevels[1] = double.Parse(x.HeightOfLv2.Text);
          heightSettingModel.HeightOfConnectorsByLevel[0] = double.Parse(x.HeightOfConectorsLv1.Text);
          heightSettingModel.HeightOfConnectorsByLevel[1] = double.Parse(x.HeightOfConectorsLv2.Text);

          // TODO handle for only 1 level
          ICollection<Element> connectors = new FilteredElementCollector(document).OfClass(typeof(FamilyInstance))
                                        .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                                        .ToElements();

          foreach (var item in connectors)
          {
            LocationPoint? lp = item.Location as LocationPoint;
            double newZ = heightSettingModel.HeightOfConnectorsByLevel[0] - lp!.Point.Z.RevitUnitsToMillimeters();
            var newLocation = new XYZ(0, 0, newZ.MillimetersToRevitUnits());
            item.Location.Move(newLocation);
          }
          return Result.Succeeded;
        });

      }



      return Result.Cancelled;
    }

  }
}