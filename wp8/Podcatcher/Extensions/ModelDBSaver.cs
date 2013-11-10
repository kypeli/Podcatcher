using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Podcatcher.Extensions
{
    public abstract class DBBackedModel : INotifyPropertyChanged
    {
        public void StoreProperty<T>(String propertyName, T value)
        {
            PropertyInfo[] properties = GetType().GetProperties();
            PropertyInfo property = properties.FirstOrDefault(p => p.Name == propertyName);
            if (property == null)
            {
                Debug.WriteLine("Error: Could not find property {0} for object {1}.", propertyName, GetType().Name);
            }

            NotifyPropertyChanging();
            property.SetValue(this, value);
            NotifyPropertyChanged(propertyName);
        }

        #region propertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        public void NotifyPropertyChanging()
        {
            if ((this.PropertyChanging != null))
            {
                this.PropertyChanging(this, null);
            }
        }

        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }
}
