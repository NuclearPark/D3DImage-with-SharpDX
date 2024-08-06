using Lges.Dnc.CuttingVision.SocketTest;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using SharpDX.WIC;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Direct3D direct3D;
        //private Device device;
        private Texture texture;
        private D3DImage d3dImage;

        public MainWindow()
        {
            InitializeComponent();
            SocketHelper socketHelper = new SocketHelper();
            socketHelper.onPacketReceived += SocketHelper_onPacketReceived;
            Loaded += MainWindow_Loaded;
        }

        private void SocketHelper_onPacketReceived(IMsgVisionToHost msg, int width, int height, int size, int messageItem, int message, int result, int reasonCode, int port)
        {

            d3dImage.AddDirtyRect(new Int32Rect(0, 0, 800, 600));
            img.Source = d3dImage;
            img.Width = d3dImage.Width;
            img.Height = d3dImage.Height;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDirect3D();
            LoadJpegToTexture(@"sample.bmp");
        }

        private void InitializeDirect3D()
        {
            //SharpDX.Direct3D9.Direct3DEx  d9context = new SharpDX.Direct3D9.Direct3DEx();

            //device = new SharpDX.Direct3D9.Device(d9context,
            //                                                                 0,
            //                                                                 DeviceType.Hardware,
            //                                                                 IntPtr.Zero,
            //                                                                 CreateFlags.HardwareVertexProcessing,
            //                                                                 new SharpDX.Direct3D9.PresentParameters()
            //                                                                 {
            //                                                                     Windowed = true,
            //                                                                     SwapEffect = SharpDX.Direct3D9.SwapEffect.Discard,
            //                                                                     DeviceWindowHandle = new WindowInteropHelper(this).Handle,
            //                                                                     PresentationInterval = PresentInterval.Default,
            //                                                                 });


        }

        public static Texture2D CreateTexture2DFrombytes(SharpDX.Direct3D11.Device device, byte[] RawData, int width, int height)
        {
            Texture2DDescription desc;
            desc.Width = width;
            desc.Height = height;
            desc.ArraySize = 1;
            desc.BindFlags = BindFlags.ShaderResource;
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = CpuAccessFlags.None;
            desc.Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
            desc.MipLevels = 1;
            desc.OptionFlags = ResourceOptionFlags.None;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            DataStream s = DataStream.Create(RawData, true, true);
            DataRectangle rect = new DataRectangle(s.DataPointer, width * 4);
            Texture2D t2D = new Texture2D(device, desc, rect);
            return t2D;
        }

        byte[] ConvertBitmapToByteArray(System.Drawing.Bitmap bitmap) { byte[] result = null; if (bitmap != null) { MemoryStream stream = new MemoryStream(); bitmap.Save(stream, bitmap.RawFormat); result = stream.ToArray(); } else { Console.WriteLine("Bitmap is null."); } return result; }


        private void LoadJpegToTexture(string filePath)
        {
            // Load the JPEG into a Bitmap
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(filePath))
            {

                SharpDX.Direct3D11.Device device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.None | DeviceCreationFlags.BgraSupport, SharpDX.Direct3D.FeatureLevel.Level_11_0);
                SharpDX.Direct3D11.DeviceContext d11dContext = device.ImmediateContext;

                byte[] byteArray = ConvertBitmapToByteArray(bitmap);
                MemoryStream memoryStream = new MemoryStream(byteArray);                
                using (var imagingFactory = new SharpDX.WIC.ImagingFactory2())
                {
                    var decoder = new SharpDX.WIC.BitmapDecoder(imagingFactory, memoryStream, DecodeOptions.CacheOnLoad);
                    var frame = decoder.GetFrame(0);
                    var formatConverter = new FormatConverter(imagingFactory);
                    formatConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPBGRA);

                    var width = formatConverter.Size.Width;
                    var height = formatConverter.Size.Height;

                    var textureDesc = new Texture2DDescription
                    {
                        Width = width,
                        Height = height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Dynamic,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.Write,

                    };

                    var d3dTexture_Draw = new Texture2D(device, textureDesc);

                    // Copy data to Texture2D
                    var dataBox = d11dContext.MapSubresource(d3dTexture_Draw, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                    using (var wicStream = new DataStream(dataBox.DataPointer, width * height * 4, true, true))
                    {
                        formatConverter.CopyPixels(width * 4, wicStream);
                    }
                    d11dContext.UnmapSubresource(d3dTexture_Draw, 0);


                    var final_descriptor = new Texture2DDescription()
                    {
                        Width = (int)img.Width,
                        Height = (int)img.Height,
                        ArraySize = 1,
                        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                        Usage = ResourceUsage.Default,
                        CpuAccessFlags = CpuAccessFlags.None,
                        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        MipLevels = 1,
                        OptionFlags = ResourceOptionFlags.Shared,
                        SampleDescription = new SampleDescription(1, 0)
                    };

                    Texture2D _finalTexture = new Texture2D(device, final_descriptor);

                    d11dContext.CopyResource(d3dTexture_Draw, _finalTexture);

                    SharpDX.Direct3D9.Direct3DEx d9context = new SharpDX.Direct3D9.Direct3DEx();
                    SharpDX.Direct3D9.Device d9device = new SharpDX.Direct3D9.Device(d9context,
                                                                        0,
                                                                        DeviceType.Hardware,
                                                                        IntPtr.Zero,
                                                                        CreateFlags.HardwareVertexProcessing,
                                                                        new SharpDX.Direct3D9.PresentParameters()
                                                                        {
                                                                            Windowed = true,
                                                                            SwapEffect = SharpDX.Direct3D9.SwapEffect.Discard,
                                                                            DeviceWindowHandle = new WindowInteropHelper(this).Handle,
                                                                            PresentationInterval = PresentInterval.Immediate,
                                                                        });

                    IntPtr renderTextureHandle = _finalTexture.QueryInterface<SharpDX.DXGI.Resource>().SharedHandle;

                    SharpDX.Direct3D9.Texture d9texture = new SharpDX.Direct3D9.Texture(d9device,
                                                                           _finalTexture.Description.Width,
                                                                           _finalTexture.Description.Height,
                                                                           1,
                                                                           SharpDX.Direct3D9.Usage.RenderTarget,
                                                                           SharpDX.Direct3D9.Format.A8R8G8B8,
                                                                           Pool.Default,
                                                                           ref renderTextureHandle);
                    
                    d3dImage = new D3DImage();
                    d3dImage.Lock();
                    
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, d9texture.GetSurfaceLevel(0).NativePointer, true);
                    d3dImage.AddDirtyRect(new Int32Rect(0, 0, 720, 370));
                    d3dImage.Unlock();
                    img.Source= d3dImage;
                }
            }
        }

    }
}