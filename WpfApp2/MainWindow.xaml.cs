using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using WpfApp2;

namespace Lab01
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker = new BackgroundWorker();

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

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
        }

        public void AddPerson(Person person)
        {
            Application.Current.Dispatcher.Invoke(() => { Items.Add(person); });
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
            PersonalInfo personal = await GetApiAsync("https://uinames.com/api/?ext", progress);
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

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            weatherDataProgressBar.Value = e.ProgressPercentage;
            weatherDataTextBlock.Text = e.UserState as string;
        }

        private async void LoadWeatherData(object sender, RoutedEventArgs e)
        {
            string responseXML = await WeatherConnection.LoadDataAsync("London");
            WeatherDataEntry result;

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseXML)))
            {
                result = ParseWeather_LINQ.Parse(stream);
                Items.Add(new Person()
                {
                    Name = result.City,
                    Age = (int)Math.Round(result.Temperature)
                });
            }

            if (worker.IsBusy != true)
                worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<string> cities = new List<string> {
                "London", "Warsaw", "Paris", "London", "Warsaw" };
            for (int i = 1; i <= cities.Count; i++)
            {
                string city = cities[i - 1];

                if (worker.CancellationPending == true)
                {
                    worker.ReportProgress(0, "Cancelled");
                    e.Cancel = true;
                    return;
                }
                else
                {
                    worker.ReportProgress(
                        (int)Math.Round((float)i * 100.0 / (float)cities.Count),
                        "Loading " + city + "...");
                    string responseXML = WeatherConnection.LoadDataAsync(city).Result;
                    WeatherDataEntry result;

                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseXML)))
                    {
                        result = ParseWeather_XmlReader.Parse(stream);
                        AddPerson(
                            new Person()
                            {
                                Name = result.City,
                                Age = (int)Math.Round(result.Temperature)
                            });
                    }
                    Thread.Sleep(2000);
                }
            }
            worker.ReportProgress(100, "Done");
        }

        private async void LoadCityTemp_Click(object sender, RoutedEventArgs e)
        {
            string city = cityTextBox.Text;
            string responseXML = await WeatherConnection.LoadDataAsync(city);
            WeatherDataEntry result;

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseXML)))
            {
                result = ParseWeather_LINQ.Parse(stream);
                Items.Add(new Person()
                {
                    Name = result.City,
                    Age = (int)Math.Round(result.Temperature)
                });
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (worker.WorkerSupportsCancellation == true)
            {
                weatherDataTextBlock.Text = "Cancelling...";
                worker.CancelAsync();
            }
        }

        public class PersonalInfo
        {
            public string name { get; set; }
            public int age { get; set; }
            public string photo { get; set; }

        }



    }
}