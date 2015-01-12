using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SoundRecorder.ViewModels
{
    public class ItemViewModel : INotifyPropertyChanged
    {
        private string _recordName;

        public string FileName      // set and get of recorded file name
        {
            get
            {
                return _recordName;
            }
            set
            {
                if (value != _recordName)
                {
                    _recordName = value;
                    NotifyPropertyChanged("FileName");
                }
            }
        }

        private string _disStr;

        public string DisplayString     // set and get of the display string of a recording
        {
            get
            {
                return _disStr;
            }
            set
            {
                if (value != _disStr)
                {
                    _disStr = value;
                    NotifyPropertyChanged("DisplayString");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));  // setting property name of a recording
            }
        }
    }
}
