﻿#if WINDOWS_UWP
using Prism.Windows.Mvvm;
using Prism.Commands;
using Prism.Windows.Navigation;
#else
using Microsoft.Practices.Prism.StoreApps;
using Microsoft.Practices.Prism.StoreApps.Interfaces;
#endif

using ModernDev.InTouch;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using VKSaver.Core.Models.Common;
using VKSaver.Core.Models.Player;
using VKSaver.Core.Services.Interfaces;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace VKSaver.Core.ViewModels
{
    [ImplementPropertyChanged]
    public sealed class TrackLyricsViewModel : ViewModelBase
    {
        public TrackLyricsViewModel(InTouch inTouch, INavigationService navigationService,
            IDialogsService dialogService, ILocService locService, IInTouchWrapper inTouchWrapper)
        {
            _inTouch = inTouch;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _locService = locService;
            _inTouchWrapper = inTouchWrapper;

            ReloadLyricsCommand = new DelegateCommand(OnReloadLyricsCommand);
        }

        public string Lyrics { get; private set; }

        public ImageSource ArtistImage { get; private set; }

        public ContentState LyricsState { get; private set; }

        public IPlayerTrack Track { get; private set; }

        [DoNotNotify]
        public DelegateCommand ReloadLyricsCommand { get; private set; }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (viewModelState.Count > 0)
            {
                Lyrics = viewModelState[nameof(Lyrics)].ToString();
                Track = JsonConvert.DeserializeObject<PlayerTrack>(
                    viewModelState[nameof(Track)].ToString());
                _artistImage = viewModelState[nameof(_artistImage)] as string;

                if (_artistImage != null)
                    ArtistImage = new BitmapImage(new Uri(_artistImage));
            }
            else if (e.Parameter != null)
            {
                var data = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(e.Parameter.ToString());                
                Track = JsonConvert.DeserializeObject<PlayerTrack>(data.Key);

                if (data.Value != null)
                {
                    ArtistImage = new BitmapImage(new Uri(data.Value));
                    _artistImage = data.Value;
                }
            }

            if (String.IsNullOrEmpty(Lyrics))
                LoadLyrics();

            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                viewModelState[nameof(Track)] = JsonConvert.SerializeObject(Track);
                viewModelState[nameof(Lyrics)] = Lyrics;
                viewModelState[nameof(_artistImage)] = _artistImage;
            }

            LyricsState = ContentState.None;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private void OnReloadLyricsCommand()
        {
            LoadLyrics();
        }

        private async void LoadLyrics()
        {
            if (Track.VKInfo != null && Track.VKInfo.LyricsID == 0)
            {
                ShowError();
                return;
            }

            LyricsState = ContentState.Loading;

            try
            {
                var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Audio.GetLyrics(Track.VKInfo.LyricsID));
                if (!response.IsError)
                {
                    if (String.IsNullOrWhiteSpace(response.Data.Text))
                    {
                        ShowError();
                        return;
                    }

                    Lyrics = response.Data.Text;
                    LyricsState = ContentState.Normal;
                    return;
                }
            }
            catch (Exception) { }

            LyricsState = ContentState.Error;
        }

        private void ShowError()
        {
            _dialogService.Show(_locService["Message_CantFindLyrics_Text"], 
                _locService["Message_CantFindLyrics_Title"]);
            LyricsState = ContentState.NoData;
        }

        private string _artistImage;
        
        private readonly INavigationService _navigationService;
        private readonly IDialogsService _dialogService;
        private readonly ILocService _locService;
        private readonly InTouch _inTouch;
        private readonly IInTouchWrapper _inTouchWrapper;
    }
}
