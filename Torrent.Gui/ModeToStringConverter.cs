using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Torrent.Gui
{
    class ModeToStringConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Mode)
            {
                var mode = (Mode)value;
                switch (mode)
                {
                    case Mode.Stopped:
                        return "暂停";
                    case Mode.Seed:
                        return "做种中...";
                    case Mode.Idle:
                        return "空闲";
                    case Mode.Download:
                        return "下载中...";
                    case Mode.Error:
                        return "错误";
                    case Mode.Hash:
                        return "正在计算文件(Hashing)";
                    case Mode.Completed:
                        return "完成.";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
