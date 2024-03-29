﻿using System.Collections.Generic ;
using System.Windows.Media ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class AutoCadColorsManager
  {
    public const string NoColor = "No Color" ;

    public static readonly Dictionary<int, Color> AutoCadColorDict = new()
    {
      { 0, Color.FromRgb( 0, 0, 0 ) },
      { 1, Color.FromRgb( 255, 0, 0 ) },
      { 2, Color.FromRgb( 255, 255, 0 ) },
      { 3, Color.FromRgb( 0, 255, 0 ) },
      { 4, Color.FromRgb( 0, 255, 255 ) },
      { 5, Color.FromRgb( 0, 0, 255 ) },
      { 6, Color.FromRgb( 255, 0, 255 ) },
      { 7, Color.FromRgb( 255, 255, 255 ) },
      { 8, Color.FromRgb( 65, 65, 65 ) },
      { 9, Color.FromRgb( 128, 128, 128 ) },
      { 10, Color.FromRgb( 255, 0, 0 ) },
      { 11, Color.FromRgb( 255, 170, 170 ) },
      { 12, Color.FromRgb( 189, 0, 0 ) },
      { 13, Color.FromRgb( 189, 126, 126 ) },
      { 14, Color.FromRgb( 129, 0, 0 ) },
      { 15, Color.FromRgb( 129, 86, 86 ) },
      { 16, Color.FromRgb( 104, 0, 0 ) },
      { 17, Color.FromRgb( 104, 69, 69 ) },
      { 18, Color.FromRgb( 79, 0, 0 ) },
      { 19, Color.FromRgb( 79, 53, 53 ) },
      { 20, Color.FromRgb( 255, 63, 0 ) },
      { 21, Color.FromRgb( 255, 191, 170 ) },
      { 22, Color.FromRgb( 189, 46, 0 ) },
      { 23, Color.FromRgb( 189, 141, 126 ) },
      { 24, Color.FromRgb( 129, 31, 0 ) },
      { 25, Color.FromRgb( 129, 96, 86 ) },
      { 26, Color.FromRgb( 104, 25, 0 ) },
      { 27, Color.FromRgb( 104, 78, 69 ) },
      { 28, Color.FromRgb( 79, 19, 0 ) },
      { 29, Color.FromRgb( 79, 59, 53 ) },
      { 30, Color.FromRgb( 255, 127, 0 ) },
      { 31, Color.FromRgb( 255, 212, 170 ) },
      { 32, Color.FromRgb( 189, 94, 0 ) },
      { 33, Color.FromRgb( 189, 157, 126 ) },
      { 34, Color.FromRgb( 129, 64, 0 ) },
      { 35, Color.FromRgb( 129, 107, 86 ) },
      { 36, Color.FromRgb( 104, 52, 0 ) },
      { 37, Color.FromRgb( 104, 86, 69 ) },
      { 38, Color.FromRgb( 79, 39, 0 ) },
      { 39, Color.FromRgb( 79, 66, 53 ) },
      { 40, Color.FromRgb( 255, 191, 0 ) },
      { 41, Color.FromRgb( 255, 234, 170 ) },
      { 42, Color.FromRgb( 189, 141, 0 ) },
      { 43, Color.FromRgb( 189, 173, 126 ) },
      { 44, Color.FromRgb( 129, 96, 0 ) },
      { 45, Color.FromRgb( 129, 118, 86 ) },
      { 46, Color.FromRgb( 104, 78, 0 ) },
      { 47, Color.FromRgb( 104, 95, 69 ) },
      { 48, Color.FromRgb( 79, 59, 0 ) },
      { 49, Color.FromRgb( 79, 73, 53 ) },
      { 50, Color.FromRgb( 255, 255, 0 ) },
      { 51, Color.FromRgb( 255, 255, 170 ) },
      { 52, Color.FromRgb( 189, 189, 0 ) },
      { 53, Color.FromRgb( 189, 189, 126 ) },
      { 54, Color.FromRgb( 129, 129, 0 ) },
      { 55, Color.FromRgb( 129, 129, 86 ) },
      { 56, Color.FromRgb( 104, 104, 0 ) },
      { 57, Color.FromRgb( 104, 104, 69 ) },
      { 58, Color.FromRgb( 79, 79, 0 ) },
      { 59, Color.FromRgb( 79, 79, 53 ) },
      { 60, Color.FromRgb( 191, 255, 0 ) },
      { 61, Color.FromRgb( 234, 255, 170 ) },
      { 62, Color.FromRgb( 141, 189, 0 ) },
      { 63, Color.FromRgb( 173, 189, 126 ) },
      { 64, Color.FromRgb( 96, 129, 0 ) },
      { 65, Color.FromRgb( 118, 129, 86 ) },
      { 66, Color.FromRgb( 78, 104, 0 ) },
      { 67, Color.FromRgb( 95, 104, 69 ) },
      { 68, Color.FromRgb( 59, 79, 0 ) },
      { 69, Color.FromRgb( 73, 79, 53 ) },
      { 70, Color.FromRgb( 127, 255, 0 ) },
      { 71, Color.FromRgb( 212, 255, 170 ) },
      { 72, Color.FromRgb( 94, 189, 0 ) },
      { 73, Color.FromRgb( 157, 189, 126 ) },
      { 74, Color.FromRgb( 64, 129, 0 ) },
      { 75, Color.FromRgb( 107, 129, 86 ) },
      { 76, Color.FromRgb( 52, 104, 0 ) },
      { 77, Color.FromRgb( 86, 104, 69 ) },
      { 78, Color.FromRgb( 39, 79, 0 ) },
      { 79, Color.FromRgb( 66, 79, 53 ) },
      { 80, Color.FromRgb( 63, 255, 0 ) },
      { 81, Color.FromRgb( 191, 255, 170 ) },
      { 82, Color.FromRgb( 46, 189, 0 ) },
      { 83, Color.FromRgb( 141, 189, 126 ) },
      { 84, Color.FromRgb( 31, 129, 0 ) },
      { 85, Color.FromRgb( 96, 129, 86 ) },
      { 86, Color.FromRgb( 25, 104, 0 ) },
      { 87, Color.FromRgb( 78, 104, 69 ) },
      { 88, Color.FromRgb( 19, 79, 0 ) },
      { 89, Color.FromRgb( 59, 79, 53 ) },
      { 90, Color.FromRgb( 0, 255, 0 ) },
      { 91, Color.FromRgb( 170, 255, 170 ) },
      { 92, Color.FromRgb( 0, 189, 0 ) },
      { 93, Color.FromRgb( 126, 189, 126 ) },
      { 94, Color.FromRgb( 0, 129, 0 ) },
      { 95, Color.FromRgb( 86, 129, 86 ) },
      { 96, Color.FromRgb( 0, 104, 0 ) },
      { 97, Color.FromRgb( 69, 104, 69 ) },
      { 98, Color.FromRgb( 0, 79, 0 ) },
      { 99, Color.FromRgb( 53, 79, 53 ) },
      { 100, Color.FromRgb( 0, 255, 63 ) },
      { 101, Color.FromRgb( 170, 255, 191 ) },
      { 102, Color.FromRgb( 0, 189, 46 ) },
      { 103, Color.FromRgb( 126, 189, 141 ) },
      { 104, Color.FromRgb( 0, 129, 31 ) },
      { 105, Color.FromRgb( 86, 129, 96 ) },
      { 106, Color.FromRgb( 0, 104, 25 ) },
      { 107, Color.FromRgb( 69, 104, 78 ) },
      { 108, Color.FromRgb( 0, 79, 19 ) },
      { 109, Color.FromRgb( 53, 79, 59 ) },
      { 110, Color.FromRgb( 0, 255, 127 ) },
      { 111, Color.FromRgb( 170, 255, 212 ) },
      { 112, Color.FromRgb( 0, 189, 94 ) },
      { 113, Color.FromRgb( 126, 189, 157 ) },
      { 114, Color.FromRgb( 0, 129, 64 ) },
      { 115, Color.FromRgb( 86, 129, 107 ) },
      { 116, Color.FromRgb( 0, 104, 52 ) },
      { 117, Color.FromRgb( 69, 104, 86 ) },
      { 118, Color.FromRgb( 0, 79, 39 ) },
      { 119, Color.FromRgb( 53, 79, 66 ) },
      { 120, Color.FromRgb( 0, 255, 191 ) },
      { 121, Color.FromRgb( 170, 255, 234 ) },
      { 122, Color.FromRgb( 0, 189, 141 ) },
      { 123, Color.FromRgb( 126, 189, 173 ) },
      { 124, Color.FromRgb( 0, 129, 96 ) },
      { 125, Color.FromRgb( 86, 129, 118 ) },
      { 126, Color.FromRgb( 0, 104, 78 ) },
      { 127, Color.FromRgb( 69, 104, 95 ) },
      { 128, Color.FromRgb( 0, 79, 59 ) },
      { 129, Color.FromRgb( 53, 79, 73 ) },
      { 130, Color.FromRgb( 0, 255, 255 ) },
      { 131, Color.FromRgb( 170, 255, 255 ) },
      { 132, Color.FromRgb( 0, 189, 189 ) },
      { 133, Color.FromRgb( 126, 189, 189 ) },
      { 134, Color.FromRgb( 0, 129, 129 ) },
      { 135, Color.FromRgb( 86, 129, 129 ) },
      { 136, Color.FromRgb( 0, 104, 104 ) },
      { 137, Color.FromRgb( 69, 104, 104 ) },
      { 138, Color.FromRgb( 0, 79, 79 ) },
      { 139, Color.FromRgb( 53, 79, 79 ) },
      { 140, Color.FromRgb( 0, 191, 255 ) },
      { 141, Color.FromRgb( 170, 234, 255 ) },
      { 142, Color.FromRgb( 0, 141, 189 ) },
      { 143, Color.FromRgb( 126, 173, 189 ) },
      { 144, Color.FromRgb( 0, 96, 129 ) },
      { 145, Color.FromRgb( 86, 118, 129 ) },
      { 146, Color.FromRgb( 0, 78, 104 ) },
      { 147, Color.FromRgb( 69, 95, 104 ) },
      { 148, Color.FromRgb( 0, 59, 79 ) },
      { 149, Color.FromRgb( 53, 73, 79 ) },
      { 150, Color.FromRgb( 0, 127, 255 ) },
      { 151, Color.FromRgb( 170, 212, 255 ) },
      { 152, Color.FromRgb( 0, 94, 189 ) },
      { 153, Color.FromRgb( 126, 157, 189 ) },
      { 154, Color.FromRgb( 0, 64, 129 ) },
      { 155, Color.FromRgb( 86, 107, 129 ) },
      { 156, Color.FromRgb( 0, 52, 104 ) },
      { 157, Color.FromRgb( 69, 86, 104 ) },
      { 158, Color.FromRgb( 0, 39, 79 ) },
      { 159, Color.FromRgb( 53, 66, 79 ) },
      { 160, Color.FromRgb( 0, 63, 255 ) },
      { 161, Color.FromRgb( 170, 191, 255 ) },
      { 162, Color.FromRgb( 0, 46, 189 ) },
      { 163, Color.FromRgb( 126, 141, 189 ) },
      { 164, Color.FromRgb( 0, 31, 129 ) },
      { 165, Color.FromRgb( 86, 96, 129 ) },
      { 166, Color.FromRgb( 0, 25, 104 ) },
      { 167, Color.FromRgb( 69, 78, 104 ) },
      { 168, Color.FromRgb( 0, 19, 79 ) },
      { 169, Color.FromRgb( 53, 59, 79 ) },
      { 170, Color.FromRgb( 0, 0, 255 ) },
      { 171, Color.FromRgb( 170, 170, 255 ) },
      { 172, Color.FromRgb( 0, 0, 189 ) },
      { 173, Color.FromRgb( 126, 126, 189 ) },
      { 174, Color.FromRgb( 0, 0, 129 ) },
      { 175, Color.FromRgb( 86, 86, 129 ) },
      { 176, Color.FromRgb( 0, 0, 104 ) },
      { 177, Color.FromRgb( 69, 69, 104 ) },
      { 178, Color.FromRgb( 0, 0, 79 ) },
      { 179, Color.FromRgb( 53, 53, 79 ) },
      { 180, Color.FromRgb( 63, 0, 255 ) },
      { 181, Color.FromRgb( 191, 170, 255 ) },
      { 182, Color.FromRgb( 46, 0, 189 ) },
      { 183, Color.FromRgb( 141, 126, 189 ) },
      { 184, Color.FromRgb( 31, 0, 129 ) },
      { 185, Color.FromRgb( 96, 86, 129 ) },
      { 186, Color.FromRgb( 25, 0, 104 ) },
      { 187, Color.FromRgb( 78, 69, 104 ) },
      { 188, Color.FromRgb( 19, 0, 79 ) },
      { 189, Color.FromRgb( 59, 53, 79 ) },
      { 190, Color.FromRgb( 127, 0, 255 ) },
      { 191, Color.FromRgb( 212, 170, 255 ) },
      { 192, Color.FromRgb( 94, 0, 189 ) },
      { 193, Color.FromRgb( 157, 126, 189 ) },
      { 194, Color.FromRgb( 64, 0, 129 ) },
      { 195, Color.FromRgb( 107, 86, 129 ) },
      { 196, Color.FromRgb( 52, 0, 104 ) },
      { 197, Color.FromRgb( 86, 69, 104 ) },
      { 198, Color.FromRgb( 39, 0, 79 ) },
      { 199, Color.FromRgb( 66, 53, 79 ) },
      { 200, Color.FromRgb( 191, 0, 255 ) },
      { 201, Color.FromRgb( 234, 170, 255 ) },
      { 202, Color.FromRgb( 141, 0, 189 ) },
      { 203, Color.FromRgb( 173, 126, 189 ) },
      { 204, Color.FromRgb( 96, 0, 129 ) },
      { 205, Color.FromRgb( 118, 86, 129 ) },
      { 206, Color.FromRgb( 78, 0, 104 ) },
      { 207, Color.FromRgb( 95, 69, 104 ) },
      { 208, Color.FromRgb( 59, 0, 79 ) },
      { 209, Color.FromRgb( 73, 53, 79 ) },
      { 210, Color.FromRgb( 255, 0, 255 ) },
      { 211, Color.FromRgb( 255, 170, 255 ) },
      { 212, Color.FromRgb( 189, 0, 189 ) },
      { 213, Color.FromRgb( 189, 126, 189 ) },
      { 214, Color.FromRgb( 129, 0, 129 ) },
      { 215, Color.FromRgb( 129, 86, 129 ) },
      { 216, Color.FromRgb( 104, 0, 104 ) },
      { 217, Color.FromRgb( 104, 69, 104 ) },
      { 218, Color.FromRgb( 79, 0, 79 ) },
      { 219, Color.FromRgb( 79, 53, 79 ) },
      { 220, Color.FromRgb( 255, 0, 191 ) },
      { 221, Color.FromRgb( 255, 170, 234 ) },
      { 222, Color.FromRgb( 189, 0, 141 ) },
      { 223, Color.FromRgb( 189, 126, 173 ) },
      { 224, Color.FromRgb( 129, 0, 96 ) },
      { 225, Color.FromRgb( 129, 86, 118 ) },
      { 226, Color.FromRgb( 104, 0, 78 ) },
      { 227, Color.FromRgb( 104, 69, 95 ) },
      { 228, Color.FromRgb( 79, 0, 59 ) },
      { 229, Color.FromRgb( 79, 53, 73 ) },
      { 230, Color.FromRgb( 255, 0, 127 ) },
      { 231, Color.FromRgb( 255, 170, 212 ) },
      { 232, Color.FromRgb( 189, 0, 94 ) },
      { 233, Color.FromRgb( 189, 126, 157 ) },
      { 234, Color.FromRgb( 129, 0, 64 ) },
      { 235, Color.FromRgb( 129, 86, 107 ) },
      { 236, Color.FromRgb( 104, 0, 52 ) },
      { 237, Color.FromRgb( 104, 69, 86 ) },
      { 238, Color.FromRgb( 79, 0, 39 ) },
      { 239, Color.FromRgb( 79, 53, 66 ) },
      { 240, Color.FromRgb( 255, 0, 63 ) },
      { 241, Color.FromRgb( 255, 170, 191 ) },
      { 242, Color.FromRgb( 189, 0, 46 ) },
      { 243, Color.FromRgb( 189, 126, 141 ) },
      { 244, Color.FromRgb( 129, 0, 31 ) },
      { 245, Color.FromRgb( 129, 86, 96 ) },
      { 246, Color.FromRgb( 104, 0, 25 ) },
      { 247, Color.FromRgb( 104, 69, 78 ) },
      { 248, Color.FromRgb( 79, 0, 19 ) },
      { 249, Color.FromRgb( 79, 53, 59 ) },
      { 250, Color.FromRgb( 51, 51, 51 ) },
      { 251, Color.FromRgb( 80, 80, 80 ) },
      { 252, Color.FromRgb( 105, 105, 105 ) },
      { 253, Color.FromRgb( 130, 130, 130 ) },
      { 254, Color.FromRgb( 190, 190, 190 ) },
      { 255, Color.FromRgb( 255, 255, 255 ) },
    } ;

    public static List<AutoCadColor> GetAutoCadColorDict()
    {
      var autoCadColors = new List<AutoCadColor> { new( NoColor, new SolidColorBrush() ) } ;
      foreach ( var (index, color) in AutoCadColorDict ) {
        autoCadColors.Add( new AutoCadColor( index.ToString(), new SolidColorBrush( color ) ) ) ;
      }

      return autoCadColors ;
    }

    public class AutoCadColor
    {
      public string Index { get ; set ; }
      public SolidColorBrush SolidColor { get ; set ; }

      public AutoCadColor( string index, SolidColorBrush solidColor )
      {
        Index = index ;
        SolidColor = solidColor ;
      }
    }
  }
}