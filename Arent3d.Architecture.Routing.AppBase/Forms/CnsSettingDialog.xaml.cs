using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.ComponentModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CnsSettingDialog
  {
    private int _editingRowIndex = -1 ;
    private readonly CnsSettingViewModel _cnsSettingViewModel ;
    private readonly Document _document ;
    private readonly ObservableCollection<CnsSettingModel> _currentCnsSettingData ;
    private bool _isEditModel = false ;
    public CnsSettingDialog( CnsSettingViewModel viewModel, Document document)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      _cnsSettingViewModel = viewModel ;
      _document = document ;
      _currentCnsSettingData = CopyCnsSetting(document.GetCnsSettingStorable().CnsSettingData) ;
      
    }

    private void Update_Click( object sender, RoutedEventArgs e )
    {
      if ( grdCategories.SelectedItem == null ) return ;
      var selectedItem = ( (CnsSettingModel) grdCategories.SelectedItem ) ;
      if ( selectedItem.CategoryName == "未設定" ) return ;
      if ( CheckDuplicateName( e ) ) return ;
      grdCategories.IsReadOnly = false ;
      _isEditModel = true ;
      grdCategories.CurrentCell = new DataGridCellInfo( grdCategories.SelectedItem, grdCategories.Columns[ 1 ] ) ; 
      grdCategories.BeginEdit() ; 
    }

    private void Close_Dialog()
    {
      SetEmptyDuplicateName() ;
      DialogResult = true ;
      Close() ;
    }

    private void CnsSettingDialog_Closing( object sender, CancelEventArgs e )
    {
      DialogResult ??= false ;
    }

    private void GrdCategories_OnCellEditEnding( object sender, DataGridCellEditEndingEventArgs e )
    {
      if ( DialogResult != false ) { 
        var isDuplicateName = grdCategories.ItemsSource.Cast<CnsSettingModel>().Where( x => ! string.IsNullOrEmpty( x.CategoryName ) ).GroupBy( x => x.CategoryName ).Any( g => g.Count() > 1 ) ;
        if ( isDuplicateName ) {
          MessageBox.Show( "工事項目名称がすでに存在しています。再度工事項目名称を入力してください。" ) ;
          _editingRowIndex = e.Row.GetIndex() ;
          _isEditModel = false ;
          e.Cancel = true ;
          return ;
        }
      }

      _isEditModel = false ;
      _editingRowIndex = -1 ; 
    }

    private void AddNewRow_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.AddRowCommand.CanExecute( null ) )
        _cnsSettingViewModel.AddRowCommand.Execute( null ) ;
      grdCategories.IsReadOnly = false ;
      _isEditModel = true ;
      grdCategories.SelectedIndex = grdCategories.Items.Count - 1 ;
      grdCategories.SelectedItem = grdCategories.Items.IndexOf( grdCategories.SelectedIndex ) ;
      grdCategories.CurrentCell = new DataGridCellInfo( grdCategories.SelectedItem, grdCategories.Columns[ 1 ] ) ; 
      grdCategories.BeginEdit() ; 
    }

    private void Delete_Click( object sender, RoutedEventArgs e )
    {
      var selectedItem = ( (CnsSettingModel) grdCategories.SelectedItem ) ;
      if ( selectedItem.CategoryName == "未設定" ) return ;
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.DeleteRowCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.DeleteRowCommand.Execute( grdCategories.SelectedIndex ) ;
    }

    private void Export_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.WriteFileCommand.CanExecute( null ) )
        _cnsSettingViewModel.WriteFileCommand.Execute( null ) ;
    }

    private void Import_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.ReadFileCommand.CanExecute( null ) )
        _cnsSettingViewModel.ReadFileCommand.Execute( null ) ;
      try {
        using Transaction t = new Transaction( _document, "Save construction item" ) ;
        var cnsSettingStorable = _cnsSettingViewModel.CnsSettingStorable ;
        t.Start() ;
        cnsSettingStorable.Save() ;
        UpdateConstructionsItem() ;
        t.Commit() ;
      }
      catch {
        MessageBox.Show( "Save construction item failed.", "Error Message" ) ;
      }
    }

    private void SymbolApply_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SetConstructionItemForSymbolsCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.SetConstructionItemForSymbolsCommand.Execute( grdCategories.SelectedIndex ) ;
    }

    private void ConduitsApply_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SetConstructionItemForConduitsCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.SetConstructionItemForConduitsCommand.Execute( grdCategories.SelectedIndex ) ;
    }
    
    private void RacksApply_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SetConstructionItemForRacksCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.SetConstructionItemForRacksCommand.Execute( grdCategories.SelectedIndex ) ;
    }
    
    private void AllElementsApply_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SetConstructionItemForAllCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.SetConstructionItemForAllCommand.Execute( grdCategories.SelectedIndex ) ;
    }

    private void Save_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.SaveCommand.CanExecute( null ) )
        _cnsSettingViewModel.SaveCommand.Execute( null ) ;
    }

    private bool CheckDuplicateName( RoutedEventArgs e )
    {
      if ( ! grdCategories.ItemsSource.Cast<CnsSettingModel>().Where( x => ! string.IsNullOrEmpty( x.CategoryName ) ).GroupBy( x => x.CategoryName ).Any( g => g.Count() > 1 ) ) return false ;
      MessageBox.Show( "工事項目名称がすでに存在しています。再度工事項目名称を入力してください。" ) ;
      grdCategories.SelectedIndex = _editingRowIndex ;
      e.Handled = true ;
      return true ;
    }

    private void SetEmptyDuplicateName()
    {
      var duplicateCategoryName = grdCategories.ItemsSource.Cast<CnsSettingModel>().Where( x => ! string.IsNullOrEmpty( x.CategoryName ) ).GroupBy( x => x.CategoryName ).Where( g => g.Count() > 1 ).ToList() ;

      if ( ! duplicateCategoryName.Any() ) return ;
      var sequences = duplicateCategoryName.FirstOrDefault()!.Select( c => c.Sequence ).ToList() ;
      for ( var i = 0 ; i < sequences.Count() ; i++ ) {
        if ( i != 0 )
          grdCategories.ItemsSource.Cast<CnsSettingModel>().ToList()[ sequences[ i ] - 1 ].CategoryName = string.Empty ;
      }
    }

    public void UpdateConstructionsItem()
    {
      var newCnsSettingData = _cnsSettingViewModel.CnsSettingStorable.CnsSettingData ;
      var conduits = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).ToList() ;
      var connectors = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).Where( x => x is FamilyInstance or TextNote ).ToList() ;
      Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      Dictionary<Element, string> updateConnectors = new Dictionary<Element, string>() ;

      if ( IsConduitsHaveConstructionItemProperty() ) {
        //update Constructions Item for Conduits
        foreach ( var conduit in conduits ) {
          conduit.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? strConduitConstructionItem ) ;
          if ( string.IsNullOrEmpty( strConduitConstructionItem ) ) continue ;

          var conduitCnsSetting = _currentCnsSettingData.FirstOrDefault( c => c.CategoryName == strConduitConstructionItem ) ;
          if ( conduitCnsSetting == null ) {
            continue ;
          }

          if ( newCnsSettingData.All( c => c.Position != conduitCnsSetting.Position ) ) {
            conduit.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, "未設定" ) ;
            continue ;
          }

          var newConduitCnsSetting = newCnsSettingData.First( c => c.Position == conduitCnsSetting.Position ) ;
          if ( newConduitCnsSetting == null ) continue ;
          if ( newConduitCnsSetting.CategoryName == strConduitConstructionItem ) continue ;
          conduit.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, newConduitCnsSetting.CategoryName ) ;
        }
      }

      //Ungroup, Get Connector to Update
      if ( ! IsConnectorsHaveConstructionItemProperty() ) return ;
      foreach ( var connector in connectors ) {
        connector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? strConnectorConstructionItem ) ;
        if ( string.IsNullOrEmpty( strConnectorConstructionItem ) ) continue ;

        var connectorCnsSetting = _currentCnsSettingData.FirstOrDefault( c => c.CategoryName == strConnectorConstructionItem ) ;
        if ( connectorCnsSetting == null ) {
          continue ;
        }

        string newConstructionItemValue ;
        if ( newCnsSettingData.All( c => c.Position != connectorCnsSetting.Position ) ) {
          newConstructionItemValue = "未設定" ;
        }
        else {
          var newConnectorCnsSetting = newCnsSettingData.First( c => c.Position == connectorCnsSetting.Position ) ;
          if ( newConnectorCnsSetting == null ) continue ;
          if ( newConnectorCnsSetting.CategoryName == strConnectorConstructionItem ) continue ;
          newConstructionItemValue = newConnectorCnsSetting.CategoryName ;
        }

        // Groupされていないコネクタに対する処理
        if ( _document.GetElement( connector.GroupId ) is not Group parentGroup ) {
          updateConnectors.Add( connector, newConstructionItemValue ) ;
        }
        // Groupされているコネクタに対する処理
        else {
          var attachedGroup = _document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ; // ungroup before set property
          List<ElementId> listTextNoteIds = new() ;
          foreach ( var group in attachedGroup ) {
            // ungroup textNote before ungroup connector
            var ids = group.GetMemberIds() ;
            listTextNoteIds.AddRange( ids ) ;
            group.UngroupMembers() ;
          }

          parentGroup.UngroupMembers() ;
          connectorGroups.Add( connector.Id, listTextNoteIds ) ;
          updateConnectors.Add( connector, newConstructionItemValue ) ;
        }
      }

      // update ConstructionItem for connector 
      foreach ( var (e, value) in updateConnectors ) {
        e.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, value ) ;
      }

      _document.Regenerate() ;
      // create group for updated connector (with new property) and related text note if any
      foreach ( var (key, value) in connectorGroups ) {
        List<ElementId> groupIds = new() { key } ;
        groupIds.AddRange( value ) ;
        _document.Create.NewGroup( groupIds ) ;
      }
    }

    public bool IsConnectorsHaveConstructionItemProperty()
    {
      var connector = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).FirstOrDefault() ;
      return connector != null && connector.HasParameter( ElectricalRoutingElementParameter.ConstructionItem ) ;
    }

    public bool IsConduitsHaveConstructionItemProperty()
    {
      var conduit = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).FirstOrDefault() ;
      return conduit != null && conduit.HasParameter( ElectricalRoutingElementParameter.ConstructionItem ) ;
    }

    public static ObservableCollection<T> CopyCnsSetting<T>( IEnumerable<T>? listCnsSettingData ) where T : ICloneable
    {
      return listCnsSettingData != null ? new ObservableCollection<T>( listCnsSettingData.Select( x => x.Clone() ).Cast<T>() ) : null! ;
    }

    private void ApplyRangSelection_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      Close_Dialog() ;
      if ( _cnsSettingViewModel.ApplyRangSelectionCommand.CanExecute( grdCategories.SelectedIndex ) )
        _cnsSettingViewModel.ApplyRangSelectionCommand.Execute( grdCategories.SelectedIndex ) ;
    }

    private void GrdCategories_OnCellBeforeEdit( object sender, DataGridPreparingCellForEditEventArgs e )
    { 
      if ( e.EditingElement is not TextBox ) return ; 
      if ( ((TextBox)e.EditingElement).Text == "未設定" || !_isEditModel)
        grdCategories.CancelEdit( DataGridEditingUnit.Cell ) ;

    }
  }
}