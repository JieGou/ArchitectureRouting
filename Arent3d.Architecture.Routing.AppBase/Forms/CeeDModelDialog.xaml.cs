﻿using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MessageBox = System.Windows.MessageBox ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeeDModelDialog : Window
  {
    private readonly Document _document ;
    private CeedViewModel? _allCeeDModels ;
    private CeedViewModel? _ceeDModelUsed ;
    private string _ceeDModelNumberSearch ;
    private string _modelNumberSearch ;
    public string SelectedSetCode ;

    private void Row_DoubleClick( object sender, DataGridViewCellEventArgs e )
    {
      MessageBox.Show( e.RowIndex.ToString() ) ;
    }

    public CeeDModelDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _allCeeDModels = null ;
      _ceeDModelUsed = null ;
      _ceeDModelNumberSearch = string.Empty ;
      _modelNumberSearch = string.Empty ;
      SelectedSetCode = string.Empty ;

      var oldCeeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( oldCeeDStorable != null ) {
        LoadData( oldCeeDStorable ) ;
      }

      Style rowStyle = new Style( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( DataGridRow.MouseDoubleClickEvent, new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
      DtGrid.RowStyle = rowStyle ;
    }

    private void Row_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (CeedModel) DtGrid.SelectedValue ;
      SelectedSetCode = selectedItem.CeeDSetCode ;
      DialogResult = true ;
      Close() ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Reset( object sender, RoutedEventArgs e )
    {
      CmbCeeDModelNumbers.SelectedIndex = -1 ;
      CmbCeeDModelNumbers.Text = "" ;
      CmbModelNumbers.SelectedIndex = -1 ;
      CmbModelNumbers.Text = "" ;
      var ceeDViewModels = CbCodeIsUsed.IsChecked == true ? _ceeDModelUsed : _allCeeDModels ;
      if ( ceeDViewModels != null )
        LoadData( ceeDViewModels ) ;
    }

    private void CmbCeeDModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _ceeDModelNumberSearch = ! string.IsNullOrEmpty( CmbCeeDModelNumbers.Text ) ? CmbCeeDModelNumbers.Text : string.Empty ;
    }

    private void CmbModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _modelNumberSearch = ! string.IsNullOrEmpty( CmbModelNumbers.Text ) ? CmbModelNumbers.Text : string.Empty ;
    }

    private void Button_Search( object sender, RoutedEventArgs e )
    {
      if ( _allCeeDModels == null && _ceeDModelUsed == null ) return ;
      var ceeDViewModels = CbCodeIsUsed.IsChecked == true ? _ceeDModelUsed : _allCeeDModels ;
      if ( ceeDViewModels == null ) return ;
      if ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) && string.IsNullOrEmpty( _modelNumberSearch ) ) {
        this.DataContext = ceeDViewModels ;
      }
      else {
        List<CeedModel> ceeDModels = new List<CeedModel>() ;
        switch ( string.IsNullOrEmpty( _ceeDModelNumberSearch ) ) {
          case false when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) && c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
          case false when string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) ).ToList() ;
            break ;
          case true when ! string.IsNullOrEmpty( _modelNumberSearch ) :
            ceeDModels = ceeDViewModels.CeedModels.Where( c => c.ModelNumber.Contains( _modelNumberSearch ) ).ToList() ;
            break ;
        }

        var ceeDModelsSearch = new CeedViewModel( ceeDViewModels.CeedStorable, ceeDModels ) ;
        this.DataContext = ceeDModelsSearch ;
      }
    }

    private void Button_SymbolRegistration( object sender, RoutedEventArgs e )
    {
      var ceeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceeDStorable != null ) {
        OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx", Multiselect = false } ;
        string filePath = string.Empty ;
        if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
          filePath = openFileDialog.FileName ;
        }

        if ( string.IsNullOrEmpty( filePath ) ) return ;
        var modelNumberUsed = ExcelToModelConverter.GetModelNumberUsed( filePath ) ;
        if ( ! modelNumberUsed.Any() ) return ;
        List<CeedModel> ceeDModelUsed = new List<CeedModel>() ;
        foreach ( var modelNumber in modelNumberUsed ) {
          var ceeDModels = ceeDStorable.CeedModelData.Where( c => c.ModelNumber.Contains( modelNumber ) ).Distinct().ToList() ;
          ceeDModelUsed.AddRange( ceeDModels ) ;
        }

        ceeDModelUsed = ceeDModelUsed.Distinct().ToList() ;
        _ceeDModelUsed = new CeedViewModel( ceeDStorable, ceeDModelUsed ) ;
        LoadData( _ceeDModelUsed ) ;
        CbCodeIsUsed.Visibility = Visibility.Visible ;
        CbCodeIsUsed.IsChecked = true ;
        if ( _ceeDModelUsed == null || ! _ceeDModelUsed.CeedModels.Any() ) return ;
        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          ceeDStorable.CeedModelUsedData = _ceeDModelUsed.CeedModels ;
          ceeDStorable.Save() ;
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
      else {
        MessageBox.Show( "Please read csv.", "Message" ) ;
      }
    }

    private void Button_LoadData( object sender, RoutedEventArgs e )
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.xlsx)|*.xlsx", Multiselect = false } ;
      string filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }

      if ( string.IsNullOrEmpty( filePath ) ) return ;
      CeedStorable ceeDStorable = _document.GetCeeDStorable() ;
      {
        List<CeedModel> ceeDModelData = ExcelToModelConverter.GetAllCeeDModelNumber( filePath ) ;
        if ( ! ceeDModelData.Any() ) return ;
        ceeDStorable.CeedModelData = ceeDModelData ;
        ceeDStorable.CeedModelUsedData = new List<CeedModel>() ;
        LoadData( ceeDStorable ) ;
        CbCodeIsUsed.Visibility = Visibility.Hidden ;
        CbCodeIsUsed.IsChecked = false ;

        try {
          using Transaction t = new Transaction( _document, "Save data" ) ;
          t.Start() ;
          ceeDStorable.Save() ;
          t.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        }
      }
    }

    private void LoadData( CeedStorable ceeDStorable )
    {
      var ceeDModelData = ceeDStorable.CeedModelUsedData.Any() ? ceeDStorable.CeedModelUsedData : ceeDStorable.CeedModelData ;
      if ( ! ceeDModelData.Any() ) return ;
      var viewModelUsed = new ViewModel.CeedViewModel( ceeDStorable, ceeDModelData ) ;
      LoadData( viewModelUsed ) ;
      _ceeDModelUsed = viewModelUsed ;
      _allCeeDModels = ceeDStorable.CeedModelUsedData.Any() ? new ViewModel.CeedViewModel( ceeDStorable, ceeDStorable.CeedModelData ) : viewModelUsed ;
      CbCodeIsUsed.Visibility = ceeDStorable.CeedModelUsedData.Any() ? Visibility.Visible : Visibility.Hidden ;
    }

    private void CodeIsUse_Checked( object sender, RoutedEventArgs e )
    {
      if ( _ceeDModelUsed == null ) return ;
      LoadData( _ceeDModelUsed ) ;
    }

    private void CodeIsUse_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( _allCeeDModels == null ) return ;
      LoadData( _allCeeDModels ) ;
    }

    private void LoadData( CeedViewModel ceeDViewModel )
    {
      this.DataContext = ceeDViewModel ;
      CmbCeeDModelNumbers.ItemsSource = ceeDViewModel.CeeDModelNumbers ;
      CmbModelNumbers.ItemsSource = ceeDViewModel.ModelNumbers ;
    }
  }
}