using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AutoSerialPort.UI.Behaviors;

/// <summary>
/// 注册一个附加行为
/// 列表自动滚动到末尾的附加行为。
/// </summary>
public sealed class AutoScrollBehavior
{
    private AutoScrollBehavior()
    {
    }

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<AutoScrollBehavior, ListBox, bool>("IsEnabled");

    private static readonly AttachedProperty<CollectionSubscription?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<AutoScrollBehavior, ListBox, CollectionSubscription?>("Subscription");

    static AutoScrollBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<ListBox>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(AvaloniaObject element) => element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(AvaloniaObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(ListBox listBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool enabled)
        {
            if (enabled)
            {
                listBox.PropertyChanged += OnListBoxPropertyChanged;
                Subscribe(listBox, listBox.ItemsSource);
            }
            else
            {
                listBox.PropertyChanged -= OnListBoxPropertyChanged;
                Unsubscribe(listBox);
            }
        }
    }

    private static void OnListBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is ListBox listBox && e.Property == ItemsControl.ItemsSourceProperty)
        {
            Subscribe(listBox, e.NewValue);
        }
    }

    private static void Subscribe(ListBox listBox, object? itemsSource)
    {
        Unsubscribe(listBox);

        if (itemsSource is not INotifyCollectionChanged collection)
        {
            return;
        }

        NotifyCollectionChangedEventHandler handler = (_, __) =>
        {
            if (!GetIsEnabled(listBox))
            {
                return;
            }

            var last = listBox.Items.Cast<object?>().LastOrDefault();
            if (last == null)
            {
                return;
            }

            Dispatcher.UIThread.Post(() => listBox.ScrollIntoView(last));
        };

        collection.CollectionChanged += handler;
        listBox.SetValue(SubscriptionProperty, new CollectionSubscription(collection, handler));
    }

    private static void Unsubscribe(ListBox listBox)
    {
        var subscription = listBox.GetValue(SubscriptionProperty);
        if (subscription != null)
        {
            subscription.Dispose();
            listBox.ClearValue(SubscriptionProperty);
        }
    }

    private sealed class CollectionSubscription : IDisposable
    {
        private readonly INotifyCollectionChanged _collection;
        private readonly NotifyCollectionChangedEventHandler _handler;

        public CollectionSubscription(INotifyCollectionChanged collection, NotifyCollectionChangedEventHandler handler)
        {
            _collection = collection;
            _handler = handler;
        }

        public void Dispose()
        {
            _collection.CollectionChanged -= _handler;
        }
    }
}
