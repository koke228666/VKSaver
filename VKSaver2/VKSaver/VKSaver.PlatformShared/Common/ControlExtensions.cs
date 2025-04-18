﻿using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace VKSaver.Common
{
    public static class ControlExtensions
    {
        public static bool GetShowFlyoutOnHolding(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowFlyoutOnHoldingProperty);
        }

        public static void SetShowFlyoutOnHolding(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowFlyoutOnHoldingProperty, value);
        }
        
        public static readonly DependencyProperty ShowFlyoutOnHoldingProperty =
            DependencyProperty.RegisterAttached("ShowFlyoutOnHolding", typeof(bool), 
                typeof(ControlExtensions), new PropertyMetadata(false, OnShowFlyoutOnHoldingChanged));

        private static void OnShowFlyoutOnHoldingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            bool isOn = (bool)e.NewValue;
            bool oldIsOn = (bool)e.OldValue;

            var control = obj as Control;
            if (control == null)
                return;

            if (oldIsOn)
                Unsubscribe(control);
            if (isOn)
                Subscribe(control);
        }

        private static void Control_Holding(object sender, HoldingRoutedEventArgs e) => 
            OpenFlyout(sender, e.GetPosition((UIElement)sender));
#if WINDOWS_UWP
        private static void Control_RightTapped(object sender, RightTappedRoutedEventArgs e) => 
            OpenFlyout(sender, e.GetPosition((UIElement)sender));
#endif

        private static void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            SetShowFlyoutOnHolding((DependencyObject)sender, false);
        }

        private static void Subscribe(Control control)
        {
            //control.Unloaded += Control_Unloaded;
            control.Holding += Control_Holding;
#if WINDOWS_UWP
            control.RightTapped += Control_RightTapped;
#endif
        }

        private static void Unsubscribe(Control control)
        {
            //control.Unloaded -= Control_Unloaded;
            control.Holding -= Control_Holding;
#if WINDOWS_UWP
            control.RightTapped -= Control_RightTapped;
#endif
        }

        private static void OpenFlyout(object sender, Point position)
        {
            var contControl = sender as ContentControl;
            if (contControl != null && contControl.DataContext == null)
            {
                contControl.DataContext = contControl.Content;
            }

            var tappedItem = (FrameworkElement)sender;
            var attachedFlyout = (MenuFlyout)FlyoutBase.GetAttachedFlyout(tappedItem);
#if WINDOWS_UWP
            attachedFlyout.ShowAt(tappedItem, position);
#else
            attachedFlyout.ShowAt(tappedItem);
#endif
        }

        public static Storyboard GetStoryboard(this FrameworkElement element, string name, string message = null)
        {
            var storyboard = element.Resources[name] as Storyboard;

            if (storyboard == null)
            {
                if (message == null)
                {
                    message = $"Storyboard '{name}' cannot be found! Check the default Generic.xaml.";
                }

                throw new NullReferenceException(message);
            }

            return storyboard;
        }

        public static CompositeTransform GetCompositeTransform(this FrameworkElement element, string message = null)
        {
            var transform = element.RenderTransform as CompositeTransform;

            if (transform == null)
            {
                if (message == null)
                {
                    message = $"{element.Name}'s RenderTransform should be a CompositeTransform! Check the default Generic.xaml.";
                }

                throw new NullReferenceException(message);
            }

            return transform;
        }

        public static ScrollViewer GetScrollViewer(this DependencyObject element)
        {
            if (element is ScrollViewer)
            {
                return (ScrollViewer)element;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                return result;
            }

            return null;
        }
    }
}
