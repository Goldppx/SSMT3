using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SSMT_Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SSMT
{
    // DdsFormat.cs
    public enum DdsFormat
    {
        BC1_UNORM,      // DXT1
        BC2_UNORM,      // DXT3
        BC3_UNORM,      // DXT5
        BC4_UNORM,      // ATI1
        BC4_SNORM,
        BC5_UNORM,      // ATI2
        BC5_SNORM,
        BC6H_UF16,      // BC6H unsigned
        BC6H_SF16,      // BC6H signed
        BC7_UNORM,      // BC7
        BC7_UNORM_SRGB,
        R8G8B8A8_UNORM,
        R8G8B8A8_UNORM_SRGB,
        B8G8R8A8_UNORM,
        B8G8R8A8_UNORM_SRGB,
        R16G16B16A16_FLOAT,
        R32G32B32A32_FLOAT
    }

    // DdsDimension.cs
    public enum DdsDimension
    {
        Texture2D,
        TextureCube,
        TextureArray
    }

    // MipmapOption.cs
    public enum MipmapOption
    {
        None,
        Generate,
        CustomCount
    }

    // AlphaMode.cs
    public enum AlphaMode
    {
        Unknown,
        Straight,
        Premultiplied,
        Opaque,
        Custom
    }

    // DdsConversionOptions.cs
    public class DdsConversionOptions
    {
        public DdsFormat Format { get; set; } = DdsFormat.BC7_UNORM;
        public DdsDimension Dimension { get; set; } = DdsDimension.Texture2D;
        public MipmapOption MipmapOption { get; set; } = MipmapOption.Generate;
        public int CustomMipCount { get; set; } = 0;
        public AlphaMode AlphaMode { get; set; } = AlphaMode.Unknown;
        public bool Srgb { get; set; } = false;
        public bool Fast { get; set; } = false;
        public bool SeamlessCubemap { get; set; } = false;
        public bool Wic { get; set; } = false;
        public bool UseDx10Header { get; set; } = true;
        public string OutputDirectory { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = false;
        public bool Recursive { get; set; } = false;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TextureToolBoxPage : Page
    {
        private DdsConversionOptions _conversionOptions;

        public TextureToolBoxPage()
        {
            this.InitializeComponent();


            _conversionOptions = new DdsConversionOptions();

            InitializeControls();
            //FilesListView.ItemsSource = _selectedFiles;

            ComboBox_SourcePictureFileFormat.Items.Add(".png");
            ComboBox_SourcePictureFileFormat.Items.Add(".jpg");
            ComboBox_SourcePictureFileFormat.Items.Add(".tga");

        }

        private async void Button_ChooseDynamicTextureFolderPath_Click(object sender, RoutedEventArgs e)
        {
            string selected_folder_path = await SSMTCommandHelper.ChooseFolderAndGetPath();
            if (selected_folder_path == "")
            {
                return;
            }

            TextBox_DynamicTextureFolderPath.Text = selected_folder_path;
        }


        private void Button_GenerateDynamicTextureMod_Click(object sender, RoutedEventArgs e)
        {
            string DynamicTextureFolderPath = TextBox_DynamicTextureFolderPath.Text + "\\";
            string TexturePrefix = TextBox_DynamicTextureName.Text;
            string TextureHash = TextBox_DynamicTextureHash.Text;
            string TextureSuffix = TextBox_DynamicTextureSuffix.Text;

            CoreFunctions.GenerateDynamicTextureMod(DynamicTextureFolderPath, TexturePrefix, TextureHash, TextureSuffix);

            SSMTCommandHelper.ShellOpenFolder(DynamicTextureFolderPath);
        }



        private void InitializeControls()
        {
            // 初始化格式下拉框
            FormatComboBox.ItemsSource = Enum.GetValues(typeof(DdsFormat)).Cast<DdsFormat>();
            FormatComboBox.SelectedItem = DdsFormat.BC7_UNORM;


            // 初始化Alpha模式下拉框
            AlphaModeComboBox.SelectedIndex = 0;

            // 事件绑定
            MipmapComboBox.SelectionChanged += MipmapComboBox_SelectionChanged;
        }
    


        private void MipmapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NumberBox_MipCount.Visibility = MipmapComboBox.SelectedIndex == 2 ?
                Visibility.Visible : Visibility.Collapsed;
        }

    

        private void ConvertFiles()
        {
            ConvertBtn.IsEnabled = false;

            // 更新转换选项
            UpdateConversionOptions();

            ConvertBtn.IsEnabled = true;
        }

        private async Task ConvertSingleFile(StorageFile inputFile, DdsConversionOptions options)
        {
            string outputPath = GetOutputPath(inputFile, options);
            string texconvPath = PathManager.Path_TexconvExe;

            if (!File.Exists(texconvPath))
            {
                throw new FileNotFoundException("texconv.exe 未找到");
            }

            string arguments = BuildTexConvArguments(inputFile.Path, outputPath, options);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = texconvPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"texconv 执行失败: {error}");
                }
            }
        }

        private string BuildTexConvArguments(string inputPath, string outputPath, DdsConversionOptions options)
        {
            var args = new List<string>
            {
                $"\"{inputPath}\"",
                $"-o \"{Path.GetDirectoryName(outputPath)}\"",
                $"-f {options.Format}",
                options.Srgb ? "-srgb" : "",
                options.Overwrite ? "-y" : "",
                options.Fast ? "-fast" : "",
                options.SeamlessCubemap ? "-seamless" : "",
                options.Wic ? "-wic" : "",
                options.UseDx10Header ? "-dx10" : ""
            };

            // Mipmap 参数
            switch (options.MipmapOption)
            {
                case MipmapOption.None:
                    args.Add("-m 1");
                    break;
                case MipmapOption.Generate:
                    args.Add("-m 0"); // 0 表示生成所有可能的mipmap
                    break;
                case MipmapOption.CustomCount:
                    args.Add($"-m {options.CustomMipCount}");
                    break;
            }

            // Alpha 模式参数
            if (options.AlphaMode != AlphaMode.Unknown)
            {
                args.Add($"-alpha {options.AlphaMode.ToString().ToLower()}");
            }

            // 维度参数
            if (options.Dimension != DdsDimension.Texture2D)
            {
                args.Add($"-{options.Dimension.ToString().ToLower()}");
            }

            return string.Join(" ", args.Where(arg => !string.IsNullOrEmpty(arg)));
        }

        private string GetOutputPath(StorageFile inputFile, DdsConversionOptions options)
        {
            string outputDir;

            if (options.OutputDirectory == "SameAsInput" )
            {
                outputDir = Path.GetDirectoryName(inputFile.Path);
            }
            else
            {
                outputDir = options.OutputDirectory;
            }

            string fileName = Path.GetFileNameWithoutExtension(inputFile.Name) + ".dds";
            return Path.Combine(outputDir, fileName);
        }



        private void UpdateConversionOptions()
        {
            _conversionOptions.Format = (DdsFormat)FormatComboBox.SelectedItem;
            _conversionOptions.Srgb = SrgbCheckBox.IsChecked ?? false;
            _conversionOptions.Fast = FastCheckBox.IsChecked ?? false;
            _conversionOptions.SeamlessCubemap = SeamlessCubemapCheckBox.IsChecked ?? false;
            _conversionOptions.Wic = WicCheckBox.IsChecked ?? false;
            _conversionOptions.UseDx10Header = Dx10HeaderCheckBox.IsChecked ?? true;

            // Mipmap 选项
            _conversionOptions.MipmapOption = MipmapComboBox.SelectedIndex switch
            {
                0 => MipmapOption.None,
                1 => MipmapOption.Generate,
                2 => MipmapOption.CustomCount,
                _ => MipmapOption.Generate
            };
            _conversionOptions.CustomMipCount = (int)NumberBox_MipCount.Value;

            // Alpha 模式
            _conversionOptions.AlphaMode = AlphaModeComboBox.SelectedIndex switch
            {
                0 => AlphaMode.Unknown,
                1 => AlphaMode.Straight,
                2 => AlphaMode.Premultiplied,
                3 => AlphaMode.Opaque,
                _ => AlphaMode.Unknown
            };

            
        }

   

        private void InitializeFilePicker(object picker)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            if (picker is FileOpenPicker filePicker)
            {
                WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
            }
            else if (picker is FolderPicker folderPicker)
            {
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            }
        }

        private async void ShowMessage(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }


    }
}
