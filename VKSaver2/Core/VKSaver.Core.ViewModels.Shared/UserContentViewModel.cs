﻿#if WINDOWS_UWP
using Prism.Windows.Mvvm;
using Prism.Commands;
using Prism.Windows.Navigation;
#else
using Microsoft.Practices.Prism.StoreApps;
using Microsoft.Practices.Prism.StoreApps.Interfaces;
#endif

using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKSaver.Core.ViewModels.Collections;
using Windows.UI.Xaml.Navigation;
using VKSaver.Core.Services.Interfaces;
using VKSaver.Core.ViewModels.Common;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using System.Collections.Specialized;
using VKSaver.Core.Models.Transfer;
using ModernDev.InTouch;
using VKSaver.Core.Services.Common;
using Windows.System;
using VKSaver.Core.Models.Common;
using VKSaver.Core.Services;

namespace VKSaver.Core.ViewModels
{
    [ImplementPropertyChanged]
    public sealed class UserContentViewModel : ViewModelBase
    {
        public UserContentViewModel(
            InTouch inTouch, 
            INavigationService navigationService,
            IPlayerService playerService, 
            IDownloadsServiceHelper downloadsServiceHelper,
            IAppLoaderService appLoaderService, 
            IVKLoginService vkLoginService,
            IDialogsService dialogsService, 
            ILocService locService, 
            IInTouchWrapper inTouchWrapper,
            ILaunchViewResolver launchViewResolver,
            IPurchaseService purchaseService)
        {
            _inTouch = inTouch;
            _navigationService = navigationService;
            _playerService = playerService;
            _downloadsServiceHelper = downloadsServiceHelper;
            _appLoaderService = appLoaderService;
            _vkLoginService = vkLoginService;
            _dialogsService = dialogsService;
            _locService = locService;
            _inTouchWrapper = inTouchWrapper;
            _launchViewResolver = launchViewResolver;
            _purchaseService = purchaseService;

            SelectedItems = new List<object>();
            PrimaryItems = new ObservableCollection<ICommandBarElement>();
            SecondaryItems = new ObservableCollection<ICommandBarElement>();

            ExecuteTracksListItemCommand = new DelegateCommand<object>(OnExecuteTracksListItemCommand);
            NotImplementedCommand = new DelegateCommand(() => _navigationService.Navigate("AccessDeniedView", null));
            DownloadItemCommand = new DelegateCommand<object>(OnDownloadItemCommand, CanExecuteDownloadItemCommand);
            ActivateSelectionMode = new DelegateCommand(SetSelectionMode, CanSelectionMode);
            ReloadContentCommand = new DelegateCommand(OnReloadContentCommand);
            DownloadSelectedCommand = new DelegateCommand(OnDownloadSelectedCommand, CanExecuteDownloadSelectedCommand);
            SelectionChangedCommand = new DelegateCommand(OnSelectionChangedCommand);
            SelectAllCommand = new DelegateCommand(OnSelectAllCommand, CanSelectionMode);

            AddToMyCollectionCommand = new DelegateCommand<object>(OnAddToMyCollection, CanAddToMyCollection);
            AddSelectedToMyCollectionCommand = new DelegateCommand(OnAddSelectedToMyCollection, CanAddSelected);
            PlaySelectedCommand = new DelegateCommand(OnPlaySelectedCommand, HasSelectedAudios);
            PlayShuffleCommand = new DelegateCommand(OnPlayShuffleCommand);

            DeleteCommand = new DelegateCommand<object>(OnDeleteCommand, CanDelete);
            DeleteSelectedCommand = new DelegateCommand(OnDeleteSelectedCommand, CanDeleteSelected);

            OpenTransferManagerCommand = new DelegateCommand(OnOpenTransferManagerCommand);
            OpenMainViewCommand = new DelegateCommand(OnOpenMainViewCommand);

            ShowTrackInfoCommand = new DelegateCommand<Audio>(OnShowTrackInfoCommand);
        }

        #region View positions

        public double AudiosScrollPosition { get; set; }
        public double VideosScrollPosition { get; set; }
        public double DocsScrollPosition { get; set; }

        #endregion

        public string PageTitle { get; private set; }

        public IncrementalLoadingJumpListCollection AudioGroup { get; private set; }

        public IncrementalLoadingJumpListCollection VideoGroup { get; private set; }

        public PaginatedCollection<Doc> Documents { get; private set; }        

        public bool IsSelectionMode { get; private set; }

        public bool IsItemClickEnabled { get; private set; }

        public bool IsLockedPivot { get; private set; }
        
        [DoNotCheckEquality]
        public bool SelectAllAudios { get; private set; }

        [DoNotCheckEquality]
        public bool SelectAllDocuments { get; private set; }

