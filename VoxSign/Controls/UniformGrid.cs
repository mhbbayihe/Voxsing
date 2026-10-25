using Microsoft.Maui.Layouts;

namespace VoxSign.Controls;

/// <summary>
/// Grille à colonnes de largeur égale, remplie dans l'ordre des enfants.
/// Compatible avec <see cref="BindableLayout"/> (contrairement à <see cref="Grid"/>).
/// </summary>
public class UniformGrid : Layout
{
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(
        nameof(Columns), typeof(int), typeof(UniformGrid), 2, propertyChanged: (b, _, _) => ((UniformGrid)b).InvalidateMeasure());

    public static readonly BindableProperty ColumnSpacingProperty = BindableProperty.Create(
        nameof(ColumnSpacing), typeof(double), typeof(UniformGrid), 0d, propertyChanged: (b, _, _) => ((UniformGrid)b).InvalidateMeasure());

    public static readonly BindableProperty RowSpacingProperty = BindableProperty.Create(
        nameof(RowSpacing), typeof(double), typeof(UniformGrid), 0d, propertyChanged: (b, _, _) => ((UniformGrid)b).InvalidateMeasure());

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double ColumnSpacing { get => (double)GetValue(ColumnSpacingProperty); set => SetValue(ColumnSpacingProperty, value); }
    public double RowSpacing { get => (double)GetValue(RowSpacingProperty); set => SetValue(RowSpacingProperty, value); }

    protected override ILayoutManager CreateLayoutManager() => new UniformGridManager(this);

    sealed class UniformGridManager(UniformGrid grid) : LayoutManager(grid)
    {
        readonly List<double> _rowHeights = [];

        double ColumnWidth(double width) =>
            Math.Max(0, (width - grid.Padding.HorizontalThickness - grid.ColumnSpacing * (grid.Columns - 1)) / grid.Columns);

        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var columnWidth = ColumnWidth(widthConstraint);
            _rowHeights.Clear();
            var visible = grid.Where(c => c.Visibility != Visibility.Collapsed).ToList();
            for (var i = 0; i < visible.Count; i++)
            {
                var size = visible[i].Measure(columnWidth, double.PositiveInfinity);
                var row = i / grid.Columns;
                if (row == _rowHeights.Count)
                    _rowHeights.Add(0);
                _rowHeights[row] = Math.Max(_rowHeights[row], size.Height);
            }

            var height = _rowHeights.Sum() + grid.RowSpacing * Math.Max(0, _rowHeights.Count - 1) + grid.Padding.VerticalThickness;
            return new Size(widthConstraint, height);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var columnWidth = ColumnWidth(bounds.Width);
            var visible = grid.Where(c => c.Visibility != Visibility.Collapsed).ToList();
            var y = bounds.Top + grid.Padding.Top;
            for (var i = 0; i < visible.Count; i++)
            {
                var row = i / grid.Columns;
                var column = i % grid.Columns;
                if (column == 0 && row > 0)
                    y += _rowHeights[row - 1] + grid.RowSpacing;

                var x = bounds.Left + grid.Padding.Left + column * (columnWidth + grid.ColumnSpacing);
                visible[i].Arrange(new Rect(x, y, columnWidth, row < _rowHeights.Count ? _rowHeights[row] : 0));
            }

            return bounds.Size;
        }
    }
}
