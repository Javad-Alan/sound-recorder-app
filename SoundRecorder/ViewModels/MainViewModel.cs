using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.IO.IsolatedStorage;
using System.IO;
using System.Linq;

namespace SoundRecorder.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.Items = new ObservableCollection<ItemViewModel>();
        }

        public ObservableCollection<ItemViewModel> Items { get; private set; }  // Collection of items objects
        
        // Creates a few objects and add them into collection

        public void LoadData()  // loading data from storage
        {
            this.Items = new ObservableCollection<ItemViewModel>();
            var isoStore = IsolatedStorageFile.GetUserStoreForApplication();
            string[] fileNames = isoStore.GetFileNames();
            List<RecordingInfo> itemValues = new List<RecordingInfo>();

            foreach (string fileName in fileNames)  // looping through the files
            {
                if (fileName.EndsWith(".info"))     // get file with .info and do stuff like get record name and display str
                {
                    // .info file used to keep a record of file recordings
                    IsolatedStorageFileStream f = isoStore.OpenFile(fileName, System.IO.FileMode.Open);
                    StreamReader sr = new StreamReader(f);
                    string data = sr.ReadLine();
                    sr.Close();
                    f.Close();
                    string[] dataParts = data.Split('|');
                    itemValues.Add(new RecordingInfo
                    {
                        disStr = dataParts[1],
                        recordName = dataParts[0]
                    });
                }
            }

            if (itemValues.Count == 0)  // if no recordings, then set ivm that no files exist in storage
            {
                ItemViewModel ivm = new ItemViewModel();
                ivm.FileName = "No recorded files exist";
                this.Items.Add(ivm);
            }
            else  // else then loop through the recordings and add them to list
            {
                var descList = from s in itemValues
                               orderby s.disStr descending
                               select s;

                foreach (RecordingInfo s in descList)   // foreach recording, set display, file name strings and add
                {
                    ItemViewModel ivm = new ItemViewModel();
                    ivm.DisplayString = s.disStr;
                    ivm.FileName = s.recordName;
                    this.Items.Add(ivm);
                }
            }
        }

        public void FilesUpdated()   // called when files have been updated
        {
            NotifyPropertyChanged("Items");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
