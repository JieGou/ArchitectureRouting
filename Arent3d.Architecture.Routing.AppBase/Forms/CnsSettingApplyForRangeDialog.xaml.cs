using System.Collections.Generic ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Model ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CnsSettingApplyForRangeDialog : Window
  {
    private List<CnsSettingApplyConstructionItem> CnsSettingApplyConstructionItems { get ; set ; } 
    public CnsSettingApplyForRangeDialog(List<CnsSettingApplyConstructionItem> cnsSettingApplyConstructionItems)
    {
      InitializeComponent() ;
      CnsSettingApplyConstructionItems = cnsSettingApplyConstructionItems ;
      DataGridConstructionItem.ItemsSource = CnsSettingApplyConstructionItems ;
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
}