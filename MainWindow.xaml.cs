using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SCRAPLauncher
{
    public partial class MainWindow : Window
    {
        private
        const string GitHubReleasesApiUrl = "https://api.github.com/repos/QuantumLeap-Studios/SCRAP_GameFiles/releases";
        private
        const string UserAgent = "SCRAPLauncher";
        private string _downloadUrl;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var releases = await FetchReleasesAsync();
                var latestRelease = releases.FirstOrDefault(r => !string.IsNullOrEmpty(r.name) && r.name.ToLower().Contains("(release)"));

                if (latestRelease != null)
                {
                    _downloadUrl = latestRelease.assets.FirstOrDefault()?.browser_download_url;
                    InstallGameButton.IsEnabled = !string.IsNullOrEmpty(_downloadUrl);
                    MessageBox.Show($"Latest release: {latestRelease.name}\nPublished at: {latestRelease.published_at}", "Update Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No releases found.", "Update Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void InstallGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_downloadUrl))
            {
                MessageBox.Show("No valid download URL found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                DownloadProgressBar.Visibility = Visibility.Visible;
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var zipFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SCRAP.zip");
                    var extractPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SCRAP");

                    using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var totalBytes = response.Content.Headers.ContentLength ?? 1;
                        var buffer = new byte[8192];
                        var bytesRead = 0L;
                        var stream = await response.Content.ReadAsStreamAsync();

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, (int)bytesRead);
                            DownloadProgressBar.Value = (double)fileStream.Length / totalBytes * 100;
                        }
                    }

                    // Replace the problematic code with the following to ensure the directory path is valid and properly handled.

                    await Task.Delay(120);

                    // Ensure the extractPath is valid and exists
                    if (Directory.Exists(extractPath))
                    {
                        Directory.Delete(extractPath, true);
                    }

                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                    }

                    await Task.Delay(120);
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                    await Task.Delay(120);

                    // Ensure the zip file exists before attempting to delete it
                    if (File.Exists(zipFilePath))
                    {
                        File.Delete(zipFilePath); // Delete the zip file after extraction
                    }

                    await Task.Delay(120);

                    MessageBox.Show($"Game downloaded and extracted successfully to {extractPath}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadProgressBar.Visibility = Visibility.Collapsed;
                DownloadProgressBar.Value = 0;
            }
        }

        public void LaunchGame()
        {
            try
            {
                var gameExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SCRAP", "SCRAP.exe");

                if (File.Exists(gameExecutablePath))
                {
                    System.Diagnostics.Process.Start(gameExecutablePath);
                }
                else
                {
                    MessageBox.Show("Game executable not found. Please ensure the game is installed correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async Task<List<Release>> FetchReleasesAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent); // Add User-Agent header to avoid GitHub API rejection
                var response = await client.GetAsync(GitHubReleasesApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to fetch releases. Status code: {response.StatusCode}");
                }

                Console.WriteLine(response.ToString()); // Log headers for debugging

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent.ToString()); // Log headers for debugging
                return JsonSerializer.Deserialize<List<Release>>(responseContent);
            }
        }
        private async void VersionSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (VersionSelector.SelectedItem == null)
                {
                    return;
                }

                var selectedRelease = VersionSelector.SelectedItem as Release;
                if (selectedRelease != null)
                {
                    _downloadUrl = selectedRelease.assets?.FirstOrDefault()?.browser_download_url;
                    InstallGameButton.IsEnabled = !string.IsNullOrEmpty(_downloadUrl);

                    Console.WriteLine($"Selected version: {selectedRelease.name}");

                    // Only show the message box if this was triggered by user interaction
                    if (e.AddedItems.Count > 0)
                    {
                        MessageBox.Show(
                $"Selected version: {selectedRelease.name}\nPublished at: {selectedRelease.published_at}",
                          "Version Info",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information
                        );
                    }
                }
                else
                {
                    _downloadUrl = null;
                    InstallGameButton.IsEnabled = false;
                    MessageBox.Show("Invalid version selected.", "Version Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _downloadUrl = null;
                InstallGameButton.IsEnabled = false;
                MessageBox.Show($"Error selecting version: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingText.Text = "Loading versions...";
                var releases = await FetchReleasesAsync();
                Console.WriteLine($"Releases fetched: {releases?.Count}");
                if (releases != null && releases.Any())
                {
                    VersionSelector.ItemsSource = releases;
                    VersionSelector.DisplayMemberPath = "name";
                    LoadingText.Text = "Select a version:";
                }
                else
                {
                    LoadingText.Text = "No versions available.";
                }
            }
            catch (Exception ex)
            {
                LoadingText.Text = "Error loading versions.";
                MessageBox.Show($"Error loading versions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SCRAP")))
            {
                PlayButton.IsEnabled = true;
            }
            else
            {
                PlayButton.IsEnabled = false;
            }
        }

        private class Release
        {
            public string name
            {
                get;
                set;
            }
            public string published_at
            {
                get;
                set;
            }
            public Asset[] assets
            {
                get;
                set;
            }
        }

        private class Asset
        {
            public string browser_download_url
            {
                get;
                set;
            }
        }

        private void PlayGameButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchGame();
            Application.Current.Shutdown();
        }
    }
}