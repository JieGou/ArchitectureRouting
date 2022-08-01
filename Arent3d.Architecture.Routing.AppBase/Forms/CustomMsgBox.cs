using System.Windows.Forms ;
using Form = System.Windows.Forms.Form ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CustomMsgBox : Form {
    private string _msg = string.Empty;
    public CustomMsgBox(string msg) {
      InitializeComponent();
      MinimizeBox = false;
      MaximizeBox = false;
      FormBorderStyle = FormBorderStyle.FixedSingle;
      message.Text = msg ;
      StartPosition = FormStartPosition.CenterScreen ;
    }
  }

  public static class MyMessageBox
  {
    public static DialogResult Show(string message)
    {
      return Show(message, string.Empty);
    }
    public static DialogResult Show(string message, string title)
    {
      CustomMsgBox msg = new( message ) { TopMost = true, Text = title } ;
      return msg.ShowDialog();
    }
  }
}