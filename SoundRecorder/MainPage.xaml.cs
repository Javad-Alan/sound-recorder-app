using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using SoundRecorder.ViewModels;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.GamerServices;
using System.Windows.Media.Imaging;

namespace SoundRecorder
{
    public partial class MainPage : PhoneApplicationPage
    {
        /* Initialize Logical Variables */
        private int _lastSelectedIndex = -1;
        private Brush _lastSelectedItemBackground;
        private ItemViewModel _lastSelectedItem = null;
        bool _inRecordingMode = false;
        bool _playbackStarted = false;
        bool _playBackPause = false;
        string _lastRecordedFileName = null;
        /* Initialize Logical Variables */

        /* Initialize Mic Variables */
        MemoryStream _stream;
        SoundEffectInstance _sound = null;
        Microphone _microphone = Microphone.Default;
        /* Initialize Mic Variables */

        byte[] _buffer;

        // setting the UI of the users current color setup
        System.Windows.Media.Color _currentAccentColorHex = (System.Windows.Media.Color)Application.Current.Resources["PhoneAccentColor"];

        // constructor
        public MainPage()
        {
            InitializeComponent();
            LoadMenuList();     // load please
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            // dispatcherTimer loop for mic capture
            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromMilliseconds(33);
            dt.Tick += delegate
            {
                try  // try and catch - checking if user has stopped the recording then update the menu
                {
                    FrameworkDispatcher.Update(); // running the loop tick - every 33ms

                    if (_sound != null)
                    {
                        if (_sound.State == SoundState.Playing && !_playbackStarted)
                        {
                            _playbackStarted = true;
                        }
                        else if (_sound.State == SoundState.Stopped && _playbackStarted)    // if playback stopped then reset values
                        {
                            StopNow();
                            LoadMenuList();
                        }
                    }
                }
                catch { }   // catch something bad
            };
            dt.Start();     // fire of the timer

            // needed for the xna microphone update
            Microsoft.Xna.Framework.FrameworkDispatcher.Update();

            // update meter bar 100ms
            Microphone.Default.BufferDuration = TimeSpan.FromSeconds(.1);            

            _microphone.BufferReady += new EventHandler<EventArgs>(microphone_BufferReady);

            // allow app to function with screen locked
            PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _lastSelectedIndex = -1;
            _lastSelectedItem = null;
            SetupPageStyle();   // setting up UI with current color scheme

            var v = (Visibility)Resources["PhoneLightThemeVisibility"];     // getting theme value

            if (v.ToString().Equals("Visible"))     // checking if phone is set to light theme
            {
                ContentPanel.Background = new SolidColorBrush(Colors.White);    // if so, then switch up background color
                stackPanelMenu.Background = new SolidColorBrush(Colors.White);
            } 
        }

        private void SetupPageStyle()   // setting the UI from the user's current color scheme
        {
            LayoutRoot.Background = new SolidColorBrush(_currentAccentColorHex);
            recordBtn.BorderBrush = new SolidColorBrush(_currentAccentColorHex);
            playBtn.BorderBrush = new SolidColorBrush(_currentAccentColorHex);
            barMaskStack.Background = new SolidColorBrush(_currentAccentColorHex);
        }

        // microphone amplitude data method
        void microphone_BufferReady(object sender, EventArgs e)
        {
            int size = _microphone.GetData(_buffer);  // get the size of the recording
            if (size == 0)
                return;

            _stream.Write(_buffer, 0, _buffer.Length);  // write to stream, the length of the recording

            long aggregate = 0;
            int sampleInx = 0;

            while (sampleInx < size)  // getting average amplitude of current byte recording
            {
                // to get amplitude, have to get a 2 byte sample from the buffer
                short sample = BitConverter.ToInt16(this._buffer, sampleInx);
                
                // must upscale to a 32-bit value
                int sampleAsInt = (int)sample;
                int value = Math.Abs(sampleAsInt);

                aggregate += value;

                sampleInx += 2;
            }

            int volume = (int)(aggregate / (size / 2));    // getting aggregate of volume

            long meterCoverSize = volume / 10;

            if (meterCoverSize > 323)
            {
                meterCoverSize = 323;   // making sure here that meter mask doesn't get any bigger
            }

            Thickness newMargin = barMask.Margin;

            // where the magic happens
            newMargin.Bottom = 0 + meterCoverSize;
            barMask.Margin = newMargin;
            barMask.Height = 323 - meterCoverSize;
        }

