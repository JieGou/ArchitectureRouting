using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CnsSettingApplyForRangeDialog
  { 
    public CnsSettingApplyForRangeDialog(CnsSettingApplyForRangeViewModel cnsSettingApplyForRangeViewModel)
    {
      InitializeComponent() ;
      DataContext = cnsSettingApplyForRangeViewModel ;
    }  
    private void BtnOK_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close();
    }

    private void BtnCancel_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close();
    }
  }
  
  public abstract class DesignCnsSettingApplyForRangeViewModel : CnsSettingApplyForRangeViewModel
  {
    protected DesignCnsSettingApplyForRangeViewModel( List<CnsSettingApplyConstructionItem> cnsSettingApplyConstructionItems, ObservableCollection<string> constructionItemList ) : base( cnsSettingApplyConstructionItems, constructionItemList )
    {
    }
  }
}