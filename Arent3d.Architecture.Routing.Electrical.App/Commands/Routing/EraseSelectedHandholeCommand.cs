﻿using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.EraseSelectedHandholeCommand", DefaultString = "Delete\nHandhole" )]
  [Image( "resources/DeleteFrom-To.png" )]
  public class EraseSelectedHandholeCommand : EraseSelectedPullBoxCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.EraseSelectedHandhole" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.Handhole ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override ISelectionFilter GetSelectionFilter() => new HandholePickFilter() ;

    private class HandholePickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is FamilyInstance familyInstance && familyInstance.GetConnectorFamilyType() == ConnectorFamilyType.Handhole ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true ;
      }
    }
  }
}