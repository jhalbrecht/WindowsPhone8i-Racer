using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using EightRacer.Resources;

namespace EightRacer
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void AboutPrompt_Click(object sender, EventArgs e)
        {
            // I got lazy with my MVVM.... :-(

            AboutPrompt about = new AboutPrompt();
            //about.Completed += about_Completed;
            about.Title = "i-Racer";
            about.VersionNumber = "Version 1.0";
            about.Footer = @"(c) 2013 appDevPro";
            // about.WaterMark = "Applications Development Professionals";
            about.Body =
                new TextBlock
                {
                    Text =
                        "Control the Dagu i-Racer Bluetooth remote control car\n\nWritten by @jhalbrecht @AppDevPro\n\nCheck out http://www.appdevpro.com",
                    TextWrapping = TextWrapping.Wrap
                };

            //AboutPersonItem item = new AboutPersonItem();
            //item.Author = "jeffa"; 

            // about.Show(@"@jhalbrecht", "@appdevpro", "na@dev.null", @"http://www.AppDevPro.com");
            about.Show(); 
        }


        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
    //public class AboutPersonItem
    //{
    //    public string Author { get; set; }
    //}

}