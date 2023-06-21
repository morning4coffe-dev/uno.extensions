﻿using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Page = Microsoft.UI.Xaml.Controls.Page;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MauiEmbedding.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MauiControlsPage : Page
{
	public MauiControlsPage()
	{
		this.InitializeComponent();

		var lbl = new Label()
		{
			// If we don't specify the size , the label will not be visible
			// when the binding changes it doesn't recalculate its size
			// This control doesn't show up on mobile targets but it's on the visual tree
			HeightRequest = 20,
			BackgroundColor = Colors.Pink
		};

		lbl.HandlerChanged += (_, __) => lbl.Layout(new Rect(0,0,lbl.WidthRequest,lbl.HeightRequest));


		var b = new BoxView
		{
			HeightRequest = 300,
			WidthRequest = 300
		};

		var source = new List<string> { "Uno", "Maui", "WinUI" };

		this.picker.ItemsSource = source;
		var mauiBinding = new Microsoft.Maui.Controls.Binding
		{
			Path = "Title",
			Source = DataContext
		};

		lbl.SetBinding(Label.TextProperty, mauiBinding);

		this.stack.BindingContextChanged +=(_,__) =>
		{
			_ = this.stack.BindingContext;
			_ = lbl.BindingContext;
		};

		this.stack.Add(lbl);
		this.stack.Add(b);

	}
}