        private void recordBtn_Click(object sender, RoutedEventArgs e)  // record function
        {
            if (_playBackPause == true)  // if in playback and paused then stop playback
            {
                _sound.Stop();
                StopNow();
                LoadMenuList();
                return;
            }
            else if (_playBackPause == false && _sound != null)  // else if not pause but sound is still playing, then stop
            {
                _sound.Stop();
                StopNow();
                LoadMenuList();
                return;
            }
            else  // else not either, then check if recording or not
            {
                if (_inRecordingMode)   // if already in recording mode then stop and save recording
                {
                    _microphone.Stop();
                    Thickness newMargin = barMask.Margin; // access point to mask of the meterbarS
                    // resetting margin below
                    newMargin.Left = 6;
                    newMargin.Top = -323;
                    newMargin.Right = 0;
                    newMargin.Bottom = 0;
                    barMask.Margin = newMargin;
                    barMask.Height = 323;
                    // resetting button
                    recordBtn.Content = "R";
                    recordBtn.Background = new SolidColorBrush(Colors.Red);
                    recLight.Source = new BitmapImage(new Uri("Resources/Images/not-recording-s.png", UriKind.RelativeOrAbsolute));

                    SaveRecording();    // saving

                    _inRecordingMode = false;
                }
                else   // else if not in recording then initialize recording
                {
                    _stream = new MemoryStream();
                    _buffer = new byte[_microphone.GetSampleSizeInBytes(_microphone.BufferDuration)];
                    recordBtn.Content = "S";
                    recordBtn.Background = new SolidColorBrush(Colors.Orange);
                    _microphone.Start();
                    _inRecordingMode = true;
                    recLight.Source = new BitmapImage(new Uri("Resources/Images/recording-s.png", UriKind.RelativeOrAbsolute));
                }
            }
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_inRecordingMode)   // check if recording then
            {
                if (_stream == null)    // if stream null do nothing
                {
                    return;
                }
                return;
            }

            if (_lastSelectedItem == null)  // check if not selected recording
            {
                MessageBox.Show("Please select a recording");
                return;
            }

            if (_playBackPause == false)    // if paused then enter
            {
                if (_sound != null)   // if there is sound loaded then resume
                {
                    _sound.Resume();
                    playBtn.Background = new SolidColorBrush(Colors.Purple);
                    recordBtn.Content = "S";
                    recordBtn.Background = new SolidColorBrush(Colors.Orange);
                    _playBackPause = true;
                    return;
                }
            }

            if (_playBackPause == true)   // if not paused then enter
            {
                // pausing and setting flags
                playBtn.Background = new SolidColorBrush(Colors.Green);
                _sound.Pause();
                _playBackPause = false;
                return;
            }

            if (_sound == null)  // if no sound is loaded or playing then load sound and play
            {
                var isoStore = IsolatedStorageFile.GetUserStoreForApplication();

                if (_lastRecordedFileName.Equals("No recorded files exist"))   // checking if files exist
                {
                    return;
                }
                else   // if files do exist then load the selected sound from the menu
                {
                    IsolatedStorageFileStream fstream = isoStore.OpenFile(_lastRecordedFileName, System.IO.FileMode.Open);
                    _stream = new MemoryStream();
                    // buffer setup
                    _buffer = new byte[fstream.Length];
                    int actualSize = fstream.Read(_buffer, 0, (int)fstream.Length);
                    if (actualSize > 0)
                    {
                        _stream.Write(_buffer, 0, actualSize);
                    }

                    fstream.Close();
                    // load new instance of recording into _sound
                    _sound = new SoundEffect(_stream.ToArray(), _microphone.SampleRate, AudioChannels.Mono).CreateInstance();
                    _sound.Play();  // start playing
                    _playBackPause = true;
                    playBtn.Background = new SolidColorBrush(Colors.Purple);
                    recordBtn.Content = "S";
                    recordBtn.Background = new SolidColorBrush(Colors.Orange);
                }
            }
        }

