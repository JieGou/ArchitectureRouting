﻿using System ;
using System.Windows ;
using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB ;
using Arent3d.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class FromToWindow : RevitDialog
  {
    public ObservableCollection<FromToItems> FromToItemsList { get ; set ; }

        public UIDocument? uiDoc;

    public FromToWindow( UIDocument uiDoc, ObservableCollection<FromToItems> fromToItemsList ) : base( uiDoc )
    {
      InitializeComponent() ;
      FromToItemsList = fromToItemsList ;
      this.uiDoc = uiDoc;
    }

    public class FromToItems
    {
      public string? Id { get ; set ; }

      //From
      public IEndPoint? From { get ; set ; }
      public string? FromType => From?.TypeName ;
      public string? FromId => From?.Accept( EndPointFieldValues.IdGetter ) ;
      public string? FromSubId => From?.Accept( EndPointFieldValues.SubIdGetter ) ;

      //To
      public IEndPoint? To { get ; set ; }
      public string? ToType => To?.TypeName ;
      public string? ToId => To?.Accept( EndPointFieldValues.IdGetter ) ;
      public string? ToSubId => To?.Accept( EndPointFieldValues.SubIdGetter ) ;

      //Domain
      public string? Domain { get ; set ; }

      //SystemType
      public ObservableCollection<MEPSystemType>? SystemTypes { get ; set ; }
      public MEPSystemType? SystemType { get ; set ; }
      public int SystemTypeIndex { get ; set ; }

      //CurveType
      public ObservableCollection<MEPCurveType>? CurveTypes { get ; set ; }
      public MEPCurveType? CurveType { get ; set ; }
      public int CurveTypeIndex { get ; set ; }

      //Direct
      public bool? Direct { get ; set ; }

      //Diameters
      public string? Diameters { get ; set ; }

      //PassPoint
      public string? PassPoints { get ; set ; }
    }


    private void Dialog2Buttons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      uiDoc?.Application.PostCommand<Commands.Routing.FileRoutingCommand>();
      //TaskDialog.Show( "test", "Import" ) ;
    }

    private void Dialog2Buttons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      uiDoc?.Application.PostCommand<Commands.Routing.ExportRoutingCommand>();
      //TaskDialog.Show( "test", "Export" ) ;
    }

    private void Dialog3Buttons_OnOnOKClick( object sender, RoutedEventArgs e )
    {
      this.Close() ;
    }

    private void Dialog3Buttons_OnOnApplyClick( object sender, RoutedEventArgs e )
    {
      throw new NotImplementedException() ;
    }

    private void Dialog3Buttons_OnOnCancelClick( object sender, RoutedEventArgs e )
    {
      this.Close() ;
    }
  }
}