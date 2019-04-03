using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lab01
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string filename;
        ObservableCollection<Person> people = new ObservableCollection<Person>();


        public ObservableCollection<Person> Items
        {
            get => people;
        }

        public object ProgresChanged { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AddNewPersonButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                people.Add(new Person { Age = int.Parse(ageTextBox.Text), Name = nameTextBox.Text, Filename = filename });
            }
            catch (FormatException)
            {
                throw new FormatException("Age have to be a number");
            }


        }

        private void AddPictureButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            // fileDialog.DefaultExt = ".jpg";
            if (fileDialog.ShowDialog() == true)
            {
                filename = fileDialog.FileName;
            }



        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ShowPictureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapImage myBitMap = new BitmapImage();
                myBitMap.BeginInit();
                myBitMap.UriSource = new Uri(filename);
                myBitMap.DecodePixelWidth = 200;
                myBitMap.EndInit();
                myImage.Source = myBitMap;
            }
            catch (ArgumentNullException)
            {
                throw new Exception("You have to load image");
            }
        }

        private async void AddTextButton_Click(object sender, RoutedEventArgs e)
        {
            Progress<int> progress = new Progress<int>();
            progress.ProgressChanged += ReportProgress;
            PersonalInfo personal = await GetApiAsync("https://uinames.com/api/?ext",progress);
        }

        async Task<PersonalInfo> GetApiAsync(string path, IProgress<int> progress)
        {
            int report = new int();
            PersonalInfo personal = null;
            int levelmax = 10;
            int presentLevel = 0;
            while(presentLevel<=10)
            {
                presentLevel++;
                report = (presentLevel * 100) / levelmax;
                progress.Report(report);
                using (HttpClient client = new HttpClient())
                {

                    using (HttpResponseMessage response = await client.GetAsync(path))
                    {
                        using (HttpContent content = response.Content)
                        {
                            var stringContent = await content.ReadAsStringAsync();
                            personal = JsonConvert.DeserializeObject<PersonalInfo>(stringContent);
                            
                                                        
                            people.Add(new Person { Age = personal.age, Name = personal.name, Filename = personal.photo });
                        }
                    }
                }
                await Task.Delay(1000);               
            }

            return personal;

        }

        private void ReportProgress(object sender, int e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = e;
                });
            }
            catch { }
        }

        public class PersonalInfo
        {
            public string name { get; set; }
            public int age { get; set; }
            public string photo { get; set; }

        }



    }
}