using System.Windows;

namespace AudioCat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        // Code that accepts drag and drop files
        private void OnDataGridDrop(object sender, DragEventArgs e)
        {
            var fileNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (fileNames == null || fileNames.Length == 0) 
                return;
            var vm = (MainViewModel)DataContext;
            fileNames = fileNames.OrderBy(s => s).ToArray();
            vm.Files.Clear();
            foreach (var file in fileNames)
                vm.Files.Add(new AudioFile(file));
        }
    }
}