﻿using AssaDraw.Logic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AssaDraw
{
    public partial class FormAssaDrawMain : Form
    {
        public string AssaDrawCodes { get; set; }

        private readonly List<DrawCommand> _drawCommands;
        private float _zoomFactor = 1.0f;
        private DrawCommand _activeDrawCommand;
        private DrawCommand _oldDrawCommand;
        private DrawCoordinate _activePoint;
        private int _x;
        private int _y;

        private DrawHistory _history;
        private object _historyLock = new object();
        private int _historyHash;

        private DrawCoordinate _mouseDownPoint;
        private string _fileName;
        private Bitmap _backgroundImage;

        private readonly Color PointHelperColor = Color.FromArgb(100, Color.Green);
        private readonly Color PointColor = Color.FromArgb(100, Color.Red);
        private Color LineColor = Color.Black;
        private Color LineColorActive = Color.Red;

        private Timer _historyTimer;

        public FormAssaDrawMain(string text)
        {
            InitializeComponent();
            _x = int.MinValue;
            _y = int.MinValue;
            _drawCommands = new List<DrawCommand>();
            numericUpDownX.Enabled = false;
            numericUpDownY.Enabled = false;
            EnableDisableCurrentShapeActions();

            _history = new DrawHistory();

            _historyTimer = new Timer();
            _historyTimer.Interval = 250;
            _historyTimer.Tick += _historyTimer_Tick;
            _historyTimer.Start();


            if (text == "standalone")
            {
                buttonCancel.Visible = false;
                buttonOk.Visible = false;
            }
            else if (!string.IsNullOrEmpty(text))
            {
                ImportAssaDrawingFromText(text);
            }
        }

        private void EnableDisableCurrentShapeActions()
        {
            toolStripButtonClearCurrent.Enabled = _activeDrawCommand != null;
            toolStripButtonCloseShape.Enabled = _activeDrawCommand != null && _activeDrawCommand.Points.Count > 2;
            toolStripButtonMirrorHor.Enabled = _drawCommands.Count > 0 && _activeDrawCommand != null && _x == int.MinValue && _y == int.MinValue;
            toolStripButtonMirrorVert.Enabled = _drawCommands.Count > 0 && _activeDrawCommand != null && _x == int.MinValue && _y == int.MinValue;
        }

        private void pictureBoxCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxCanvas.Width < 1 || pictureBoxCanvas.Height < 1)
            {
                return;
            }

            var bitmap = _backgroundImage != null ? (Bitmap)_backgroundImage.Clone() : new Bitmap(pictureBoxCanvas.Width, pictureBoxCanvas.Height);
            var graphics = e.Graphics;
            if (_backgroundImage == null)
            {
                using (var brush = new SolidBrush(Color.LightGray))
                {
                    graphics.FillRectangle(brush, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                }
            }
            else
            {
                graphics.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            }

            DrawResolution(graphics);

            foreach (var drawCommand in _drawCommands)
            {
                if (drawCommand != _activeDrawCommand)
                {
                    Draw(drawCommand, graphics, false);
                }
                for (int i = 0; i < drawCommand.Points.Count; i++)
                {
                    DrawCoordinate point = drawCommand.Points[i];
                    using (var pen3 = new Pen(new SolidBrush(point.PointColor), 2))
                    {
                        graphics.DrawLine(pen3, new Point(point.X - 5, point.Y), new Point(point.X + 5, point.Y));
                        graphics.DrawLine(pen3, new Point(point.X, point.Y - 5), new Point(point.X, point.Y + 5));
                    }
                }
            }

            Draw(_activeDrawCommand, graphics, true);

            if (_activePoint != null)
            {
                using (var pen = new Pen(new SolidBrush(Color.FromArgb(255, _activePoint.PointColor)), 3))
                {
                    graphics.DrawLine(pen, new Point(_activePoint.X - 7, _activePoint.Y), new Point(_activePoint.X + 7, _activePoint.Y));
                    graphics.DrawLine(pen, new Point(_activePoint.X, _activePoint.Y - 7), new Point(_activePoint.X, _activePoint.Y + 7));
                }
            }

            bitmap.Dispose();
        }

        private void DrawResolution(Graphics graphics)
        {
            using (var pen = new Pen(new SolidBrush(Color.Green), 2))
            {
                graphics.DrawLine(pen, new Point(0, (int)numericUpDownHeight.Value), new Point((int)numericUpDownWidth.Value, (int)numericUpDownHeight.Value));
                graphics.DrawLine(pen, new Point((int)numericUpDownWidth.Value, 0), new Point((int)numericUpDownWidth.Value, (int)numericUpDownHeight.Value));
            }
        }

        private void Draw(DrawCommand drawCommand, Graphics graphics, bool isActive)
        {
            if (drawCommand == null || drawCommand.Points.Count == 0)
            {
                return;
            }

            var color = isActive ? LineColorActive : LineColor;
            using (var pen = new Pen(new SolidBrush(color), 2))
            {
                int i = 0;
                while (i < drawCommand.Points.Count)
                {
                    if (drawCommand.Points[i].DrawCommandType == DrawCommandType.Line)
                    {
                        if (i > 0 && i < drawCommand.Points.Count - 1 && drawCommand.Points[i].DrawCommandType == DrawCommandType.Line && drawCommand.Points[i - 1].DrawCommandType == DrawCommandType.BezierCurve)
                        {
                            graphics.DrawLine(pen, drawCommand.Points[i - 1].Point, drawCommand.Points[i].Point);

                        }
                        else if (i < drawCommand.Points.Count - 1 && drawCommand.Points[i].DrawCommandType == DrawCommandType.Line && drawCommand.Points[i + 1].DrawCommandType == DrawCommandType.Line)
                        {
                            graphics.DrawLine(pen, drawCommand.Points[i].Point, drawCommand.Points[i + 1].Point);
                        }
                        else if (i < drawCommand.Points.Count - 1)
                        {
                            graphics.DrawLine(pen, drawCommand.Points[i - 1].Point, drawCommand.Points[i].Point);
                        }

                        if (isActive && drawCommand.Points.Count > 0 && (_x != int.MinValue || _y != int.MinValue) && !_drawCommands.Contains(_activeDrawCommand))
                        {
                            using (var penNewLine = new Pen(new SolidBrush(LineColorActive), 2))
                            {
                                graphics.DrawLine(penNewLine, drawCommand.Points[drawCommand.Points.Count - 1].Point, new Point(_x, _y));
                            }
                        }

                        i++;
                        if (i >= drawCommand.Points.Count - 1)
                        {
                            var useActiveColor = drawCommand == _activeDrawCommand && _drawCommands.Contains(drawCommand);
                            using (var penClosing = new Pen(new SolidBrush(useActiveColor ? LineColorActive : LineColor), 2))
                            {
                                graphics.DrawLine(penClosing, drawCommand.Points[drawCommand.Points.Count - 1].Point, drawCommand.Points[0].Point);
                            }
                        }

                    }
                    else if (drawCommand.Points[i].DrawCommandType == DrawCommandType.BezierCurve)
                    {

                        if (drawCommand.Points.Count - i >= 3 && i > 0)
                        {
                            graphics.DrawBezier(pen, drawCommand.Points[i - 1].Point, drawCommand.Points[i].Point, drawCommand.Points[i + 1].Point, drawCommand.Points[i + 2].Point);
                            i += 2;
                        }
                        else if (drawCommand.Points.Count - i >= 3 && i == 0)
                        {
                            graphics.DrawBezier(pen, drawCommand.Points[i].Point, drawCommand.Points[i + 1].Point, drawCommand.Points[i + 2].Point, drawCommand.Points[i + 3].Point);
                            i += 3;
                        }

                        if (isActive && drawCommand.Points.Count > 0 && (_x != int.MinValue || _y != int.MinValue) && !_drawCommands.Contains(_activeDrawCommand))
                        {
                            using (var penNewLine = new Pen(new SolidBrush(LineColorActive), 2))
                            {
                                graphics.DrawLine(penNewLine, drawCommand.Points[drawCommand.Points.Count - 1].Point, new Point(_x, _y));
                            }
                        }

                        i++;
                        if (i >= drawCommand.Points.Count - 1)
                        {
                            var useActiveColor = drawCommand == _activeDrawCommand && _drawCommands.Contains(drawCommand);
                            using (var penClosing = new Pen(new SolidBrush(useActiveColor ? LineColorActive : LineColor), 2))
                            {
                                graphics.DrawLine(penClosing, drawCommand.Points[drawCommand.Points.Count - 1].Point, drawCommand.Points[0].Point);
                            }
                        }

                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        private void pictureBoxCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (_mouseDownPoint != null)
            {
                return;
            }

            _activePoint = null;
            numericUpDownX.Enabled = false;
            numericUpDownY.Enabled = false;

            if (e.Button == MouseButtons.Left && _activeDrawCommand != null && _activeDrawCommand.Points.Count > 0 && !_drawCommands.Contains(_activeDrawCommand))
            {
                // continue drawing
                if (toolStripButtonLine.Checked)
                {
                    _activeDrawCommand.AddPoint(DrawCommandType.Line, (int)Math.Round(e.Location.X / _zoomFactor), (int)Math.Round(e.Location.Y / _zoomFactor), PointColor);
                }
                else if (toolStripButtonBeizer.Checked)
                {
                    // add two support points
                    var startX = _activeDrawCommand.Points[_activeDrawCommand.Points.Count - 1].X;
                    var startY = _activeDrawCommand.Points[_activeDrawCommand.Points.Count - 1].Y;
                    var endX = (int)Math.Round(e.Location.X / _zoomFactor);
                    var endY = (int)Math.Round(e.Location.Y / _zoomFactor);
                    var oneThirdX = (int)Math.Round((endX - startX) / 3.0);
                    var oneThirdY = (int)Math.Round((endY - startY) / 3.0);
                    _activeDrawCommand.AddPoint(DrawCommandType.BezierCurve, startX + oneThirdX, startY + oneThirdY, PointHelperColor);
                    _activeDrawCommand.AddPoint(DrawCommandType.BezierCurve, startX + oneThirdX + oneThirdX, startY + oneThirdY + oneThirdY, PointHelperColor);

                    // add end point
                    _activeDrawCommand.AddPoint(DrawCommandType.BezierCurve, endX, endY, PointColor);

                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                _activePoint = null;
                numericUpDownX.Enabled = false;
                numericUpDownY.Enabled = false;

                _oldDrawCommand = null;
                if (toolStripButtonLine.Checked)
                {
                    _activeDrawCommand = new DrawCommand();
                    _activeDrawCommand.AddPoint(DrawCommandType.Line, (int)Math.Round(e.Location.X / _zoomFactor), (int)Math.Round(e.Location.Y / _zoomFactor), PointColor);
                }
                else if (toolStripButtonBeizer.Checked)
                {
                    _activeDrawCommand = new DrawCommand();
                    _activeDrawCommand.AddPoint(DrawCommandType.BezierCurve, (int)Math.Round(e.Location.X / _zoomFactor), (int)Math.Round(e.Location.Y / _zoomFactor), PointColor);
                }

            }

            pictureBoxCanvas.Invalidate();
            EnableDisableCurrentShapeActions();

        }

        private DrawCoordinate GetClosePoint(int x, int y)
        {
            var maxDiff = int.MaxValue;
            DrawCoordinate pointDiff = null;

            foreach (var drawCommand in _drawCommands)
            {
                foreach (var point in drawCommand.Points)
                {
                    var diff = Math.Abs(x - point.X) + Math.Abs(y - point.Y);
                    if (diff <= maxDiff)
                    {
                        maxDiff = diff;
                        pointDiff = point;
                    }
                }
            }

            if (maxDiff > 10)
            {
                return null;
            }

            return pointDiff;
        }

        private void pictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var x = (int)Math.Round(e.Location.X / _zoomFactor);
            var y = (int)Math.Round(e.Location.Y / _zoomFactor);
            labelPosition.Text = $"Position {x},{y}";

            if (_mouseDownPoint != null)
            {
                _mouseDownPoint.X = x;
                _mouseDownPoint.Y = y;

                foreach (TreeNode node in treeView1.Nodes)
                {
                    foreach (TreeNode subNode in node.Nodes)
                    {
                        if (subNode.Tag == _mouseDownPoint)
                        {
                            var command = "Command";
                            if (_mouseDownPoint.DrawCommand.Points[0] == _mouseDownPoint)
                            {
                                command = "Move";
                            }
                            else if (_mouseDownPoint.DrawCommandType == DrawCommandType.Line)
                            {
                                command = "Line";
                            }
                            else if (_mouseDownPoint.DrawCommandType == DrawCommandType.Move)
                            {
                                command = "Move";
                            }
                            else if (_mouseDownPoint.DrawCommandType == DrawCommandType.BezierCurve)
                            {
                                command = "Curve";
                            }

                            subNode.Text = $"{command} to {_mouseDownPoint.X} {_mouseDownPoint.Y}";
                            break;
                        }
                    }
                }

                pictureBoxCanvas.Invalidate();
                return;
            }


            if (_drawCommands.Contains(_activeDrawCommand) || _activeDrawCommand == null)
            {
                var closePoint = GetClosePoint(x, y);
                if (closePoint != null)
                {
                    Cursor = Cursors.Hand;
                    return;
                }
            }

            if (_activeDrawCommand == null && _activePoint == null)
            {
                Cursor = Cursors.Default;
                return;
            }


            Cursor = Cursors.Default;
            if (_activeDrawCommand != null)
            {
                if (_activeDrawCommand.Points.Count == 0 && _drawCommands.Contains(_activeDrawCommand))
                {
                    _activeDrawCommand.AddPoint(DrawCommandType.Line, x, y, PointColor);
                }
                else
                {
                    _x = x;
                    _y = y;
                }

                pictureBoxCanvas.Invalidate();
            }

        }

        private static string WrapInPTag(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            return $"{{\\p1}}{s.Trim()}{{\\p0}}";
        }

        private void FillTreeView(List<DrawCommand> drawCommands)
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();
            foreach (var drawCommand in drawCommands)
            {
                var node = new TreeNode("Shape");
                node.Tag = drawCommand;
                for (int i = 0; i < drawCommand.Points.Count; i++)
                {
                    var p = drawCommand.Points[i];
                    var command = "Move";
                    if (drawCommand.Points[0] == p)
                    {
                        command = "Move";
                    }
                    else if (p.DrawCommandType == DrawCommandType.Line)
                    {
                        command = "Line";
                    }
                    else if (p.DrawCommandType == DrawCommandType.BezierCurve)
                    {
                        command = "Curve";
                    }

                    if (i == 0)
                    {
                        var subNode = new TreeNode($"{command} to {p.X} {p.Y}") { Tag = p };
                        node.Nodes.Add(subNode);
                    }
                    else
                    {
                        var subNode = new TreeNode($"{command} to {p.X} {p.Y}") { Tag = p };
                        node.Nodes.Add(subNode);
                    }
                }

                treeView1.Nodes.Add(node);
            }
            treeView1.ExpandAll();
            treeView1.EndUpdate();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V)
            {
                if (Clipboard.ContainsImage())
                {
                    _backgroundImage = new Bitmap(Clipboard.GetImage());
                    numericUpDownWidth.Value = _backgroundImage.Width;
                    numericUpDownHeight.Value = _backgroundImage.Height;
                    pictureBoxCanvas.Invalidate();
                    e.SuppressKeyPress = true;
                }
                else if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (text.StartsWith("{\\p1"))
                    {
                        ImportAssaDrawingFromText(text);
                        e.SuppressKeyPress = true;
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (files.Count == 1)
                    {
                        ImportFile(files[0]);
                    }
                }
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.C)
            {
                buttonCopyAssaToClipboard_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.N)
            {
                toolStripButtonNew_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F2)
            {
                if (toolStripButtonLine.Checked)
                {
                    toolStripButtonBeizer_Click(null, null);
                }
                else
                {
                    toolStripButtonLine_Click(null, null);
                }

                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F3)
            {
                toolStripButtonLine_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F4)
            {
                toolStripButtonBeizer_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F5)
            {
                toolStripButtonCloseShape_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (_activeDrawCommand != null && e.KeyCode == Keys.Escape)
            {
                if (!_drawCommands.Contains(_activeDrawCommand))
                {
                    toolStripButtonClearCurrent_Click(null, null);
                }
                else if (_oldDrawCommand != null)
                {
                    _activeDrawCommand.Points = _oldDrawCommand.Points;
                    _oldDrawCommand = null;
                    _activeDrawCommand = null;
                    pictureBoxCanvas.Invalidate();
                }
                else
                {
                    _activeDrawCommand = null;
                }

                e.SuppressKeyPress = true;
            }
            else if (_activeDrawCommand != null && e.KeyCode == Keys.Enter)
            {
                toolStripButtonCloseShape_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (ActiveControl == treeView1 && e.KeyCode == Keys.Delete && treeView1.SelectedNode != null)
            {
                if (treeView1.SelectedNode.Tag is DrawCoordinate point)
                {
                    foreach (var drawCommand in _drawCommands)
                    {
                        if (drawCommand.Points.Contains(point))
                        {
                            drawCommand.Points.Remove(point);
                        }
                    }

                    treeView1.SelectedNode.Remove();
                    pictureBoxCanvas.Invalidate();
                }
                else if (treeView1.SelectedNode.Tag is DrawCommand)
                {
                    buttonRemoveShape_Click(null, null);
                }
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Z)
            {
                Undo();
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Y)
            {
                Redo();
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Alt || e.Modifiers == Keys.Control)
            {
                var v = e.Modifiers == Keys.Alt ? 1 : 10;

                if (e.KeyCode == Keys.Up)
                {
                    AdjustPosition(0, -v);
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    AdjustPosition(0, v);
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Left)
                {
                    AdjustPosition(-v, 0);
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Right)
                {
                    AdjustPosition(v, 0);
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void AdjustPosition(int xAdjust, int yAdjust)
        {
            if (_activeDrawCommand != null)
            {
                foreach (var p in _activeDrawCommand.Points)
                {
                    p.X += xAdjust;
                    p.Y += yAdjust;
                }

                FillTreeView(_drawCommands);
                pictureBoxCanvas.Invalidate();
                return;
            }

            foreach (var drawCommand in _drawCommands)
            {
                foreach (var p in drawCommand.Points)
                {
                    p.X += xAdjust;
                    p.Y += yAdjust;
                }

                FillTreeView(_drawCommands);
                pictureBoxCanvas.Invalidate();
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            numericUpDownX.Enabled = false;
            numericUpDownY.Enabled = false;
            var tag = e.Node.Tag;
            if (e.Node.Nodes.Count == 0 && tag is DrawCoordinate point)
            {
                numericUpDownX.Value = point.X;
                numericUpDownY.Value = point.Y;
                _activePoint = point;
                _activeDrawCommand = null;
                numericUpDownX.Enabled = true;
                numericUpDownY.Enabled = true;
            }
            else if (tag is DrawCommand command)
            {
                _activePoint = null;
                numericUpDownX.Enabled = false;
                numericUpDownY.Enabled = false;
                _activeDrawCommand = command;
                _oldDrawCommand = new DrawCommand(command);
                _x = int.MinValue;
                _y = int.MinValue;
            }
            pictureBoxCanvas.Invalidate();
            EnableDisableCurrentShapeActions();
        }

        private void numericUpDownX_ValueChanged(object sender, EventArgs e)
        {
            if (!numericUpDownX.Enabled)
            {
                return;
            }

            var p = (DrawCoordinate)treeView1.SelectedNode.Tag;
            p.X = (int)numericUpDownX.Value;
            pictureBoxCanvas.Invalidate();
        }

        private void numericUpDownY_ValueChanged(object sender, EventArgs e)
        {
            if (!numericUpDownY.Enabled)
            {
                return;
            }

            var p = (DrawCoordinate)treeView1.SelectedNode.Tag;
            p.Y = (int)numericUpDownY.Value;
            pictureBoxCanvas.Invalidate();

            var i = treeView1.SelectedNode.Index;
            if (i == 0)
            {
                treeView1.SelectedNode.Text = $"Move to {p.X} {p.Y}";
            }
            else
            {
                treeView1.SelectedNode.Text = $"Line to {p.X} {p.Y}";
            }

        }

        private void buttonRemoveShape_Click(object sender, EventArgs e)
        {
            if (_activeDrawCommand != null)
            {
                _drawCommands.Remove(_activeDrawCommand);
            }

            _drawCommands.Remove(treeView1.SelectedNode.Tag as DrawCommand);
            treeView1.Nodes.Remove(treeView1.SelectedNode);
            _activeDrawCommand = null;
            pictureBoxCanvas.Invalidate();
        }

        private void pictureBoxCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            _mouseDownPoint = null;
        }

        private void pictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            var closePoint = GetClosePoint(e.X, e.Y);
            if (closePoint != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Cursor = Cursors.Hand;
                    _activePoint = closePoint;
                    _mouseDownPoint = closePoint;
                    pictureBoxCanvas.Invalidate();
                    SelectTreeViewNodePoint(closePoint);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    _activePoint = closePoint;
                    pictureBoxCanvas.Invalidate();
                    SelectTreeViewNodePoint(closePoint);
                    contextMenuStripTreeView.Show(pictureBoxCanvas, e.X, e.Y);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                contextMenuStripCanvasBackground.Show(pictureBoxCanvas, e.X, e.Y);
            }
        }

        private void toolStripButtonCloseShape_Click(object sender, EventArgs e)
        {
            if (_activeDrawCommand == null)
            {
                pictureBoxCanvas.Invalidate();
                return;
            }

            if (_activeDrawCommand.Points.Count < 2)
            {
                _activeDrawCommand = null;
            }

            _activePoint = null;
            numericUpDownX.Enabled = false;
            numericUpDownY.Enabled = false;

            if (_activeDrawCommand != null && !_drawCommands.Contains(_activeDrawCommand))
            {
                _drawCommands.Add(_activeDrawCommand);
            }

            FillTreeView(_drawCommands);
            _activeDrawCommand = null;
            _x = int.MinValue;
            _y = int.MinValue;
            pictureBoxCanvas.Invalidate();
            EnableDisableCurrentShapeActions();
        }

        private void toolStripButtonClearCurrent_Click(object sender, EventArgs e)
        {
            if (_activeDrawCommand != null)
            {
                _drawCommands.Remove(_activeDrawCommand);
                EnableDisableCurrentShapeActions();
            }

            if (treeView1.SelectedNode?.Tag is DrawCommand drawCommand)
            {
                _drawCommands.Remove(drawCommand);
                treeView1.Nodes.Remove(treeView1.SelectedNode);
            }

            _activeDrawCommand = null;
            pictureBoxCanvas.Invalidate();
            _x = int.MinValue;
            _y = int.MinValue;
            EnableDisableCurrentShapeActions();
        }

        private void toolStripButtonClearAll_Click(object sender, EventArgs e)
        {

        }

        private void ClearAll()
        {
            _x = int.MinValue;
            _y = int.MinValue;
            treeView1.Nodes.Clear();
            _activeDrawCommand = null;
            _oldDrawCommand = null;
            _activePoint = null;
            numericUpDownX.Enabled = false;
            numericUpDownY.Enabled = false;
            _drawCommands.Clear();
            pictureBoxCanvas.Invalidate();
        }

        private string GetAssaDrawCode()
        {
            var sb = new StringBuilder();
            foreach (var drawCommand in _drawCommands)
            {
                sb.AppendLine(drawCommand.ToAssa());
            }

            return WrapInPTag(sb.ToString());
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            var code = GetAssaDrawCode();
            if (string.IsNullOrEmpty(code))
            {
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.FileName = System.IO.Path.GetFileName(_fileName);
                saveFileDialog.Filter = "ASSA drawing|*.assadraw";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _fileName = saveFileDialog.FileName;
                    System.IO.File.WriteAllText(_fileName, code);
                }
            }
        }

        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.FileName = string.Empty;
                openFileDialog.Filter = "ASSA drawing|*.assadraw";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClearAll();
                    ImportAssaDrawing(openFileDialog.FileName);
                }
            }
        }

        private void ImportAssaDrawing(string fileName)
        {
            _activeDrawCommand = null;
            _fileName = fileName;
            var text = System.IO.File.ReadAllText(_fileName);
            ImportAssaDrawingFromText(text);
        }

        private void ImportAssaDrawingFromText(string text)
        {
            text = text.Replace("{\\p1}", string.Empty);
            text = text.Replace("{\\p0}", string.Empty);
            var arr = text.Split();
            int i = 0;
            int beizerCount = 0;
            var state = DrawCommandType.None;
            DrawCoordinate moveCoordinate = null;
            DrawCommand drawCommand = null;
            while (i < arr.Length)
            {
                var v = arr[i];
                if (v == "m" && i < arr.Length - 2 &&
                    float.TryParse(arr[i + 1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var mX) &&
                    float.TryParse(arr[i + 2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var mY))
                {
                    beizerCount = 0;
                    moveCoordinate = new DrawCoordinate(null, DrawCommandType.Move, (int)Math.Round(mX), (int)Math.Round(mY), PointColor);
                    state = DrawCommandType.Move;
                    i += 2;
                }
                else if (v == "l")
                {
                    state = DrawCommandType.Line;
                    beizerCount = 0;
                    if (moveCoordinate != null)
                    {
                        drawCommand = new DrawCommand();
                        drawCommand.AddPoint(state, moveCoordinate.X, moveCoordinate.Y, PointColor);
                        moveCoordinate = null;
                        _drawCommands.Add(drawCommand);
                    }
                }
                else if (v == "b")
                {
                    state = DrawCommandType.BezierCurve;
                    if (moveCoordinate != null)
                    {
                        drawCommand = new DrawCommand();
                        drawCommand.AddPoint(state, moveCoordinate.X, moveCoordinate.Y, PointColor);
                        moveCoordinate = null;
                        _drawCommands.Add(drawCommand);
                    }
                    beizerCount = 1;
                }
                else if (state == DrawCommandType.Line && drawCommand != null && i < arr.Length - 1 &&
                    float.TryParse(arr[i + 0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var lX) &&
                    float.TryParse(arr[i + 1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var lY))
                {
                    drawCommand.AddPoint(state, (int)Math.Round(lX), (int)Math.Round(lY), PointColor);
                    i++;
                }
                else if (state == DrawCommandType.BezierCurve && drawCommand != null &&
                    float.TryParse(arr[i + 0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bX) &&
                    float.TryParse(arr[i + 1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bY))
                {
                    beizerCount++;
                    if (beizerCount > 3)
                    {
                        beizerCount = 1;
                    }

                    drawCommand.AddPoint(state, (int)Math.Round(bX), (int)Math.Round(bY), (beizerCount == 2 || beizerCount == 3) ? PointHelperColor : PointColor);
                    i++;
                }

                i++;
            }

            FillTreeView(_drawCommands);
            _x = int.MinValue;
            _y = int.MinValue;
            pictureBoxCanvas.Invalidate();
        }

        private void FormAssaDrawMain_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void FormAssaDrawMain_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    ImportFile(files[0]);
                }
            }
        }

        private void ImportFile(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".assadraw")
            {
                ImportAssaDrawing(fileName);
            }

            if (ext == ".png" || ext == ".jpg" || ext == ".bmp" || ext == ".tiff" || ext == ".jpe" || ext == ".jpeg" || ext == ".gif")
            {
                SetBackgroundImage(fileName);
            }
        }

        private void SetBackgroundImage(string fileName)
        {
            _backgroundImage?.Dispose();
            _backgroundImage = new Bitmap(fileName);
            numericUpDownWidth.Value = _backgroundImage.Width;
            numericUpDownHeight.Value = _backgroundImage.Height;
            pictureBoxCanvas.Invalidate();
        }

        private void toolStripButtonLine_Click(object sender, EventArgs e)
        {
            toolStripButtonBeizer.Checked = false;
            toolStripButtonLine.Checked = true;
        }

        private void toolStripButtonBeizer_Click(object sender, EventArgs e)
        {
            toolStripButtonLine.Checked = false;
            toolStripButtonBeizer.Checked = true;
        }

        private void buttonCopyAssaToClipboard_Click(object sender, EventArgs e)
        {
            var text = GetAssaDrawCode();
            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
            pictureBoxCanvas.Focus();
        }

        private void _historyTimer_Tick(object sender, EventArgs e)
        {
            lock (_historyLock)
            {
                var newHistoryItem = _history.MakeHistoryItem(_drawCommands, _activeDrawCommand, _oldDrawCommand, _activePoint, _zoomFactor);
                var newHash = newHistoryItem.GetFastHashCode();
                if (newHash == _historyHash)
                {
                    return;
                }

                _historyHash = newHash;
                if (_historyHash == 0)
                {
                    return;
                }

                _history.AddChange(newHistoryItem);

            }
        }

        private void SetPropertiesFromHistory(DrawHistoryItem item)
        {
            _drawCommands.Clear();
            _drawCommands.AddRange(item.DrawCommands);
            _activeDrawCommand = item.ActiveDrawCommand;
            _oldDrawCommand = item.OldDrawCommand;
            _activePoint = item.ActivePoint;
            _zoomFactor = item.ZoomFactor;
        }

        private void Undo()
        {
            lock (_historyLock)
            {
                var item = _history.Undo();
                if (item == null)
                {
                    return;
                }

                SetPropertiesFromHistory(item);
                _historyHash = item.GetFastHashCode();
            }

            pictureBoxCanvas.Invalidate();
            FillTreeView(_drawCommands);
        }

        private void Redo()
        {
            lock (_historyLock)
            {
                var item = _history.Redo();
                if (item == null)
                {
                    return;
                }

                SetPropertiesFromHistory(item);
                _historyHash = item.GetFastHashCode();
            }

            pictureBoxCanvas.Invalidate();
            FillTreeView(_drawCommands);
        }

        private void contextMenuStripTreeView_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                e.Cancel = true;
                return;
            }

            deleteShapeToolStripMenuItem.Visible = false;
            deletePointToolStripMenuItem.Visible = false;
            duplicatePointToolStripMenuItem.Visible = false;
            if (treeView1.SelectedNode.Tag is DrawCoordinate point)
            {
                if (point.DrawCommandType == DrawCommandType.Line)
                {
                    duplicatePointToolStripMenuItem.Visible = true;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else if (treeView1.SelectedNode.Tag is DrawCommand)
            {
                deleteShapeToolStripMenuItem.Visible = true;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void deleteShapeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButtonClearCurrent_Click(null, null);
        }

        private void SelectTreeViewNodePoint(DrawCoordinate point)
        {
            foreach (TreeNode node in treeView1.Nodes)
            {
                foreach (TreeNode subNode in node.Nodes)
                {
                    if (subNode.Tag == point)
                    {
                        treeView1.SelectedNode = subNode;
                        return;
                    }
                }
            }
        }

        private void duplicatePointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.Tag is DrawCoordinate point)
            {
                if (point.DrawCommandType == DrawCommandType.Line)
                {
                    var newPoint = new DrawCoordinate(point.DrawCommand, DrawCommandType.Line, point.X, point.Y, point.PointColor);
                    var idx = point.DrawCommand.Points.IndexOf(point) + 1;
                    point.DrawCommand.Points.Insert(idx, newPoint);
                    duplicatePointToolStripMenuItem.Visible = true;
                    _activePoint = newPoint;

                    FillTreeView(_drawCommands);
                    SelectTreeViewNodePoint(_activePoint);
                    pictureBoxCanvas.Invalidate();
                }
            }
        }

        private void toolStripButtonNew_Click(object sender, EventArgs e)
        {
            if (_drawCommands.Count == 0)
            {
                return;
            }

            var result = MessageBox.Show(this, "Clear all shapes?", "", MessageBoxButtons.YesNoCancel);
            if (result != DialogResult.Yes)
            {
                return;
            }

            ClearAll();
        }

        private void toolStripButtonMirrorHor_Click(object sender, EventArgs e)
        {
            if (_activeDrawCommand == null)
            {
                return;
            }

            var maxY = _activeDrawCommand.Points.Max(p => p.Y);
            var newDrawing = new DrawCommand();
            foreach (var p in _activeDrawCommand.Points)
            {
                newDrawing.AddPoint(p.DrawCommandType, p.X, maxY + maxY - p.Y, p.PointColor);
            }

            _drawCommands.Add(newDrawing);
            _activeDrawCommand = newDrawing;
            FillTreeView(_drawCommands);
            pictureBoxCanvas.Invalidate();
        }

        private void toolStripButtonMirrorVert_Click(object sender, EventArgs e)
        {
            if (_activeDrawCommand == null)
            {
                return;
            }

            var maxX = _activeDrawCommand.Points.Max(p => p.X);
            var newDrawing = new DrawCommand();
            foreach (var p in _activeDrawCommand.Points)
            {
                newDrawing.AddPoint(p.DrawCommandType, maxX + maxX - p.X, p.Y, p.PointColor);
            }

            _drawCommands.Add(newDrawing);
            _activeDrawCommand = newDrawing;
            FillTreeView(_drawCommands);
            pictureBoxCanvas.Invalidate();
            _x = int.MinValue;
            _y = int.MinValue;
        }

        private void chooseBackgroundImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.FileName = string.Empty;
                openFileDialog.Filter = "Image files|*.png;*.jpg;*.bmp;*.tiff;*.jpe;*.jpeg;*.gif";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SetBackgroundImage(openFileDialog.FileName);
                }
            }
        }

        private void clearBackgroundImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _backgroundImage?.Dispose();
            _backgroundImage = null;
            pictureBoxCanvas.Invalidate();
        }

        private void contextMenuStripCanvasBackground_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            clearBackgroundImageToolStripMenuItem.Visible = _backgroundImage != null;
        }

        private void toolStripButtonSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new FormAssaDrawSettings(LineColor, LineColorActive))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    LineColor = settingsForm.LineColor;
                    LineColorActive = settingsForm.LineColorActive;
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            using (var helpForm = new FormAssaDrawHelp())
            {
                helpForm.ShowDialog();
                pictureBoxCanvas.Invalidate();
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            AssaDrawCodes = GetAssaDrawCode();
            DialogResult = DialogResult.OK;
        }
    }
}
