using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.ComponentModel ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CnsSettingDialog : Window
  {
    private int _editingRowIndex = -1 ;
    private readonly CnsSettingViewModel _cnsSettingViewModel ;
    private readonly Document _document ;
    private readonly ObservableCollection<CnsSettingModel> _currentCnsSettingData ;
    private readonly bool _hasConstructionItemProp ;

    public CnsSettingDialog( CnsSettingViewModel viewModel, Document document, ObservableCollection<CnsSettingModel> currentCnsSettingData, bool hasConstructionItemProp )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      _cnsSettingViewModel = viewModel ;
      _document = document ;
      _currentCnsSettingData = currentCnsSettingData ;
      _hasConstructionItemProp = hasConstructionItemProp ;
    }

    private void Update_Click( object sender, RoutedEventArgs e )
    {
      if ( grdCategories.SelectedItem == null ) return ;
      var selectedItem = ( (CnsSettingModel) grdCategories.SelectedItem ) ;
      if ( selectedItem.CategoryName == "未設定" ) return ;
      if ( CheckDuplicateName( e ) ) return ;
      grdCategories.IsReadOnly = false ;
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
          e.Cancel = true ;
          return ;
        }
      }
      _editingRowIndex = -1 ;
      grdCategories.IsReadOnly = true ;
    }

    private void AddNewRow_Click( object sender, RoutedEventArgs e )
    {
      if ( CheckDuplicateName( e ) ) return ;
      if ( _cnsSettingViewModel.AddRowCommand.CanExecute( null ) )
        _cnsSettingViewModel.AddRowCommand.Execute( null ) ;
      grdCategories.IsReadOnly = false ;
    }

    private void Delete_Click( object sender, RoutedEventArgs e )
    {
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
        UpdateConstructionsItem( _document, _currentCnsSettingData, cnsSettingStorable.CnsSettingData, _hasConstructionItemProp ) ;
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

    public static void UpdateConstructionsItem( Document document, ObservableCollection<CnsSettingModel> currentCnsSettingData, ObservableCollection<CnsSettingModel> newCnsSettingData, bool hasConstructionItemProp )
    {
      var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).ToList() ;
      var connectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).Where( x => x is FamilyInstance or TextNote ).ToList() ;
      Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      Dictionary<Element, string> updateConnectors = new Dictionary<Element, string>() ;

      //update Constructions Item for Conduits
      foreach ( var conduit in conduits ) {
        var strConduitConstructionItem = conduit.GetPropertyString( RoutingFamilyLinkedParameter.ConstructionItem ) ;
        if ( string.IsNullOrEmpty( strConduitConstructionItem ) ) continue ;

        var conduitCnsSetting = currentCnsSettingData.FirstOrDefault( c => c.CategoryName == strConduitConstructionItem ) ;
        if ( conduitCnsSetting == null ) {
          continue ;
        }

        if ( newCnsSettingData.All( c => c.Position != conduitCnsSetting.Position ) ) {
          conduit.SetProperty( RoutingFamilyLinkedParameter.ConstructionItem, "未設定" ) ;
          continue ;
        }

        var newConduitCnsSetting = newCnsSettingData.First( c => c.Position == conduitCnsSetting.Position ) ;
        if ( newConduitCnsSetting == null ) continue ;
        if ( newConduitCnsSetting.CategoryName == strConduitConstructionItem ) continue ;
        conduit.SetProperty( RoutingFamilyLinkedParameter.ConstructionItem, newConduitCnsSetting.CategoryName ) ;
      }

      //Ungroup, Get Connector to Update
      if ( ! hasConstructionItemProp ) return ;
      foreach ( var connector in connectors ) {
        var strConnectorConstructionItem = connector.GetPropertyString( RoutingFamilyLinkedParameter.ConstructionItem ) ;
        if ( string.IsNullOrEmpty( strConnectorConstructionItem ) ) continue ;

        var connectorCnsSetting = currentCnsSettingData.FirstOrDefault( c => c.CategoryName == strConnectorConstructionItem ) ;
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

        var parentGroup = document.GetElement( connector.GroupId ) as Group ;
        if ( parentGroup != null ) {
          // ungroup before set property
          var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
          List<ElementId> listTextNoteIds = new List<ElementId>() ;
          // ungroup textNote before ungroup connector
          foreach ( var group in attachedGroup ) {
            var ids = @group.GetMemberIds() ;
            listTextNoteIds.AddRange( ids ) ;
            @group.UngroupMembers() ;
          }

          parentGroup.UngroupMembers() ;
          connectorGroups.Add( connector.Id, listTextNoteIds ) ;
          updateConnectors.Add( connector, newConstructionItemValue ) ;
        }
      }

      // update ConstructionItem for connector 
      foreach ( var updateItem in updateConnectors ) {
        var e = updateItem.Key ;
        string value = updateItem.Value ;
        e.SetProperty( RoutingFamilyLinkedParameter.ConstructionItem, value ) ;
      }

      document.Regenerate() ;
      // create group for updated connector (with new property) and related text note if any
      foreach ( var item in connectorGroups ) {
        List<ElementId> groupIds = new List<ElementId>() ;
        groupIds.Add( item.Key ) ;
        groupIds.AddRange( item.Value ) ;
        document.Create.NewGroup( groupIds ) ;
      }
    }
  }
}