using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace Grant_Searcher
{
    public partial class MainWindow : Window
    {
        private readonly GrantService _grantService;

        public MainWindow()
        {
            InitializeComponent();
            _grantService = new GrantService();
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled = false;
            UpdateButton.Content = "Updating...";
            SearchProgressBar.Visibility = Visibility.Visible;

            try
            {
                await DownloadAndExtractDataAsync();
                MessageBox.Show("Data update completed successfully.", "Update Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while updating data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateButton.IsEnabled = true;
                UpdateButton.Content = "Update Data";
                SearchProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async Task DownloadAndExtractDataAsync()
        {
            var dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            var zipFilePath = Path.Combine(dataFolder, "GrantsDBExtract.zip");
            var xmlFileName = "gov_grants.xml";
            var xmlFilePath = Path.Combine(dataFolder, xmlFileName);

            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            using (var httpClient = new HttpClient())
            {
                DateTime date = DateTime.Today;
                DateTime minDate = date.AddDays(-30);

                bool fileDownloaded = false;

                while (date >= minDate)
                {
                    string dateString = date.ToString("yyyyMMdd");
                    var dataUrl = $"https://prod-grants-gov-chatbot.s3.amazonaws.com/extracts/GrantsDBExtract{dateString}v2.zip";

                    try
                    {
                        using (var response = await httpClient.GetAsync(dataUrl))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                using (var fs = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await response.Content.CopyToAsync(fs);
                                }
                                fileDownloaded = true;
                                break;
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                date = date.AddDays(-1);
                                continue;
                            }
                            else
                            {
                                throw new Exception($"Failed to download data: {response.StatusCode} {response.ReasonPhrase}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error downloading file from {dataUrl}: {ex.Message}", ex);
                    }
                }

                if (!fileDownloaded)
                {
                    throw new Exception("Failed to download data file. No data files found in the specified date range.");
                }
            }

            if (File.Exists(zipFilePath))
            {
                if (File.Exists(xmlFilePath))
                {
                    File.Delete(xmlFilePath);
                }

                using (var zip = ZipFile.OpenRead(zipFilePath))
                {
                    var entry = zip.Entries.FirstOrDefault();
                    if (entry != null)
                    {
                        var tempXmlFilePath = Path.Combine(dataFolder, entry.Name);
                        entry.ExtractToFile(tempXmlFilePath);

                        File.Move(tempXmlFilePath, xmlFilePath);
                    }
                    else
                    {
                        throw new Exception("No entries found in the zip file.");
                    }
                }

                File.Delete(zipFilePath);
            }
            else
            {
                throw new FileNotFoundException("Downloaded zip file not found.");
            }
        }


        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var orgInfo = new OrganizationInfo
            {
                Name = NameTextBox.Text.Trim(),
                Mission = MissionTextBox.Text.Trim(),
                Geography = GeographyTextBox.Text.Trim(),
                AwardCeiling = AwardCeilingTextBox.Text.Trim(),
                AwardFloor = AwardFloorTextBox.Text.Trim(),
                Agency = AgencyTextBox.Text.Trim()
            };

            if (string.IsNullOrEmpty(orgInfo.Mission))
            {
                MessageBox.Show("Please fill in the required fields: Mission.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SearchButton.IsEnabled = false;
            SearchProgressBar.Visibility = Visibility.Visible;
            GrantsDataGrid.ItemsSource = null;

            try
            {
                var grants = await _grantService.FindGrantsAsync(orgInfo);

                if (grants.Count == 0)
                {
                    MessageBox.Show("No grants found.", "Result", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    GrantsDataGrid.ItemsSource = grants;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while searching for grants: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SearchButton.IsEnabled = true;
                SearchProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open the link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
