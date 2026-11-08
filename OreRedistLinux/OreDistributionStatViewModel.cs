using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace SpaceEngineersOreRedistribution
{
    public class OreDistributionStatViewModel : PropChangeNotifier
    {
        public OreDistributionStatViewModel(
            string oreType,
            double percentageArea, double percentageVolume,
            int totalArea, int totalVolume)
        {
            _oreType = oreType;
            _percentageArea = percentageArea;
            _percentageVolume = percentageVolume;
            _totalArea = totalArea;
            _totalVolume = totalVolume;
            ShowByArea = true;
        }

        // ---- map color of this ore (the tag at the end of the name)
        Color _color;
        bool _initializing;

        /// <summary>Color this ore has when it is not selected.</summary>
        public Color DefaultColor { get; private set; }

        /// <summary>Raised when the user picked a color or reset it.</summary>
        public event Action<OreDistributionStatViewModel> ColorChanged;

        public void InitColor(Color defaultColor, Color? custom)
        {
            _initializing = true;
            DefaultColor = defaultColor;
            Color = custom ?? defaultColor;
            IsCustom = custom.HasValue && custom.Value != defaultColor;
            _initializing = false;
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (!SetProp(ref _color, value)) return;
                OnPropertyChanged(nameof(ColorBrush));
                OnPropertyChanged(nameof(ColorTextBrush));
                OnPropertyChanged(nameof(ColorText));
                if (_initializing) return;
                IsCustom = value != DefaultColor;
                ColorChanged?.Invoke(this);
            }
        }

        bool _isCustom;
        public bool IsCustom
        {
            get => _isCustom;
            private set => SetProp(ref _isCustom, value);
        }

        public IBrush ColorBrush => new ImmutableSolidColorBrush(_color);
        public IBrush ColorTextBrush => new ImmutableSolidColorBrush(OrePalette.TextOn(_color));
        public string ColorText => OrePalette.ToHex(_color);

        public ICommand ResetColorCommand => new RelayCommand(o => Color = DefaultColor, o => IsCustom);

        double _percentageArea;
        double _percentageVolume;
        double _scaledPercentageArea;
        double _scaledPercentageVolume;
        int _totalArea;
        int _totalVolume;

        public double ScaledPercentageArea
        {
            get => _scaledPercentageArea;
            set => SetProp(ref _scaledPercentageArea, value);
        }

        public double ScaledPercentageVolume
        {
            get => _scaledPercentageVolume;
            set => SetProp(ref _scaledPercentageVolume, value);
        }

        public double PercentageArea
        {
            get => _percentageArea;
            set => SetProp(ref _percentageArea, value);
        }

        public double PercentageVolume
        {
            get => _percentageVolume;
            set => SetProp(ref _percentageVolume, value);
        }

        double _total;
        public double Total
        {
            get => _total;
            set => SetProp(ref _total, value);
        }

        bool _showByArea;
        public bool ShowByArea
        {
            get => _showByArea;
            set
            {
                SetProp(ref _showByArea, value);
                // Always update
                Percentage = value ? _percentageArea : _percentageVolume;
                ScaledPercentage = value ? _scaledPercentageArea : _scaledPercentageVolume;
                Total = value ? _totalArea : _totalVolume;
            }
        }

        string _oreType;
        public string OreType
        {
            get => _oreType;
            set => SetProp(ref _oreType, value);
        }

        double _percentage;
        public double Percentage
        {
            get => _percentage;
            set => SetProp(ref _percentage, value);
        }

        double _scaledPercentage;
        public double ScaledPercentage
        {
            get => _scaledPercentage;
            set => SetProp(ref _scaledPercentage, value);
        }

    }
}
