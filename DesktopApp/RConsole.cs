using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using REngine;

namespace DesktopApp
{
    public partial class RConsole : UserControl
    {
        private readonly List<string> _commandHistory;
        private bool _commandFiredFromConsole;
        private int _lastCommandIndex;
        private int _currentCommandIndex;
        private InteractiveR _rConsole;
        public InteractiveR InteractiveRConsole
        {
            set
            {
                _rConsole = value;
                consoleFeed.Text = _rConsole.RMessage;
                _rConsole.RCommandFired += ShowRCommand;
                WriteCaret();
                SaveLastTextIndex();
            }
        }

        private void AddRText(string text)
        {
            foreach (var line in text.Split(new [] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
            {
                consoleFeed.AppendText(line);
                consoleFeed.AppendText(Environment.NewLine);
            }
        }

        private void WriteCaret()
        {
            consoleFeed.AppendText("> ");
        }

        public RConsole()
        {
            InitializeComponent();
            consoleFeed.Font = new Font(FontFamily.GenericMonospace, consoleFeed.Font.Size);
            consoleFeed.VisibleChanged += (sender, e) =>
            {
                if (consoleFeed.Visible)
                {
                    consoleFeed.SelectionStart = consoleFeed.TextLength;
                    consoleFeed.ScrollToCaret();
                }
            };
            consoleFeed.KeyDown += KeyDownPressed;

            _commandHistory = new List<string>();
            _currentCommandIndex = -1;

        }

        ~RConsole()
        {
            _rConsole.Dispose();
        }

        private void SaveLastTextIndex()
        {
            _lastCommandIndex = consoleFeed.TextLength;
        }

        private void ShowRCommand(string cmd, string response, string error)
        {
            _commandHistory.Add(cmd);
            _currentCommandIndex = _commandHistory.Count;
            if (!_commandFiredFromConsole)
            {
                AddRText(cmd);
            }
            else
            {
                _commandFiredFromConsole = false;
            }
            AddRText(response);
            AddRText(error);
            WriteCaret();
            SaveLastTextIndex();
        }

        private void RunRCommand(string cmd)
        {
            _commandFiredFromConsole = true;
            string err;
            _rConsole.RunRCommand(cmd, out err);
        }

        private void consoleFeed_MouseClick(object sender, MouseEventArgs e)
        {
            consoleFeed.SelectionStart = consoleFeed.TextLength;
            consoleFeed.ScrollToCaret();
        }

        private void SetCommandToExecute(string cmd)
        {
            consoleFeed.Text = consoleFeed.Text.Substring(0, _lastCommandIndex);
            if (!string.IsNullOrEmpty(cmd)) consoleFeed.AppendText(cmd);
        }

        private void consoleFeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Return)
            {
                var cmd = consoleFeed.Lines.Last().Trim('>', ' ');
                consoleFeed.AppendText(Environment.NewLine);
                RunRCommand(cmd);
                e.Handled = true;
            }

            if (e.KeyChar == (char)Keys.Back)
            {
                e.Handled = (consoleFeed.TextLength <=_lastCommandIndex);
            }
        }

        private void KeyDownPressed(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                if (_currentCommandIndex > 0)
                {
                    _currentCommandIndex--;
                    SetCommandToExecute(_commandHistory[_currentCommandIndex]);
                }
                e.Handled = true;
            }

            if (e.KeyCode == Keys.Down)
            {
                if (_currentCommandIndex < _commandHistory.Count-1)
                {
                    _currentCommandIndex++;
                    SetCommandToExecute(_commandHistory[_currentCommandIndex]);
                }
                e.Handled = true;
            }

            if (e.KeyCode == Keys.Left) e.Handled = true;
        }

        public void ClearConsole()
        {
            _commandHistory.Clear();
            _currentCommandIndex = -1;
            consoleFeed.Text = string.Empty;
            WriteCaret();
            SaveLastTextIndex();
        }
    }
}
