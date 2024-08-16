using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace WebTool.Controls
{
    public partial class HeaderedContentControl : UserControl
    {
        public HeaderedContentControl()
        {
            this.InitializeComponent();
        }

        #region Icon

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(HeaderedContentControl),
            new PropertyMetadata(null, OnIconChanged)
        );

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var instance = (HeaderedContentControl)d;
            if (args.NewValue is not IconElement iconElement) return;
            instance.IconPresenter.Content = iconElement;
            instance.OnIconChanged(args.OldValue, args.NewValue);
        }

        protected virtual void OnIconChanged(object oldValue, object newValue) { }

        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region Header

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(HeaderedContentControl),
            new PropertyMetadata(null, OnHeaderChanged)
        );

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var instance = (HeaderedContentControl)d;
            instance.SetHeaderVisibility();
            instance.OnHeaderChanged(args.OldValue, args.NewValue);
        }

        private void SetHeaderVisibility()
        {
            if (Header is not string headerText) return;
            HeaderLabel.Visibility = string.IsNullOrEmpty(headerText) ? Visibility.Collapsed : Visibility.Visible;
        }

        protected virtual void OnHeaderChanged(object oldValue, object newValue) { }

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        #endregion

        #region Description

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description),
            typeof(object),
            typeof(HeaderedContentControl),
            new PropertyMetadata(null, OnDescriptionChanged)
        );

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var instance = (HeaderedContentControl)d;
            instance.SetDescriptionVisibility();
            instance.OnDescriptionChanged(args.OldValue, args.NewValue);
        }

        private void SetDescriptionVisibility()
        {
            if (Description is not string descriptionText) return;
            DescriptionLabel.Visibility = string.IsNullOrEmpty(descriptionText) ? Visibility.Collapsed : Visibility.Visible;
        }

        protected virtual void OnDescriptionChanged(object oldValue, object newValue) { }

        public object Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        #endregion
    }
}