        [DoNotNotify]
        public ObservableCollection<ICommandBarElement> PrimaryItems { get; private set; }

        [DoNotNotify]
        public ObservableCollection<ICommandBarElement> SecondaryItems { get; private set; }

        [DoNotNotify]
        public List<object> SelectedItems { get; private set; }

        #region Команды

        [DoNotNotify]
        public DelegateCommand<object> ExecuteTracksListItemCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand<object> DownloadItemCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand DownloadSelectedCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand NotImplementedCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand ActivateSelectionMode { get; private set; }

        [DoNotNotify]
        public DelegateCommand ReloadContentCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand SelectionChangedCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand SelectAllCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand AddSelectedToMyCollectionCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand<object> AddToMyCollectionCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand PlaySelectedCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand PlayShuffleCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand DeleteSelectedCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand<object> DeleteCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand OpenTransferManagerCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand OpenMainViewCommand { get; private set; }

        [DoNotNotify]
        public DelegateCommand<Audio> ShowTrackInfoCommand { get; private set; }

        #endregion

        public int LastPivotIndex
        {
            get { return _lastPivotIndex; }
            set
            {
                _lastPivotIndex = value;
                SetDefaultMode();
                //ActivateSelectionMode.RaiseCanExecuteChanged();
            }
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            var parameter = JsonConvert.DeserializeObject<KeyValuePair<string, int>>(e.Parameter.ToString());
            _userID = parameter.Value;

            if (e.NavigationMode == NavigationMode.New)
            {
                switch (parameter.Key)
                {
                    case "audios":
                        LastPivotIndex = 0;
                        break;
                    case "videos":
                        LastPivotIndex = 1;
                        break;
                    case "docs":
                        LastPivotIndex = 2;
                        break;
                    default:
                        LastPivotIndex = 0;
                        break;
                }
            }

            if (viewModelState.Count > 0)
            {
                if (e.NavigationMode != NavigationMode.New)
                    LastPivotIndex = (int)viewModelState[nameof(LastPivotIndex)];

                PageTitle = (string)viewModelState[nameof(PageTitle)];

                _audioAlbumsOffset = (int)viewModelState[nameof(_audioAlbumsOffset)];
                _audiosOffset = (int)viewModelState[nameof(_audiosOffset)];
                _videoAlbumsOffset = (int)viewModelState[nameof(_videoAlbumsOffset)];
                _videosOffset = (int)viewModelState[nameof(_videosOffset)];
                _docsOffset = (int)viewModelState[nameof(_docsOffset)];

                var audioAlbums = JsonConvert.DeserializeObject<List<AudioAlbum>>(
                    viewModelState["AudioAlbums"].ToString());
                var audios = JsonConvert.DeserializeObject<List<Audio>>(
                    viewModelState["Audios"].ToString());

                
                var audiosSection = new PaginatedJumpListGroup<object>(audios, LoadMoreAudios) { Key = "audios" };
                audiosSection.CollectionChanged += Downloadable_CollectionChanged;

                AudioGroup = new IncrementalLoadingJumpListCollection();
                AudioGroup.Add(new PaginatedJumpListGroup<object>(audioAlbums, LoadMoreAudioAlbums) { Key = "albums" });
                AudioGroup.Add(audiosSection);

                var videoAlbums = JsonConvert.DeserializeObject<List<VideoAlbum>>(
                    viewModelState["VideoAlbums"].ToString());
                var videos = JsonConvert.DeserializeObject<List<Video>>(
                    viewModelState["Videos"].ToString());

                VideoGroup = new IncrementalLoadingJumpListCollection();
                VideoGroup.Add(new PaginatedJumpListGroup<object>(videoAlbums, LoadMoreVideoAlbums) { Key = "albums" });
                VideoGroup.Add(new PaginatedJumpListGroup<object>(videos, LoadMoreVideos) { Key = "videos" });

                Documents = JsonConvert.DeserializeObject<PaginatedCollection<Doc>>(
                    viewModelState[nameof(Documents)].ToString());
                Documents.LoadMoreItems = LoadMoreDocuments;
                Documents.CollectionChanged += Downloadable_CollectionChanged;

                object audiosScroll = null;
                object videosScroll = null;
                object docsScroll = null;

                viewModelState.TryGetValue(nameof(AudiosScrollPosition), out audiosScroll);
                viewModelState.TryGetValue(nameof(VideosScrollPosition), out videosScroll);
                viewModelState.TryGetValue(nameof(DocsScrollPosition), out docsScroll);

                if (audiosScroll != null)
                    AudiosScrollPosition = (double)audiosScroll;
                if (videosScroll != null)
                    VideosScrollPosition = (double)videosScroll;
                if (docsScroll != null)
                    DocsScrollPosition = (double)docsScroll;
            }
            else
            {
                var audiosSection = new PaginatedJumpListGroup<object>(LoadMoreAudios) { Key = "audios" };
                audiosSection.CollectionChanged += Downloadable_CollectionChanged;

                AudioGroup = new IncrementalLoadingJumpListCollection();
                AudioGroup.Add(new PaginatedJumpListGroup<object>(LoadMoreAudioAlbums) { Key = "albums" });
                AudioGroup.Add(audiosSection);

                VideoGroup = new IncrementalLoadingJumpListCollection();
                VideoGroup.Add(new PaginatedJumpListGroup<object>(LoadMoreVideoAlbums) { Key = "albums" });
                VideoGroup.Add(new PaginatedJumpListGroup<object>(LoadMoreVideos) { Key = "videos" });

                Documents = new PaginatedCollection<Doc>(LoadMoreDocuments);
                Documents.CollectionChanged += Downloadable_CollectionChanged;

                _audiosOffset = 0;
                _audioAlbumsOffset = 0;
                _videosOffset = 0;
                _videoAlbumsOffset = 0;
                _docsOffset = 0;

                PageTitle = _locService["AppNameText"];
                LoadUserInfo(_userID);
            }

            SetDefaultMode();

            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            if (e.NavigationMode == NavigationMode.Back && _appLoaderService.IsShowed)
            {
                e.Cancel = true;
                _cancelOperations = true;
                _appLoaderService.Hide();
                return;
            }

            if (e.NavigationMode == NavigationMode.Back && IsSelectionMode)
            {
                SetDefaultMode();
                e.Cancel = true;
                return;
            }

            if (e.NavigationMode == NavigationMode.New)
            {
                var audioAlbums = AudioGroup.FirstOrDefault(g => (string)g.Key == "albums");
                var audios = AudioGroup.FirstOrDefault(g => (string)g.Key == "audios");

                if (audios != null)
                {
                    audios.CollectionChanged -= Downloadable_CollectionChanged;
                    viewModelState["Audios"] = JsonConvert.SerializeObject(audios.ToList());
                }
                if (audioAlbums != null)
                    viewModelState["AudioAlbums"] = JsonConvert.SerializeObject(audioAlbums.ToList());

                var videoAlbums = VideoGroup.FirstOrDefault(g => (string)g.Key == "albums");
                var videos = VideoGroup.FirstOrDefault(g => (string)g.Key == "videos");

                if (videos != null)
                    viewModelState["Videos"] = JsonConvert.SerializeObject(videos.ToList());
                if (videoAlbums != null)
                    viewModelState["VideoAlbums"] = JsonConvert.SerializeObject(videoAlbums.ToList());

                Documents.CollectionChanged -= Downloadable_CollectionChanged;

                viewModelState[nameof(Documents)] = JsonConvert.SerializeObject(Documents.ToList());
                viewModelState[nameof(LastPivotIndex)] = LastPivotIndex;
                viewModelState[nameof(PageTitle)] = PageTitle;
                viewModelState[nameof(_audiosOffset)] = _audiosOffset;
                viewModelState[nameof(_audioAlbumsOffset)] = _audioAlbumsOffset;
                viewModelState[nameof(_videosOffset)] = _videosOffset;
                viewModelState[nameof(_videoAlbumsOffset)] = _videoAlbumsOffset;
                viewModelState[nameof(_docsOffset)] = _docsOffset;

                viewModelState[nameof(AudiosScrollPosition)] = AudiosScrollPosition;
                viewModelState[nameof(VideosScrollPosition)] = VideosScrollPosition;
                viewModelState[nameof(DocsScrollPosition)] = DocsScrollPosition;
            }

            if (!suspending)
            {
                PrimaryItems.Clear();
                SecondaryItems.Clear();
                SelectedItems.Clear();
            }

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private async Task<IEnumerable<object>> LoadMoreAudios(uint page)
        {
            int count = 50;
#if WINDOWS_UWP
            count = 6000;
#endif

            var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Audio.Get(
                _userID == 0 ? null : (int?)_userID, 
                count: count, offset: _audiosOffset));

            if (response.IsError)
            {
                if (response.Error.Message.StartsWith("Access"))
                    return new List<Audio>(0);
                throw new Exception(response.Error.ToString());
            }
            else
            {
                _audiosOffset += count;
                return response.Data.Items;
            }
        }

