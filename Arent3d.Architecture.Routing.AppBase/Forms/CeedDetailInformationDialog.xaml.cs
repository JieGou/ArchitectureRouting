using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeedDetailInformationDialog : Window
  {
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModel ;
    private readonly List<HiroiMasterModel> _hiroiMasterModel ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterModel ;
    private readonly List<CeedModel> _listCeedModel ;
    private readonly CsvStorable _csvStorable ;
    private readonly Document _document ;
    private string _setCode ;
    private string _productCodeFormat = "{0:D6}" ;

    public CeedDetailInformationDialog( Document document, string pickedText )
    {
      InitializeComponent() ;
      _document = document ;
      _setCode = pickedText ;
      _listCeedModel = document.GetAllStorables<CeedStorable>().FirstOrDefault()?.CeedModelData ?? new List<CeedModel>() ;
      _csvStorable = document.GetCsvStorable() ;
      _hiroiSetMasterNormalModel = _csvStorable.HiroiSetMasterNormalModelData ;
      _hiroiMasterModel = _csvStorable.HiroiMasterModelData ;
      _hiroiSetCdMasterModel = _csvStorable.HiroiSetCdMasterNormalModelData ;

      txtSetCode.Text = _setCode ;
      LoadComboboxSelect() ;
    }

    private void LoadComboboxSelect()
    {
      // Fill data into combobox
      ConstructionClassificationCombobox.ItemsSource = _hiroiSetCdMasterModel.Select( x => x.ConstructionClassification ).Distinct() ;

      //Set selected value for combobox
      var hiroiSetCdMaster = _hiroiSetCdMasterModel.Find( x => x.SetCode == _setCode ) ;
      ConstructionClassificationCombobox.SelectedValue = hiroiSetCdMaster == null ? string.Empty : hiroiSetCdMaster.ConstructionClassification ;
    }

    private void BuildQueryData( string materialCode, string quantity, string parentPartModelNumber, int materialIndex, ref ObservableCollection<QueryData> queryData )
    {
      if ( ! string.IsNullOrWhiteSpace( materialCode ) ) {
        materialCode = String.Format( _productCodeFormat, Convert.ToInt32( materialCode ) ) ;
        var hiroiMasterItem = _hiroiMasterModel.FirstOrDefault( x => x.Buzaicd == materialCode ) ;
        var name = hiroiMasterItem != null ? hiroiMasterItem.Hinmei.Trim() : string.Empty ;
        var standard = hiroiMasterItem != null ? hiroiMasterItem.Kikaku.Trim() : string.Empty ;
        queryData.Add( new QueryData( materialCode, name, standard, quantity, parentPartModelNumber, materialIndex ) ) ;
      }
    }

    private void LoadData()
    {
      ObservableCollection<QueryData> queryData = new ObservableCollection<QueryData>() ;
      if ( ! string.IsNullOrWhiteSpace( _setCode ) ) {
        CeedModel? ceedModel = _listCeedModel.FirstOrDefault( ( model => string.Equals( model.CeeDSetCode, _setCode, StringComparison.InvariantCultureIgnoreCase ) ) ) ;
        if ( ceedModel != null ) {
          var hiroiSetMasterNormalList = _hiroiSetMasterNormalModel.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeeDModelNumber ) ) ;
          foreach ( var item in hiroiSetMasterNormalList ) {
            BuildQueryData( item.MaterialCode1, item.Quantity1, item.ParentPartModelNumber, 1, ref queryData ) ;
            BuildQueryData( item.MaterialCode2, item.Quantity2, item.ParentPartModelNumber, 2, ref queryData ) ;
            BuildQueryData( item.MaterialCode3, item.Quantity3, item.ParentPartModelNumber, 3, ref queryData ) ;
            BuildQueryData( item.MaterialCode4, item.Quantity4, item.ParentPartModelNumber, 4, ref queryData ) ;
            BuildQueryData( item.MaterialCode5, item.Quantity5, item.ParentPartModelNumber, 5, ref queryData ) ;
            BuildQueryData( item.MaterialCode6, item.Quantity6, item.ParentPartModelNumber, 6, ref queryData ) ;
            BuildQueryData( item.MaterialCode7, item.Quantity7, item.ParentPartModelNumber, 7, ref queryData ) ;
            BuildQueryData( item.MaterialCode8, item.Quantity8, item.ParentPartModelNumber, 8, ref queryData ) ;
          }
        }
      }

      CeeDDetailInformationModel ceeDDetailInformationModels = new CeeDDetailInformationModel( queryData, "" ) ;
      CeeDDetailInformationViewModel viewModel = new CeeDDetailInformationViewModel( ceeDDetailInformationModels ) ;
      DataContext = viewModel ;
    }

    private void TxtSetCode_OnTextChanged( object sender, TextChangedEventArgs e )
    {
      _setCode = txtSetCode.Text.Trim() ;

      //Set selected value for combobox
      var hiroiSetCdMaster = _hiroiSetCdMasterModel.Find( x => x.SetCode == _setCode ) ;

      if ( hiroiSetCdMaster == null ) {
        ConstructionClassificationCombobox.SelectedIndex = -1 ;
      }
      else {
        ConstructionClassificationCombobox.SelectedValue = hiroiSetCdMaster.ConstructionClassification ;
      }

      LoadData() ;
    }

    private void UpdateConstructionClassification( object sender, TextChangedEventArgs e )
    {
      if ( ConstructionClassificationCombobox.SelectedValue == null ) return ;
      var selectedValue = ConstructionClassificationCombobox.SelectedValue ;
      var item = _hiroiSetCdMasterModel.Find( x => x.SetCode.Equals( _setCode.ToUpper() ) ) ;
      if ( item == null ) return ;
      if ( item.ConstructionClassification.Equals( ConstructionClassificationCombobox.SelectedValue.ToString() ) ) return ;

      item.ConstructionClassification = ConstructionClassificationCombobox.SelectedValue.ToString() ;
      _csvStorable.HiroiSetCdMasterNormalModelData = _hiroiSetCdMasterModel ;

      try {
        using Transaction t = new Transaction( _document, "Update Consutrction Classification" ) ;
        t.Start() ;
        _csvStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Failed to update the construction classification.", "Error Message" ) ;
        DialogResult = false ;
      }
    }

    private void BtnOK_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      this.Close() ;
    }

    private void BtnReset_OnClick( object sender, RoutedEventArgs e )
    {
      this.Close() ;
    }

    private void Button_DeleteRow( object sender, RoutedEventArgs e )
    {
      if ( DtGrid.SelectedCells.Count < 1 ) return ;

      var row = (QueryData) DtGrid.SelectedItem ;
      var item = _hiroiSetMasterNormalModel.Find( x => x.ParentPartModelNumber.Equals( row.ParentPartModelNumber ) && x.ParentPartModelNumber.Equals( row.ParentPartModelNumber ) ) ;
      switch ( row.MaterialIndex ) {
        case 1 :
          item.MaterialCode1 = string.Empty ;
          item.Quantity1 = string.Empty ;
          item.Name1 = string.Empty ;
          break ;
        case 2 :
          item.MaterialCode2 = string.Empty ;
          item.Quantity2 = string.Empty ;
          item.Name2 = string.Empty ;
          break ;
        case 3 :
          item.MaterialCode3 = string.Empty ;
          item.Quantity3 = string.Empty ;
          item.Name3 = string.Empty ;
          break ;
        case 4 :
          item.MaterialCode4 = string.Empty ;
          item.Quantity4 = string.Empty ;
          item.Name4 = string.Empty ;
          break ;
        case 5 :
          item.MaterialCode5 = string.Empty ;
          item.Quantity5 = string.Empty ;
          item.Name5 = string.Empty ;
          break ;
        case 6 :
          item.MaterialCode6 = string.Empty ;
          item.Quantity6 = string.Empty ;
          item.Name6 = string.Empty ;
          break ;
        case 7 :
          item.MaterialCode7 = string.Empty ;
          item.Quantity7 = string.Empty ;
          item.Name7 = string.Empty ;
          break ;
        case 8 :
          item.MaterialCode8 = string.Empty ;
          item.Quantity8 = string.Empty ;
          item.Name8 = string.Empty ;
          break ;
        default :
          break ;
      }

      if ( string.IsNullOrWhiteSpace( item.MaterialCode1 ) && string.IsNullOrWhiteSpace( item.MaterialCode2 ) && string.IsNullOrWhiteSpace( item.MaterialCode3 ) && string.IsNullOrWhiteSpace( item.MaterialCode4 ) && string.IsNullOrWhiteSpace( item.MaterialCode5 ) && string.IsNullOrWhiteSpace( item.MaterialCode6 ) && string.IsNullOrWhiteSpace( item.MaterialCode7 ) && string.IsNullOrWhiteSpace( item.MaterialCode8 ) ) {
        _hiroiSetMasterNormalModel.Remove( item ) ;
      }

      _csvStorable.HiroiSetMasterNormalModelData = _hiroiSetMasterNormalModel ;

      try {
        using Transaction t = new Transaction( _document, "Delete data" ) ;
        t.Start() ;
        _csvStorable.Save() ;
        t.Commit() ;

        LoadData() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Delete Data Failed.", "Error Message" ) ;
        DialogResult = false ;
      }
    }
  }
}