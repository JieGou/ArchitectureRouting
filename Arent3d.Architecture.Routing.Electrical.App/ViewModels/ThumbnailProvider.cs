﻿using System ;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.IO ;
using System.Runtime.InteropServices ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  [Flags]
  public enum ThumbnailOptions
  {
    None = 0x00,
    BiggerSizeOk = 0x01,
    InMemoryOnly = 0x02,
    IconOnly = 0x04,
    ThumbnailOnly = 0x08,
    InCacheOnly = 0x10,
  }

  public class ThumbnailProvider
  {
    private const string ShellItemGuid = "7E9FB0D3-919F-4307-AB2E-9B1860310C93" ;

    [DllImport( "shell32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    internal static extern int SHCreateItemFromParsingName( [MarshalAs( UnmanagedType.LPWStr )] string path, IntPtr pbc, ref Guid riid, [MarshalAs( UnmanagedType.Interface )] out IShellItem shellItem ) ;

    [DllImport( "gdi32.dll" )]
    [return: MarshalAs( UnmanagedType.Bool )]
    internal static extern bool DeleteObject( IntPtr hObject ) ;

    [ComImport]
    [InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
    [Guid( "43826d1e-e718-42ee-bc55-a1e261c37bfe" )]
    internal interface IShellItem
    {
      void BindToHandler( IntPtr pbc, [MarshalAs( UnmanagedType.LPStruct )] Guid bhid, [MarshalAs( UnmanagedType.LPStruct )] Guid riid, out IntPtr ppv ) ;
      void GetParent( out IShellItem ppsi ) ;
      void GetDisplayName( Sigdn sigdnName, out IntPtr ppszName ) ;
      void GetAttributes( uint sfgaoMask, out uint psfgaoAttribs ) ;
      void Compare( IShellItem psi, uint hint, out int piOrder ) ;
    } ;

    internal enum Sigdn : uint
    {
      NormalDisplay = 0,
      ParentRelativeParsing = 0x80018001,
      ParentRelativeForAddressBar = 0x8001c001,
      DesktopAbsoluteParsing = 0x80028000,
      ParentRelativeEditing = 0x80031001,
      DesktopAbsoluteEditing = 0x8004c000,
      FileSysPath = 0x80058000,
      Url = 0x80068000
    }

    internal enum HResult
    {
      Ok = 0x0000,
      False = 0x0001,
      InvalidArguments = unchecked( (int) 0x80070057 ),
      OutOfMemory = unchecked( (int) 0x8007000E ),
      NoInterface = unchecked( (int) 0x80004002 ),
      Fail = unchecked( (int) 0x80004005 ),
      ElementNotFound = unchecked( (int) 0x80070490 ),
      TypeElementNotFound = unchecked( (int) 0x8002802B ),
      NoObject = unchecked( (int) 0x800401E5 ),
      Win32ErrorCanceled = 1223,
      Canceled = unchecked( (int) 0x800704C7 ),
      ResourceInUse = unchecked( (int) 0x800700AA ),
      AccessDenied = unchecked( (int) 0x80030005 )
    }

    [ComImport]
    [Guid( "bcc18b79-ba16-442f-80c4-8a59c30c463b" )]
    [InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
    internal interface IShellItemImageFactory
    {
      [PreserveSig]
      HResult GetImage( [In, MarshalAs( UnmanagedType.Struct )] NativeSize size, [In] ThumbnailOptions flags, [Out] out IntPtr phbm ) ;
    }

    [StructLayout( LayoutKind.Sequential )]
    internal struct NativeSize
    {
      private int width ;
      private int height ;

      public int Width
      {
        set => width = value ;
      }

      public int Height
      {
        set => height = value ;
      }
    } ;

    [StructLayout( LayoutKind.Sequential )]
    public struct Rgbquad
    {
      public readonly byte rgbBlue ;
      public readonly byte rgbGreen ;
      public readonly byte rgbRed ;
      public readonly byte rgbReserved ;
    }

    public static Bitmap GetThumbnail( string fileName, int width, int height, ThumbnailOptions options )
    {
      var hBitmap = GetHBitmap( Path.GetFullPath( fileName ), width, height, options ) ;

      try {
        return GetBitmapFromHBitmap( hBitmap ) ;
      }
      finally {
        DeleteObject( hBitmap ) ;
      }
    }

    public static Bitmap GetBitmapFromHBitmap( IntPtr nativeHBitmap )
    {
      var bmp = Image.FromHbitmap( nativeHBitmap ) ;
      return Image.GetPixelFormatSize( bmp.PixelFormat ) < 32 ? bmp : CreateAlphaBitmap( bmp, PixelFormat.Format32bppArgb ) ;
    }

    public static Bitmap CreateAlphaBitmap( Bitmap srcBitmap, PixelFormat targetPixelFormat )
    {
      var result = new Bitmap( srcBitmap.Width, srcBitmap.Height, targetPixelFormat ) ;
      var bmpBounds = new Rectangle( 0, 0, srcBitmap.Width, srcBitmap.Height ) ;
      var srcData = srcBitmap.LockBits( bmpBounds, ImageLockMode.ReadOnly, srcBitmap.PixelFormat ) ;
      var isAlplaBitmap = false ;

      try {
        for ( var y = 0 ; y <= srcData.Height - 1 ; y++ ) {
          for ( var x = 0 ; x <= srcData.Width - 1 ; x++ ) {
            var pixelColor = Color.FromArgb( Marshal.ReadInt32( srcData.Scan0, ( srcData.Stride * y ) + ( 4 * x ) ) ) ;

            if ( pixelColor.A > 0 & pixelColor.A < 255 ) {
              isAlplaBitmap = true ;
            }

            result.SetPixel( x, y, pixelColor ) ;
          }
        }
      }
      finally {
        srcBitmap.UnlockBits( srcData ) ;
      }

      return isAlplaBitmap ? result : srcBitmap ;
    }

    private static IntPtr GetHBitmap( string fileName, int width, int height, ThumbnailOptions options )
    {
      var shellItemGuid = new Guid( ShellItemGuid ) ;
      var retCode = SHCreateItemFromParsingName( fileName, IntPtr.Zero, ref shellItemGuid, out var nativeShellItem ) ;

      if ( retCode != 0 )
        throw Marshal.GetExceptionForHR( retCode ) ;

      var nativeSize = new NativeSize { Width = width, Height = height } ;
      var hr = ( (IShellItemImageFactory) nativeShellItem ).GetImage( nativeSize, options, out var hBitmap ) ;
      Marshal.ReleaseComObject( nativeShellItem ) ;

      if ( hr == HResult.Ok ) 
        return hBitmap ;
      
      throw Marshal.GetExceptionForHR( (int) hr ) ;
    }
  }
}