        private void SaveRecording()  // saving recording into isolated storage
        {
            var isoStore = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();
            DateTime fileTime = DateTime.Now;   // getting the date now
            long fileTicks = fileTime.Ticks;    // getting ticks and setting as filename
            string datFileName = fileTicks.ToString() + ".dat";
            _lastRecordedFileName = datFileName;

            using (var targetFile = isoStore.CreateFile(datFileName))   // creating the file where the recording sits
            {
                var dataBuffer = _stream.GetBuffer();   // get data from buffer
                targetFile.Write(dataBuffer, 0, (int)_stream.Length);   // length of target file
                targetFile.Flush(); // flush and close file
                targetFile.Close();
                _stream = null;
            }

            using (var targetFile = isoStore.CreateFile(fileTicks.ToString() + ".info"))    // info file created for references to recording
            {
                // creating file name from the recording file name and the current time and saving it
                StreamWriter sw = new StreamWriter(targetFile); 
                string fileName = datFileName + "|" + fileTime.ToString();
                sw.WriteLine(fileName);
                sw.Flush(); // flush and close again
                sw.Close();
                _stream = null;
            }

            LoadMenuList();  // update the menu with hopefully the new recording
        }
        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If not selected anything yet, do nothing
            if (MainListBox.SelectedIndex == -1)
                return;

            if (_lastSelectedIndex != -1)   // if last selected is not '-1' then enter create a few vars
            {
                var listBox = sender as ListBox;
                var listBoxItem = listBox.ItemContainerGenerator.ContainerFromIndex(_lastSelectedIndex) as ListBoxItem;

                listBoxItem.Background = _lastSelectedItemBackground;
            }

            ItemViewModel ivm = (ItemViewModel)MainListBox.SelectedItem;    // get last selected item

            if (ivm != null)  // if not null then get recording file name and index of where it is in the menu
            {
                var listBox = sender as ListBox;
                var listBoxItem = listBox.ItemContainerGenerator.ContainerFromItem(listBox.SelectedItem) as ListBoxItem;
                // getting stuff from menu here
                _lastSelectedItemBackground = listBoxItem.Background;
                _lastSelectedIndex = MainListBox.SelectedIndex;
                listBoxItem.Background = new SolidColorBrush(_currentAccentColorHex);
                _lastSelectedItem = ivm;
                _lastRecordedFileName = ivm.FileName;
            }
        }

        private void StopNow()  // invoked when user wants to stop recording or playback
        {
            playBtn.Background = new SolidColorBrush(Colors.Green);
            recordBtn.Content = "R";
            recordBtn.Background = new SolidColorBrush(Colors.Red);
            _playBackPause = false;
            _sound = null;
            _playbackStarted = false;
        }

        private void LoadMenuList()   // used to update list on menu with current recordings and deselect any recordings
        {
            App.ViewModel.LoadData();
            DataContext = App.ViewModel;
            App.ViewModel.FilesUpdated();
            _lastSelectedItem = null;
            _lastSelectedIndex = -1;
        }

        private void DeleteNow()  // delete a recording after a user has used the long tap-hold event
        {
            string infoFileName = _lastSelectedItem.FileName.Replace(".dat", ".info");
            var isoStore = IsolatedStorageFile.GetUserStoreForApplication();
            isoStore.DeleteFile(_lastSelectedItem.FileName);
            isoStore.DeleteFile(infoFileName);
            App.ViewModel.LoadData();
            DataContext = App.ViewModel;
            App.ViewModel.FilesUpdated();
            _lastSelectedItem = null;
        }


        private void OnMessageBoxAction(IAsyncResult ar)
        {
            int? selectedButton = Guide.EndShowMessageBox(ar);
            switch (selectedButton)
            {
                case 0:
                    Deployment.Current.Dispatcher.BeginInvoke(() => DeleteNow());   // delete function called if user taps 'yes'
                    break;
                case 1:   // do nothing if user taps 'no'
                    break;
                default:
                    break;
            }
        }

        private void MainListBox_Hold(object sender, GestureEventArgs e)  // tap-hold function used to delete a recording from storage
        {
            if (_lastSelectedItem == null)  // if user selects no recording then error message
            {
                MessageBox.Show("Please select a recording");
                return;
            }

            if (_sound != null)   // if the user is currently recording or playing back a sound then error message
            {
                MessageBox.Show("Please stop playback");
                return;
            }

            // question to the user if 'yes' then delete recording, if 'no' then do nothing
            Guide.BeginShowMessageBox("Question", "Delete this recording?", new string[] { "yes", "no" }, 0, MessageBoxIcon.Warning, new AsyncCallback(OnMessageBoxAction), null);
        }
    }
}