        private async Task<IEnumerable<object>> LoadMoreAudioAlbums(uint page)
        {
            var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Audio.GetAlbums(
                _userID == 0 ? null : (int?)_userID,
                count: 50, offset: _audioAlbumsOffset));

            if (response.IsError)
            {
                if (response.Error.Message.StartsWith("Access"))
                    return new List<AudioAlbum>(0);
                throw new Exception(response.Error.ToString());
            }
            else
            {
                _audioAlbumsOffset += 50;
                return response.Data.Items;
            }
        }

        private async Task<IEnumerable<object>> LoadMoreVideos(uint page)
        {
            var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Videos.Get(
                _userID == 0 ? null : (int?)_userID,
                count: 50, offset: _videosOffset));

            if (response.IsError)
            {
                if (response.Error.Message.StartsWith("Access"))
                    return new List<Video>(0);
                throw new Exception(response.Error.ToString());
            }
            else
            {
                _videosOffset += 50;
                return response.Data.Items;
            }
        }

        private async Task<IEnumerable<object>> LoadMoreVideoAlbums(uint page)
        {
            var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Videos.GetAlbums(
                _userID == 0 ? null : (int?)_userID,
                50, _videoAlbumsOffset, true, true));

            if (response.IsError)
            {
                if (response.Error.Message.StartsWith("Access"))
                    return new List<VideoAlbum>(0);
                throw new Exception(response.Error.ToString());
            }
            else
            {
                _videoAlbumsOffset += 50;
                return response.Data.Items;
            }
        }

        private async Task<IEnumerable<Doc>> LoadMoreDocuments(uint page)
        {
            var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Docs.Get(count: 50, 
                offset: _docsOffset,
                ownerId: _userID == 0 ? null : (int?)_userID));

            if (response.IsError)
            {
                if (response.Error.Message.StartsWith("Access"))
                    return new List<Doc>(0);
                throw new Exception(response.Error.ToString());
            }
            else
            {
                _docsOffset += 50;
                return response.Data.Items;
            }
        }

        private async void LoadUserInfo(long userID)
        {
            try
            {
                if (_userID >= 0)
                {
                    var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Users.Get(
                        _userID == 0 ? null : new List<object> { _userID }));

                    if (!response.IsError && response.Data.Any() && _userID == userID)
                    {
                        var user = response.Data[0];
                        PageTitle = $"{user.FirstName} {user.LastName}";
                    }
                }
                else
                {
                    var response = await _inTouchWrapper.ExecuteRequest(_inTouch.Groups.GetById(
                        new List<object> { -_userID }));

                    if (!response.IsError && response.Data.Any() && _userID == userID)
                        PageTitle = response.Data[0].Name;
                }
            }
            catch (Exception) { }
        }

        private void Downloadable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ActivateSelectionMode.RaiseCanExecuteChanged();            
        }

        private void CreateDefaultAppBarButtons()
        {
            PrimaryItems.Clear();
            SecondaryItems.Clear();

            if (LastPivotIndex == 0)
            {
                PrimaryItems.Add(new AppBarButton
                {
                    Label = _locService["AppBarButton_Shuffle_Text"],
                    Icon = new FontIcon { Glyph = "\uE14B" },
                    Command = PlayShuffleCommand
                });
            }

            PrimaryItems.Add(new AppBarButton
            {
                Label = _locService["AppBarButton_Refresh_Text"],
                Icon = new FontIcon { Glyph = "\uE117", FontSize = 14 },
                Command = ReloadContentCommand
            });

            if (LastPivotIndex != 1)
            {
                PrimaryItems.Add(new AppBarButton
                {
                    Label = _locService["AppBarButton_Select_Text"],
                    Icon = new FontIcon { Glyph = "\uE133", FontSize = 14 },
                    Command = ActivateSelectionMode
                });
            }
            
            if (_launchViewResolver.LaunchViewName != AppConstants.DEFAULT_MAIN_VIEW)
            {
                PrimaryItems.Add(new AppBarButton
                {
                    Label = _locService["AppBarButton_Home_Text"],
                    Icon = new FontIcon { Glyph = "\uE10F" },
                    Command = OpenMainViewCommand
                });
            }

            SecondaryItems.Add(new AppBarButton
            {
                Label = _locService["AppBarButton_TransferManager_Text"],
                Command = OpenTransferManagerCommand
            });
        }

        private void CreateSelectionAppBarButtons()
        {
            PrimaryItems.Clear();
            SecondaryItems.Clear();

            PrimaryItems.Add(new AppBarButton
            {
                Label = _locService["AppBarButton_Download_Text"],
                Icon = new FontIcon { Glyph = "\uE118" },
                Command = DownloadSelectedCommand
            });
            PrimaryItems.Add(new AppBarButton
            {
                Label = _locService["AppBarButton_Play_Text"],
                Icon = new FontIcon { Glyph = "\uE102" },
                Command = PlaySelectedCommand
            });
            PrimaryItems.Add(new AppBarButton
            {
                Label = _locService["AppBarButton_SelectAll_Text"],
                Icon = new FontIcon { Glyph = "\uE0E7" },
                Command = SelectAllCommand
            });

            if (_userID != 0 && _userID != _vkLoginService.UserID)
            {
                switch (LastPivotIndex)
                {
                    case 0:
                        SecondaryItems.Add(new AppBarButton
                        {
                            Label = _locService["AppBarButton_AddToMyAudios_Text"],
                            Command = AddSelectedToMyCollectionCommand
                        });
                        break;
                    case 1:
                        SecondaryItems.Add(new AppBarButton
                        {
                            Label = _locService["AppBarButton_AddToMyVideos_Text"],
                            Command = AddSelectedToMyCollectionCommand
                        });
                        break;
                    case 2:
                        SecondaryItems.Add(new AppBarButton
                        {
                            Label = _locService["AppBarButton_AddToMyDocs_Text"],
                            Command = AddSelectedToMyCollectionCommand
                        });
                        break;
                }                
            }
            else
            {
                SecondaryItems.Add(new AppBarButton
                {
                    Label = _locService["AppBarButton_Delete_Text"],
                    Command = DeleteSelectedCommand
                });
            }
        }

        private void SetSelectionMode()
        {
            IsSelectionMode = true;
            IsItemClickEnabled = false;
            IsLockedPivot = true;

            CreateSelectionAppBarButtons();
        }

        private void SetDefaultMode()
        {
            IsSelectionMode = false;
            IsItemClickEnabled = true;
            IsLockedPivot = false;

            CreateDefaultAppBarButtons();
        }

        private void OnShowTrackInfoCommand(Audio track)
        {
            var info = new VKAudioInfo(track.Id, track.OwnerId, track.Title, track.Artist, track.Url, track.Duration);
            string parameter = JsonConvert.SerializeObject(info);

            if (_purchaseService.IsFullVersionPurchased)
                _navigationService.Navigate("VKAudioInfoView", parameter);
            else
                _navigationService.Navigate("PurchaseView", JsonConvert.SerializeObject(
                    new KeyValuePair<string, string>("VKAudioInfoView", parameter)));
        }

        private void OnOpenTransferManagerCommand()
        {
            _navigationService.Navigate("TransferView", "downloads");
        }

        private void OnOpenMainViewCommand()
        {
            _navigationService.Navigate("MainView", null);
        }

        private void OnReloadContentCommand()
        {
            switch (LastPivotIndex)
            {
                case 0:
                    _audioAlbumsOffset = 0;
                    _audiosOffset = 0;
                    AudioGroup.Refresh();
                    break;
                case 1:
                    _videoAlbumsOffset = 0;
                    _videosOffset = 0;
                    VideoGroup.Refresh();
                    break;
                case 2:
                    _docsOffset = 0;
                    Documents.Refresh();
                    break;
            }                        
        }

        private void OnSelectAllCommand()
        {
            switch (LastPivotIndex)
            {
                case 0:
                    SelectAllAudios = !SelectAllAudios;
                    break;
                case 2:
                    SelectAllDocuments = !SelectAllDocuments;
                    break;
            }
        }

        private bool CanSelectionMode()
        {
            switch (LastPivotIndex)
            {
                case 0:
                    var audios = AudioGroup.FirstOrDefault(g => (string)g.Key == "audios");
                    if (audios != null && audios.Any())
                        return true;
                    break;
                case 2:
                    return Documents != null && Documents.Any();
            }

            return false;
        }

        private async void OnDownloadSelectedCommand()
        {
            _appLoaderService.Show(_locService["AppLoader_PreparingFilesToDownload"]);
            var items = SelectedItems.ToList();
            SetDefaultMode();

            var toDownload = new List<IDownloadable>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var audio = items[i] as Audio;
                var doc = items[i] as Doc;

                if (audio != null)
                    toDownload.Add(audio.ToDownloadable());
                else if (doc != null)
                    toDownload.Add(doc.ToDownloadable());
            }

            await _downloadsServiceHelper.StartDownloadingAsync(toDownload);
            _appLoaderService.Hide();
        }

        private bool CanExecuteDownloadSelectedCommand()
        {
            return SelectedItems.Count > 0 && SelectedItems.Any(o => o is Audio || o is Doc);
        }

        private void OnSelectionChangedCommand()
        {
            DownloadSelectedCommand.RaiseCanExecuteChanged();
            PlaySelectedCommand.RaiseCanExecuteChanged();
            AddSelectedToMyCollectionCommand.RaiseCanExecuteChanged();
            DeleteSelectedCommand.RaiseCanExecuteChanged();
        }

        private async void OnPlayShuffleCommand()
        {
            _appLoaderService.Show();
            var audios = AudioGroup.FirstOrDefault(g => (string)g.Key == "audios");
            if (audios == null)
                throw new Exception("Не найдена группа аудиозаписей.");

            _playerService.IsShuffleMode = true;
            await _playerService.PlayNewTracks(audios.Cast<Audio>().ToPlayerTracks(), 0);

#if !WINDOWS_UWP
            _navigationService.Navigate("PlayerView", null);
#endif
            _appLoaderService.Hide();
        }

        private async void OnExecuteTracksListItemCommand(object item)
        {     
            if (item is AudioAlbum)
            {
                _navigationService.Navigate("AudioAlbumView", JsonConvert.SerializeObject(item));
            }
            else if (item is Audio)
            {
                _appLoaderService.Show();
                var audios = AudioGroup.FirstOrDefault(g => (string)g.Key == "audios");
                if (audios == null)
                    throw new Exception("Не найдена группа аудиозаписей.");

                _playerService.IsShuffleMode = false;
                await _playerService.PlayNewTracks(audios.Cast<Audio>().ToPlayerTracks(),
                    audios.IndexOf(item));

#if !WINDOWS_UWP
            _navigationService.Navigate("PlayerView", null);
#endif

                _appLoaderService.Hide();
            }
            else if (item is Video)
            {
                _navigationService.Navigate("VideoInfoView", JsonConvert.SerializeObject(item));
            }
            else if (item is VideoAlbum)
            {
                _navigationService.Navigate("VideoAlbumView", JsonConvert.SerializeObject(item));
            }
            else if (item is Doc)
            {
                var doc = (Doc)item;
                if (doc.Ext == "gif" || doc.Ext == "png" || doc.Ext == "jpg")
                    await Launcher.LaunchUriAsync(new Uri(doc.Url));
                else
                    await _downloadsServiceHelper.StartDownloadingAsync(doc.ToDownloadable());
            }
        }

        private async void OnPlaySelectedCommand()
        {
            _appLoaderService.Show();

            var toPlay = SelectedItems.Where(o => o is Audio).Cast<Audio>().ToPlayerTracks();
            await _playerService.PlayNewTracks(toPlay, 0);

#if !WINDOWS_UWP
            _navigationService.Navigate("PlayerView", null);
#endif

            _appLoaderService.Hide();
            SetDefaultMode();
        }

        private async void OnDownloadItemCommand(object item)
        {
            _appLoaderService.Show();
            if (item is Audio)
            {
                var audio = (Audio)item;
                await _downloadsServiceHelper.StartDownloadingAsync(audio.ToDownloadable());
            }
            else if (item is Doc)
            {
                var doc = (Doc)item;
                await _downloadsServiceHelper.StartDownloadingAsync(doc.ToDownloadable());
            }
            _appLoaderService.Hide();
        }

        private bool CanExecuteDownloadItemCommand(object item)
        {
            return item is Audio || item is Doc;
        }

        private bool HasSelectedAudios()
        {
            return SelectedItems.Count > 0 && SelectedItems.Any(o => o is Audio);
        }

        private bool CanAddSelected()
        {
            return SelectedItems.Count > 0 && SelectedItems.Any(o => o is Audio || o is Video || o is Doc);
        }

        private bool CanDeleteSelected()
        {
            return SelectedItems.Count > 0 && SelectedItems.Any(o => o is Audio || o is Video || o is Doc);
        }

        private bool CanAddToMyCollection(object obj)
        {
            if (obj is Audio)
            {
                var audio = (Audio)obj;
                return audio.OwnerId != _inTouch.Session.UserId;
            }
            else if (obj is Video)
            {
                var video = (Video)obj;
                return video.OwnerId != _inTouch.Session.UserId && _userID != 0;
            }
            else if (obj is Doc)
            {
                var doc = (Doc)obj;
                return doc.OwnerId != _inTouch.Session.UserId;
            }

            return false;
        }

        private bool CanDelete(object obj)
        {
            if (obj is Audio)
            {
                var audio = (Audio)obj;
                return audio.OwnerId == _inTouch.Session.UserId;
            }
            else if (obj is Video)
            {
                var video = (Video)obj;
                return video.OwnerId == _inTouch.Session.UserId || _userID == 0;
            }
            else if (obj is Doc)
            {
                var doc = (Doc)obj;
                return doc.OwnerId == _inTouch.Session.UserId;
            }
            else if (obj is AudioAlbum)
            {
                var audioAlbum = (AudioAlbum)obj;
                return audioAlbum.OwnerId == _inTouch.Session.UserId;
            }
            else if (obj is VideoAlbum)
            {
                var videoAlbum = (VideoAlbum)obj;
                return videoAlbum.OwnerId == _inTouch.Session.UserId;
            }

            return false;
        }

        private async void OnDeleteCommand(object obj)
        {
            _appLoaderService.Show(String.Format(_locService["AppLoader_DeletingItem"], obj.ToString()));

            bool isSuccess = false;
            try
            {
                isSuccess = await DeleteObject(obj);
            }
            catch (Exception) { }

            if (obj is Audio)
            {
                if (!isSuccess)
                {
                    _dialogsService.Show(_locService["Message_AudioDeleteError_Text"],
                        _locService["Message_AudioDeleteError_Title"]);
                }
                else
                {
                    var audios = AudioGroup.FirstOrDefault(g => (string)g.Key == "audios");
                    audios?.Remove(obj);
                }
            }
            else if (obj is Video)
            {
                if (!isSuccess)
                {
                    _dialogsService.Show(_locService["Message_VideoDeleteError_Text"],
                        _locService["Message_VideoDeleteError_Title"]);
                }
                else
                {
                    var videos = VideoGroup.FirstOrDefault(g => (string)g.Key == "videos");
                    videos?.Remove(obj);
                }
            }
            else if (obj is Doc)
            {
                if (!isSuccess)
                {
                    _dialogsService.Show(_locService["Message_DocDeleteError_Text"],
                        _locService["Message_DocDeleteError_Title"]);
                }
                else
                {
                    Documents.Remove((Doc)obj);
                }
            }
            else if (obj is AudioAlbum)
            {
                if (!isSuccess)
                {
                    _dialogsService.Show(_locService["Message_AudioAlbumDeleteError_Text"],
                        _locService["Message_AudioAlbumDeleteError_Title"]);
                }
                else
                {
                    var audioAlbums = AudioGroup.FirstOrDefault(g => (string)g.Key == "albums");
                    audioAlbums?.Remove(obj);
                }
            }
            else if (obj is VideoAlbum)
            {
                if (!isSuccess)
                {
                    _dialogsService.Show(_locService["Message_VideoAlbumDeleteError_Text"],
                        _locService["Message_VideoAlbumDeleteError_Title"]);
                }
                else
                {
                    var videoAlbums = VideoGroup.FirstOrDefault(g => (string)g.Key == "albums");
                    videoAlbums?.Remove(obj);
                }
            }

            _appLoaderService.Hide();
        }

        private async void OnDeleteSelectedCommand()
        {
            _appLoaderService.Show(_locService["AppLoader_Preparing"]);
            _cancelOperations = false;

            var items = SelectedItems.Where(o => o is Audio || o is Video || o is Doc).ToList();
            var errors = new List<object>();
            var success = new List<object>();

            foreach (var obj in items)
            {
                if (_cancelOperations)
                {
                    errors.Add(obj);
                    continue;
                }

                _appLoaderService.Show(String.Format(_locService["AppLoader_DeletingItem"], obj.ToString()));

                bool isSuccess = false;
                try
                {
                    isSuccess = await DeleteObject(obj);
                }
                catch (Exception)
                {
                    errors.Add(obj);
                    _cancelOperations = true;
                    _appLoaderService.Show(_locService["AppLoader_PleaseWait"]);
                    continue;
                }

                if (isSuccess)
                    success.Add(obj);
                else
                    errors.Add(obj);

                await Task.Delay(300);
            }

            RemoveDeletedItems(success);
            if (errors.Any())
                ShowDeletingError(errors);

            _appLoaderService.Hide();
            SetDefaultMode();
        }

        private async void OnAddToMyCollection(object obj)
        {
            _appLoaderService.Show(String.Format(_locService["AppLoader_AddingItem"], obj.ToString()));

            bool isSuccess = false;
            try
            {
                isSuccess = await AddToMyCollection(obj);
            }
            catch (Exception) { }

            if (!isSuccess)
            {
                if (obj is Audio)
                {
                    _dialogsService.Show(_locService["Message_AudioAddError_Text"],
                        _locService["Message_AudioAddError_Title"]);
                }
                else if (obj is Video)
                {
                    _dialogsService.Show(_locService["Message_VideoAddError_Text"],
                        _locService["Message_VideoAddError_Title"]);
                }
                else if (obj is Doc)
                {
                    _dialogsService.Show(_locService["Message_DocAddError_Text"],
                        _locService["Message_DocAddError_Title"]);
                }
            }
            _appLoaderService.Hide();
        }

        private async void OnAddSelectedToMyCollection()
        {
            _appLoaderService.Show(_locService["AppLoader_Preparing"]);
            _cancelOperations = false;

            var items = SelectedItems.Where(o => o is Audio || o is Video || o is Doc).ToList();
            var errors = new List<object>();
            var success = new List<object>();

            foreach (var obj in items)
            {
                if (_cancelOperations)
                {
                    errors.Add(obj);
                    continue;
                }

                _appLoaderService.Show(String.Format(_locService["AppLoader_AddingItem"], obj.ToString()));
                bool isSuccess = false;
                try
                {
                    isSuccess = await AddToMyCollection(obj);
                }
                catch (Exception)
                {
                    errors.Add(obj);
                    _cancelOperations = true;
                    _appLoaderService.Show(_locService["AppLoader_PleaseWait"]);
                    continue;
                }

                if (isSuccess)
                    success.Add(obj);
                else
                    errors.Add(obj);

                await Task.Delay(300);
            }
            
            if (errors.Any())
                ShowAddingError(errors);

            _appLoaderService.Hide();
            SetDefaultMode();
        }

        private async Task<bool> AddToMyCollection(object obj)
        {
            Response<int> response = null;
            if (obj is Audio)
            {
                var audio = (Audio)obj;
                response = await _inTouchWrapper.ExecuteRequest(_inTouch.Audio.Add(
                    audio.Id, audio.OwnerId));
            }
            else if (obj is Video)
            {
                var video = (Video)obj;
                var vResponse = await _inTouchWrapper.ExecuteRequest(_inTouch.Videos.Add(
                    (int)video.Id, video.OwnerId, _inTouch.Session.UserId));

                return !vResponse.IsError;
            }
            else if (obj is Doc)
            {
                var doc = (Doc)obj;
                response = await _inTouchWrapper.ExecuteRequest(_inTouch.Docs.Add(
                    doc.Id, doc.OwnerId));
            }

            if (response.IsCaptchaError())
                throw new Exception("Captcha error: cancel");
            return !response.IsError;
        }

        private async Task<bool> DeleteObject(object obj)
        {
            Response<bool> response = null;
            if (obj is Audio)
            {
                var audio = (Audio)obj;
                response = await _inTouchWrapper.ExecuteRequest(_inTouch.Audio.Delete(
                    audio.Id, audio.OwnerId));
            }
            else if (obj is Video)
            {
                var video = (Video)obj;
                response = await _inTouchWrapper.ExecuteRequest(_inTouch.Videos.Delete(
                    (int)video.Id, video.OwnerId, _inTouch.Session.UserId));
            }
            else if (obj is Doc)
            {
                var doc = (Doc)obj;
                response = await _inTouchWrapper.ExecuteRequest(_inTouch.Docs.Delete(
                    doc.OwnerId, doc.Id));
            }
            else if (obj is AudioAlbum)
            {
                var audioAlbum = (AudioAlbum)obj;
                response = await _inTouchWrapper.ExecuteRequest(_inTouch.Audio.DeleteAlbum(
                    (int)audioAlbum.Id));
            }
            else if (obj is VideoAlbum)
            {
                var videoAlbum = (VideoAlbum)obj;
                response = await _inTouchWrapper.ExecuteRequest(_inTouch.Videos.DeleteAlbum(
                    (int)videoAlbum.Id));
            }

            if (response.IsCaptchaError())
                throw new Exception("Captcha error: cancel");
            return !response.IsError;
        }

        private void RemoveDeletedItems(List<object> items)
        {
            var audios = AudioGroup.FirstOrDefault(g => (string)g.Key == "audios");
            var videos = VideoGroup.FirstOrDefault(g => (string)g.Key == "videos");

            foreach (var item in items)
            {
                if (item is Audio)
                    audios.Remove(item);
                else if (item is Video)
                    videos.Remove(item);
                else if (item is Doc)
                    Documents.Remove((Doc)item);
            }
        }

        private void ShowDeletingError(List<object> errorObjects)
        {
            var sb = new StringBuilder();
            sb.AppendLine(_locService["Message_DeleteSelectedError_Text"]);
            sb.AppendLine();

            foreach (var obj in errorObjects)
                sb.AppendLine(obj.ToString());

            _dialogsService.Show(sb.ToString(), _locService["Message_DeleteSelectedError_Title"]);
        }

        private void ShowAddingError(List<object> errorObjects)
        {
            var sb = new StringBuilder();
            sb.AppendLine(_locService["Message_AddSelectedError_Text"]);
            sb.AppendLine();

            foreach (var obj in errorObjects)
                sb.AppendLine(obj.ToString());

            _dialogsService.Show(sb.ToString(), _locService["Message_AddSelectedError_Title"]);
        }

        private int _userID;
        private int _audiosOffset;
        private int _videosOffset;
        private int _audioAlbumsOffset;
        private int _videoAlbumsOffset;
        private int _docsOffset;

        private int _lastPivotIndex;
        private bool _cancelOperations = false;
        
        private readonly INavigationService _navigationService;
        private readonly IPlayerService _playerService;
        private readonly IDownloadsServiceHelper _downloadsServiceHelper;
        private readonly IAppLoaderService _appLoaderService;
        private readonly IVKLoginService _vkLoginService;
        private readonly IDialogsService _dialogsService;
        private readonly ILocService _locService;
        private readonly InTouch _inTouch;
        private readonly IInTouchWrapper _inTouchWrapper;
        private readonly ILaunchViewResolver _launchViewResolver;
        private readonly IPurchaseService _purchaseService;
    }
}
