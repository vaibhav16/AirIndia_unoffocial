﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace AirIndia_unofficial
{
    public partial class baggage : PhoneApplicationPage
    {
        private string _homeURL = "http://www.airindia.in/checked-baggage-allowances.htm";
        private Uri _deactivatedURL;
        private IsolatedStorageSettings _userSettings = IsolatedStorageSettings.ApplicationSettings;
        private ProgressIndicator _progressIndicator;
        public baggage()
        {
            InitializeComponent();
            _progressIndicator = new ProgressIndicator();
            _progressIndicator.IsIndeterminate = true;
            _progressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, _progressIndicator);

            // Event handler for the hardware back key
            BackKeyPress += aptz_BackKeyPress;

            // Fast app resume events
            PhoneApplicationService.Current.Deactivated += Current_Deactivated;
            PhoneApplicationService.Current.Closing += Current_Closing;
        }


        #region App Navigation Events

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Browser event handlers
            Browser.Navigating += Browser_Navigating;
            Browser.Navigated += Browser_Navigated;
            Browser.NavigationFailed += Browser_NavigationFailed;

            Browser.IsScriptEnabled = true;

            // Try to get the URL stored for fast app resume.
            try
            {
                _deactivatedURL = (Uri)(_userSettings["deactivatedURL"]);
            }
            catch (System.Collections.Generic.KeyNotFoundException keyNotFound)
            {
                Debug.WriteLine(keyNotFound.Message);
            }

            // Were we started from a pinned tile?
            if (NavigationContext.QueryString.ContainsKey("StartURL"))
            {
                // Navigate to the pinned page.
                Browser.Navigate(new Uri(NavigationContext.QueryString["StartURL"], UriKind.RelativeOrAbsolute));
            }
            else if ((_deactivatedURL != null) && (e.NavigationMode != NavigationMode.Reset))
            {
                // If there is a stored URL from our last 
                // session being deactivated, navigate there
                if (Browser.Source != _deactivatedURL)
                {
                    Browser.Navigate(_deactivatedURL);
                }
            }
            else
            {
                // Not launched from a pinned tile...
                // No stored URL from the last time the app was deactivated...
                // So, just navigate to the home page
                Browser.Navigate(new Uri(_homeURL, UriKind.RelativeOrAbsolute));
            }
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Browser.Navigating -= Browser_Navigating;
            Browser.Navigated -= Browser_Navigated;
            Browser.NavigationFailed -= Browser_NavigationFailed;
        }


        // Handle the hardware Back button.
        void aptz_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Browser.CanGoBack)
            {
                e.Cancel = true;
                Browser.GoBack();
                _progressIndicator.IsVisible = true;
            }
        }


        // Clears the last deactivated URL in storage
        void Current_Closing(object sender, ClosingEventArgs e)
        {
            try
            {
                _userSettings.Remove("deactivatedURL");
            }
            catch (KeyNotFoundException keyNotFound)
            {
                Debug.WriteLine(keyNotFound.Message);
            }
        }


        // Stores the current URL so the app can resume to that page
        void Current_Deactivated(object sender, DeactivatedEventArgs e)
        {
            try
            {
                _userSettings.Remove("deactivatedURL");
            }
            catch (KeyNotFoundException keyNotFound)
            {
                Debug.WriteLine(keyNotFound.Message);
            }

            // Persist last URL in storage
            _userSettings.Add("deactivatedURL", _deactivatedURL);
        }

        #endregion


        #region Browser Events

        void Browser_Navigating(object sender, NavigatingEventArgs e)
        {
            // If the URL is a telephone number, turn off the progess indicator
            if (!e.Uri.Scheme.Contains("tel"))
            {
                _progressIndicator.IsVisible = true;
            }
        }


        void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // Save for fast resume
            _deactivatedURL = e.Uri;

            // We have arrived at a new page, 
            // hide the progress indicator
            _progressIndicator.IsVisible = false;
        }


        // Handle navigation failures
        private void Browser_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            MessageBox.Show("Navigation to this page failed. Please ensure that you have a working internet connection");
        }

        #endregion


        #region AppBar/Menu Events

        // Navigates to the web site home page
        private void HomeApplicationBar_Click(object sender, EventArgs e)
        {
            if (Browser.Source.ToString() != _homeURL)
            {
                Browser.Navigate(new Uri(_homeURL, UriKind.RelativeOrAbsolute));
            }
        }


        // Reloads the current page in the web browser control
        private void RefreshApplicationBar_Click(object sender, EventArgs e)
        {
            // Reload the current page.
            if (Browser.Source != null)
            {
                Browser.Navigate(Browser.Source);
            }
        }



        // Creates a Tile for the current page and pins it to the start screen
        private void PinApplicationBar_Click(object sender, EventArgs e)
        {
            FlipTileData tileData = new FlipTileData();

            // Use these variables to search the existing Tiles so we don't pin the same page twice.
            ShellTile currentTile = ShellTile.ActiveTiles.First();
            String currentURI = currentTile.NavigationUri.ToString();
            String browserSource = Browser.Source.ToString();
            int activeTilesCount = ShellTile.ActiveTiles.Count();
            Boolean tileFound = false;

            int activeTilesIndex = 0;

            // Search the active Tiles to see if the page is already pinned.
            while (activeTilesIndex < activeTilesCount)
            {
                if (currentURI.Contains(browserSource))
                {
                    // Found the current page in the collection of pinned tiles.
                    tileFound = true;
                    break;
                }
                else
                {
                    if (++activeTilesIndex < activeTilesCount)
                    {
                        currentTile = ShellTile.ActiveTiles.ElementAt(activeTilesIndex);
                        currentURI = currentTile.NavigationUri.ToString();
                    }
                }
            }

            // The page is already pinned
            if (tileFound)
            {
                // Show an error message and return.
                MessageBox.Show("This page is already pinned.");
                return;
            }

            // Uses the current date and time to create a unique title for this tile.
            // You'll want to use a title that better reflects your app and web site pages.
            tileData.Title = DateTime.Now.ToString();

            // This is only an example. You must insert your own Tile image.
            tileData.BackgroundImage = new Uri("/Assets/Tiles/PinnedTile.png", UriKind.Relative);

            try
            {
                ShellTile.Create(new Uri("/review.xaml?StartURL=" + Browser.Source.ToString(), UriKind.Relative), tileData, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }


        // Invokes a ShareLinkTask for sharing the current URL on social media
        private void ShareApplicationBar_Click(object sender, EventArgs e)
        {
            ShareLinkTask shareLinkTask = new ShareLinkTask();

            shareLinkTask.Title = "Air India";
            shareLinkTask.LinkUri = Browser.Source;
            shareLinkTask.Message = "Amazing app for your Windows Phone!";

            shareLinkTask.Show();
        }

        #endregion

    }
}
