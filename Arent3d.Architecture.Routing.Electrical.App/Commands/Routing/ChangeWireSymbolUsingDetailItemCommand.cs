﻿using System ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ChangeWireSymbolUsingDetailItemCommand", DefaultString = "Location\nBy Detail" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class ChangeWireSymbolUsingDetailItemCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;

        if ( uiDocument.ActiveView is not ViewPlan ) {
          message = "Only active in the view plan!" ;
          return Result.Cancelled ;
        }
        
        var externalEventHandler = new ExternalEventHandler() ;
        var viewModel = new ChangeWireSymbolUsingDetailItemViewModel( uiDocument ) { ExternalEventHandler = externalEventHandler } ;
        externalEventHandler.ExternalEvent = ExternalEvent.Create( viewModel.ExternalEventHandler ) ;
        var view = new ChangeWireSymbolUsingDetailItemView { DataContext = viewModel } ;
        view.Show() ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }
  }
}