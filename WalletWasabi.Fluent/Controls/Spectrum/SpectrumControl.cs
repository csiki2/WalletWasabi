using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Skia.Helpers;
using Avalonia.Threading;
using SkiaSharp;

namespace WalletWasabi.Fluent.Controls.Spectrum;
#pragma warning disable CS0612

public class SpectrumControl : TemplatedControl, ICustomDrawOperation
{
	private const int NumBins = 64;

	private readonly AuraSpectrumDataSource _auraSpectrumDataSource;
	private readonly SplashEffectDataSource _splashEffectDataSource;

	private readonly SpectrumDataSource[] _sources;

	private ImmutablePen? _linePen;
	private IBrush? _lineBrush;

	private float[] _data;

	private bool _isAuraActive;
	private bool _isSplashActive;

	public static readonly StyledProperty<bool> IsActiveProperty =
		AvaloniaProperty.Register<SpectrumControl, bool>(nameof(IsActive));

	public static readonly StyledProperty<bool> IsDockEffectVisibleProperty =
		AvaloniaProperty.Register<SpectrumControl, bool>(nameof(IsDockEffectVisible));

	public SpectrumControl()
	{
		SetVisibility();
		_data = new float[NumBins];
		_auraSpectrumDataSource = new AuraSpectrumDataSource(NumBins);
		_splashEffectDataSource = new SplashEffectDataSource(NumBins);

		_auraSpectrumDataSource.GeneratingDataStateChanged += OnAuraGeneratingDataStateChanged;
		_splashEffectDataSource.GeneratingDataStateChanged += OnSplashGeneratingDataStateChanged;

		_sources = new SpectrumDataSource[] { _auraSpectrumDataSource, _splashEffectDataSource };

		Background = new RadialGradientBrush()
		{
			GradientStops =
			{
				new GradientStop { Color = Color.Parse("#00000D21"), Offset = 0 },
				new GradientStop { Color = Color.Parse("#FF000D21"), Offset = 1 }
			}
		};

		DispatcherTimer.Run(() =>
		{
			InvalidateVisual();
			return true;
		}, TimeSpan.FromMilliseconds(70));
	}

	public bool IsActive
	{
		get => GetValue(IsActiveProperty);
		set => SetValue(IsActiveProperty, value);
	}

	public bool IsDockEffectVisible
	{
		get => GetValue(IsDockEffectVisibleProperty);
		set => SetValue(IsDockEffectVisibleProperty, value);
	}

	private void OnSplashGeneratingDataStateChanged(object? sender, bool e)
	{
		_isSplashActive = e;
		SetVisibility();
	}

	private void OnAuraGeneratingDataStateChanged(object? sender, bool e)
	{
		_isAuraActive = e;
		SetVisibility();
	}

	private void SetVisibility()
	{
		IsVisible = _isSplashActive || _isAuraActive;
	}

	private void OnIsActiveChanged()
	{
		_auraSpectrumDataSource.IsActive = IsActive;

		if (IsActive)
		{
			_auraSpectrumDataSource.Start();
		}
	}

	protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == IsActiveProperty)
		{
			OnIsActiveChanged();
		}
		else if (change.Property == IsDockEffectVisibleProperty)
		{
			if (change.NewValue.GetValueOrDefault<bool>() && !IsActive)
			{
				_splashEffectDataSource.Start();
			}
		}
		else if (change.Property == ForegroundProperty)
		{
			_lineBrush = Foreground ?? Brushes.Magenta;
			InvalidateArrange();
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		_linePen = new Pen(_lineBrush, finalSize.Width / NumBins).ToImmutable();
		return base.ArrangeOverride(finalSize);
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);

		for (int i = 0; i < NumBins; i++)
		{
			_data[i] = 0;
		}

		foreach (var source in _sources)
		{
			source.Render(ref _data);
		}

		context.Custom(this);
	}

	private  SKPaint _paint = new SKPaint { ColorF = new SKColorF(1.0f, 1.0f, 1.0f) };
	private SKSurface _surface;
	private const double TextureHeight = 32;
	private const double TextureWidth = 32;

	private void RenderBars(SKCanvas canvas)
	{
		canvas.Clear();
		var thickness = TextureWidth / NumBins;
		var center = (TextureWidth / 2);

		double x = 0;

		for (int i = 0; i < NumBins; i++)
		{
			var dCenter = Math.Abs(x - center);
			var multiplier = 1 - (dCenter / center);

			canvas.DrawLine(
				new SKPoint((float)x, (float)TextureHeight),
				new SKPoint((float)x, (float)(TextureHeight - multiplier * _data[i] * (TextureHeight * 0.8))), _paint);

			x += thickness;
		}
	}

	void IDisposable.Dispose()
	{
		// nothing to do.
	}

	SKPaint _blur = new SKPaint
	{
		ImageFilter = SKImageFilter.CreateBlur(24, 24, SKShaderTileMode.Clamp),
		FilterQuality = SKFilterQuality.Low
	};

	bool IDrawOperation.HitTest(Point p) => Bounds.Contains(p);

	void IDrawOperation.Render(IDrawingContextImpl context)
	{
		var bounds = Bounds;

		if (context is not ISkiaDrawingContextImpl skia)
		{
			return;
		}

		if (_surface is null)
		{
			_surface =
				SKSurface.Create(skia.GrContext, false, new SKImageInfo((int)TextureWidth, (int)TextureHeight));
		}

		RenderBars(_surface.Canvas);

		using var snapshot = _surface.Snapshot();

		skia.SkCanvas.DrawImage(snapshot,
			new SKRect(0, 0, (float)TextureWidth, (float)TextureHeight),
			new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height), _blur);
	}

	bool IEquatable<ICustomDrawOperation>.Equals(ICustomDrawOperation? other) => false;
}
