using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NUISlideshow.Models
{
    public class MediaList : ObservableCollection<BitmapImage>
    {
        private int index;
        // The MediaList encapsulates the management of viewing a single image from a list of many
        public BitmapImage CurrentImage
        {
            get
            {
                if (base.Count != 0)
                {
                    BitmapImage current = base[index];
                    return current;
                }
                else throw new InvalidOperationException();
            }
        }

        public void addListener(PropertyChangedEventHandler listener)
        {
            base.PropertyChanged += listener;
        }
        // Increment and Decrement Position change the CurrentImage returned from the medialist.
        public void incrementPosition()
        {
            if(base.Count != 0) { 
                index = (index + 1) % base.Count;
            }
            OnPropertyChanged();
        }

        public void decrementPosition()
        {
            if (base.Count != 0)
            {
                if (index > 0)
                    index = (index - 1) % base.Count;
                else
                    index = base.Count - 1;
            }
            OnPropertyChanged();

        }
        // The annotation CallerMemberName automatically sets the parameter propertyname to the calling function at compiletime.
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}
