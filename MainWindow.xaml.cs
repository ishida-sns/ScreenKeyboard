using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenKeyboard
{
    public partial class MainWindow : Window
    {
        // Windows API imports for simulating keyboard and mouse input
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // Constants for keybd_event
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // Constants for mouse_event
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MOVE = 0x0001;

        private bool isShiftPressed = false;
        private bool isTouchpadDragging = false;
        private Point lastTouchpadPosition;

        public MainWindow()
        {
            InitializeComponent();
            SensitivitySlider.ValueChanged += SensitivitySlider_ValueChanged;
        }

        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SensitivityLabel != null)
            {
                SensitivityLabel.Text = $"{SensitivitySlider.Value:F0}x";
            }
        }

        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string key)
            {
                SendKey(key);
            }
        }

        private void SpecialKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string keyName)
            {
                if (keyName == "SHIFT")
                {
                    isShiftPressed = !isShiftPressed;
                    button.Background = isShiftPressed ? 
                        System.Windows.Media.Brushes.DarkOrange : 
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2d, 0x2d, 0x30));
                    return;
                }

                SendSpecialKey(keyName);
            }
        }

        private void SendKey(string key)
        {
            try
            {
                // For regular characters, use SendKeys
                if (key.Length == 1)
                {
                    char c = key[0];
                    byte vk = 0;

                    // Letters
                    if (c >= 'A' && c <= 'Z')
                    {
                        vk = (byte)c;
                    }
                    // Numbers
                    else if (c >= '0' && c <= '9')
                    {
                        vk = (byte)c;
                    }
                    // Special characters mapping
                    else
                    {
                        vk = GetVirtualKeyForChar(c);
                    }

                    if (vk != 0)
                    {
                        if (isShiftPressed || (c >= 'A' && c <= 'Z') || RequiresShift(c))
                        {
                            keybd_event(0x10, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Shift down
                        }

                        keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                        if (isShiftPressed || (c >= 'A' && c <= 'Z') || RequiresShift(c))
                        {
                            keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift up
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool RequiresShift(char c)
        {
            return "!@#$%^&*()_+{}|:\"<>?~".IndexOf(c) >= 0;
        }

        private byte GetVirtualKeyForChar(char c)
        {
            return c switch
            {
                ' ' => 0x20, // Space
                '!' => 0x31, // 1
                '@' => 0x32, // 2
                '#' => 0x33, // 3
                '$' => 0x34, // 4
                '%' => 0x35, // 5
                '^' => 0x36, // 6
                '&' => 0x37, // 7
                '*' => 0x38, // 8
                '(' => 0x39, // 9
                ')' => 0x30, // 0
                '-' => 0xBD, // OEM_MINUS
                '_' => 0xBD, // OEM_MINUS (with shift)
                '=' => 0xBB, // OEM_PLUS
                '+' => 0xBB, // OEM_PLUS (with shift)
                '[' => 0xDB, // OEM_4
                '{' => 0xDB, // OEM_4 (with shift)
                ']' => 0xDD, // OEM_6
                '}' => 0xDD, // OEM_6 (with shift)
                '\\' => 0xDC, // OEM_5
                '|' => 0xDC, // OEM_5 (with shift)
                ';' => 0xBA, // OEM_1
                ':' => 0xBA, // OEM_1 (with shift)
                '\'' => 0xDE, // OEM_7
                '"' => 0xDE, // OEM_7 (with shift)
                ',' => 0xBC, // OEM_COMMA
                '<' => 0xBC, // OEM_COMMA (with shift)
                '.' => 0xBE, // OEM_PERIOD
                '>' => 0xBE, // OEM_PERIOD (with shift)
                '/' => 0xBF, // OEM_2
                '?' => 0xBF, // OEM_2 (with shift)
                '`' => 0xC0, // OEM_3
                '~' => 0xC0, // OEM_3 (with shift)
                _ => 0
            };
        }

        private void SendSpecialKey(string keyName)
        {
            byte vk = keyName switch
            {
                "SPACE" => 0x20,
                "RETURN" => 0x0D,
                "BACK" => 0x08,
                "TAB" => 0x09,
                "ESCAPE" => 0x1B,
                "LCONTROL" => 0xA2,
                "LMENU" => 0xA4,
                "LWIN" => 0x5B,
                _ => 0
            };

            if (vk != 0)
            {
                keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        // Touchpad functionality
        private void Touchpad_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(TouchpadBorder);

                if (isTouchpadDragging)
                {
                    double deltaX = (currentPosition.X - lastTouchpadPosition.X) * SensitivitySlider.Value;
                    double deltaY = (currentPosition.Y - lastTouchpadPosition.Y) * SensitivitySlider.Value;

                    GetCursorPos(out POINT currentCursor);
                    int newX = currentCursor.X + (int)deltaX;
                    int newY = currentCursor.Y + (int)deltaY;
                    
                    SetCursorPos(newX, newY);
                }

                lastTouchpadPosition = currentPosition;
                isTouchpadDragging = true;
            }
        }

        private void Touchpad_LeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lastTouchpadPosition = e.GetPosition(TouchpadBorder);
            isTouchpadDragging = true;
        }

        private void Touchpad_LeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isTouchpadDragging = false;
        }

        private void Touchpad_RightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right click is handled by the button
            e.Handled = true;
        }

        private void LeftClick_Button(object sender, RoutedEventArgs e)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private void RightClick_Button(object sender, RoutedEventArgs e)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }
}
