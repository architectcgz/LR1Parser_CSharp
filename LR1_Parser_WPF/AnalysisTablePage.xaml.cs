using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Exp3_WPF;

public partial class AnalysisTablePage : Window
{
    private Dictionary<(int, char), string> _actionTable;
    private Dictionary<(int, string), int> _gotoTable;

    public AnalysisTablePage(Dictionary<(int, char), string> actionTable, Dictionary<(int, string), int> gotoTable)
    {
        InitializeComponent();
        _actionTable = actionTable;
        _gotoTable = gotoTable;
        DrawTable();
    }

        private void DrawTable()
        {
            double cellWidth = 50;
            double cellHeight = 30;
            double startX = 10;
            double startY = 10;

            // Extract unique states and headers dynamically from dictionaries
            var states = _actionTable.Keys.Select(k => k.Item1).Union(_gotoTable.Keys.Select(k => k.Item1)).Distinct().OrderBy(s => s).ToList();
            var actionHeaders = _actionTable.Keys.Select(k => k.Item2).Distinct().OrderBy(h => h).ToArray();
            var gotoHeaders = _gotoTable.Keys.Select(k => k.Item2).Distinct().OrderBy(h => h).ToArray();

            // Calculate positions for ACTION and GOTO labels
            double actionLabelWidth = actionHeaders.Length * cellWidth;
            double gotoLabelWidth = gotoHeaders.Length * cellWidth;

            // Draw "ACTION" label spanning multiple columns
            DrawCell(TableCanvas, startX + cellWidth, startY, actionLabelWidth, cellHeight, "ACTION");

            // Draw "GOTO" label spanning multiple columns
            DrawCell(TableCanvas, startX + cellWidth + actionLabelWidth, startY, gotoLabelWidth, cellHeight, "GOTO");

            // Draw column headers for ACTION
            for (int i = 0; i < actionHeaders.Length; i++)
            {
                DrawCell(TableCanvas, startX + (i + 1) * cellWidth, startY + cellHeight, cellWidth, cellHeight, actionHeaders[i].ToString());
            }

            // Draw column headers for GOTO
            for (int i = 0; i < gotoHeaders.Length; i++)
            {
                DrawCell(TableCanvas, startX + (actionHeaders.Length + 1 + i) * cellWidth, startY + cellHeight, cellWidth, cellHeight, gotoHeaders[i]);
            }

            // Draw row headers and fill cells with ACTION and GOTO values
            for (int row = 0; row < states.Count; row++)
            {
                int state = states[row];
                DrawCell(TableCanvas, startX, startY + (row + 2) * cellHeight, cellWidth, cellHeight, state.ToString());

                for (int col = 0; col < actionHeaders.Length; col++)
                {
                    char actionHeader = actionHeaders[col];
                    _actionTable.TryGetValue((state, actionHeader), out string actionValue);
                    DrawCell(TableCanvas, startX + (col + 1) * cellWidth, startY + (row + 2) * cellHeight, cellWidth, cellHeight, actionValue);
                }

                for (int col = 0; col < gotoHeaders.Length; col++)
                {
                    string gotoHeader = gotoHeaders[col];
                    bool hasValue = _gotoTable.TryGetValue((state, gotoHeader), out int gotoValue);
                    DrawCell(TableCanvas, startX + (actionHeaders.Length + 1 + col) * cellWidth, startY + (row + 2) * cellHeight, cellWidth, cellHeight, hasValue ? gotoValue.ToString() : string.Empty);
                }
            }
        }

        private void DrawCell(Canvas canvas, double x, double y, double width, double height, string text)
        {
            // Draw the rectangle
            Rectangle rect = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = Brushes.Black
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            canvas.Children.Add(rect);

            // Draw the text
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                Width = width,
                Height = height,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            canvas.Children.Add(textBlock);
        }
}


    