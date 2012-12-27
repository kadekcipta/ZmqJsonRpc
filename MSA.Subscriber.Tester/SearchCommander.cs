using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace MSA.Subscriber.Tester
{
    public class SearchCommander : IDisposable
    {
        private const int COUNTER_LIMIT = 10;
        private const int MIN_CHARS_TRIGGER = 3;

        private TextBox _textbox;
        private Timer _timer;
        private Action<string> _callback;
        private int _thresholdCounter;
        private string _lastKeywords;

        public SearchCommander(TextBox textbox, Action<string> callback)
        {
            _lastKeywords = String.Empty;
            _thresholdCounter = 0;
            _textbox = textbox;
            _callback = callback;

            _textbox.KeyPress += (sender, ea) =>
            {
                _thresholdCounter = 0;
            };

            _timer = new Timer();
            _timer.Interval = 10;
            _timer.Tick += (sender, arg) =>
            {
                _thresholdCounter++;
                if (_thresholdCounter > COUNTER_LIMIT)
                {
                    _thresholdCounter = 0;
                    if (_textbox.Text.Length >= MIN_CHARS_TRIGGER && !_lastKeywords.Equals(_textbox.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        _lastKeywords = _textbox.Text;
                        _callback(_textbox.Text);

                        // monitor the request traffic
                        System.Diagnostics.Debug.WriteLine("Triggering " + _lastKeywords);
                    }
                }
            };

            _timer.Start();
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }
    }    
}
