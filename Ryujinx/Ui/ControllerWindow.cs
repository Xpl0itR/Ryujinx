using Gtk;
using JsonPrettyPrinterPlus;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Ryujinx.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.HLE.FileSystem;
using Utf8Json;
using Utf8Json.Resolvers;

using GUI = Gtk.Builder.ObjectAttribute;
using Key = Ryujinx.Configuration.Hid.Key;

namespace Ryujinx.Ui
{
    public class ControllerWindow : Window
    {
        private static ControllerId _controllerId;
        private static object _inputConfig;
        private static Gdk.Key? _pressedKey;
        private static bool _isWaitingForInput;
        private static IJsonFormatterResolver _resolver;

#pragma warning disable CS0649
#pragma warning disable IDE0044
        [GUI] Adjustment   _controllerDeadzoneLeft;
        [GUI] Adjustment   _controllerDeadzoneRight;
        [GUI] Adjustment   _controllerTriggerThreshold;
        [GUI] ComboBoxText _inputDevice;
        [GUI] ComboBoxText _profile;
        [GUI] ToggleButton _refreshInputDevicesButton;
        [GUI] Box          _settingsBox;
        [GUI] Grid         _leftStickKeyboard;
        [GUI] Grid         _leftStickController;
        [GUI] Box          _deadZoneLeftBox;
        [GUI] Grid         _rightStickKeyboard;
        [GUI] Grid         _rightStickController;
        [GUI] Box          _deadZoneRightBox;
        [GUI] Grid         _leftSideTriggerBox;
        [GUI] Grid         _rightSideTriggerBox;
        [GUI] Box          _triggerThresholdBox;
        [GUI] ComboBoxText _controllerType;
        [GUI] ToggleButton _lStickX;
        [GUI] ToggleButton _lStickY;
        [GUI] ToggleButton _lStickUp;
        [GUI] ToggleButton _lStickDown;
        [GUI] ToggleButton _lStickLeft;
        [GUI] ToggleButton _lStickRight;
        [GUI] ToggleButton _lStickButton;
        [GUI] ToggleButton _dpadUp;
        [GUI] ToggleButton _dpadDown;
        [GUI] ToggleButton _dpadLeft;
        [GUI] ToggleButton _dpadRight;
        [GUI] ToggleButton _minus;
        [GUI] ToggleButton _l;
        [GUI] ToggleButton _zL;
        [GUI] ToggleButton _rStickX;
        [GUI] ToggleButton _rStickY;
        [GUI] ToggleButton _rStickUp;
        [GUI] ToggleButton _rStickDown;
        [GUI] ToggleButton _rStickLeft;
        [GUI] ToggleButton _rStickRight;
        [GUI] ToggleButton _rStickButton;
        [GUI] ToggleButton _a;
        [GUI] ToggleButton _b;
        [GUI] ToggleButton _x;
        [GUI] ToggleButton _y;
        [GUI] ToggleButton _plus;
        [GUI] ToggleButton _r;
        [GUI] ToggleButton _zR;
        [GUI] ToggleButton _lSl;
        [GUI] ToggleButton _lSr;
        [GUI] ToggleButton _rSl;
        [GUI] ToggleButton _rSr;
        [GUI] Image        _controllerImage;
#pragma warning restore CS0649
#pragma warning restore IDE0044

        public ControllerWindow(ControllerId controllerId) : this(new Builder("Ryujinx.Ui.ControllerWindow.glade"), controllerId) { }

        private ControllerWindow(Builder builder, ControllerId controllerId) : base(builder.GetObject("_controllerWin").Handle)
        {
            builder.Autoconnect(this);

            this.Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");

            _controllerId = controllerId;

            _inputConfig = ConfigurationState.Instance.Hid.InputConfig.Value.Find(inputConfig =>
            {
                if (inputConfig is NpadController controllerConfig)
                {
                    return controllerConfig.ControllerId == _controllerId;
                }
                else if (inputConfig is NpadKeyboard keyboardConfig)
                {
                    return keyboardConfig.ControllerId == _controllerId;
                }
                else
                {
                    return false;
                }
            });

            _resolver = CompositeResolver.Create(
                new[] { new ConfigurationFileFormat.ConfigurationEnumFormatter<Key>() },
                new[] { StandardResolver.AllowPrivateSnakeCase }
            );

            //Bind Events
            _lStickX.Clicked        += Button_Pressed;
            _lStickY.Clicked        += Button_Pressed;
            _lStickUp.Clicked       += Button_Pressed;
            _lStickDown.Clicked     += Button_Pressed;
            _lStickLeft.Clicked     += Button_Pressed;
            _lStickRight.Clicked    += Button_Pressed;
            _lStickButton.Clicked   += Button_Pressed;
            _dpadUp.Clicked         += Button_Pressed;
            _dpadDown.Clicked       += Button_Pressed;
            _dpadLeft.Clicked       += Button_Pressed;
            _dpadRight.Clicked      += Button_Pressed;
            _minus.Clicked          += Button_Pressed;
            _l.Clicked              += Button_Pressed;
            _zL.Clicked             += Button_Pressed;
            _lSl.Clicked            += Button_Pressed;
            _lSr.Clicked            += Button_Pressed;
            _rStickX.Clicked        += Button_Pressed;
            _rStickY.Clicked        += Button_Pressed;
            _rStickUp.Clicked       += Button_Pressed;
            _rStickDown.Clicked     += Button_Pressed;
            _rStickLeft.Clicked     += Button_Pressed;
            _rStickRight.Clicked    += Button_Pressed;
            _rStickButton.Clicked   += Button_Pressed;
            _a.Clicked              += Button_Pressed;
            _b.Clicked              += Button_Pressed;
            _x.Clicked              += Button_Pressed;
            _y.Clicked              += Button_Pressed;
            _plus.Clicked           += Button_Pressed;
            _r.Clicked              += Button_Pressed;
            _zR.Clicked             += Button_Pressed;
            _rSl.Clicked            += Button_Pressed;
            _rSr.Clicked            += Button_Pressed;

            // Setup current values
            UpdateInputDeviceList();
            SetAvailableOptions();
        }

        private void UpdateInputDeviceList()
        {
            _inputDevice.RemoveAll();
            _inputDevice.Append("disabled", "Disabled");

            //TODO: Remove this line and uncomment the loop below when the keyboard API is implemented in OpenTK.
            _inputDevice.Append("keyboard/0", "Keyboard/0");
            /*for (int i = 0; Keyboard.GetState(i).IsConnected; i++)
            {
                _inputDevice.Append($"keyboard/{i}", $"Keyboard/{i}");
            }*/

            for (int i = 0; GamePad.GetState(i).IsConnected; i++)
            {
                _inputDevice.Append($"controller/{i}", $"Controller/{i} ({GamePad.GetName(i)})");
            }

            if (_inputConfig is NpadKeyboard keyboard)
            {
                _inputDevice.SetActiveId($"keyboard/{keyboard.Index}");
            }
            else if (_inputConfig is NpadController controller)
            {
                _inputDevice.SetActiveId($"controller/{controller.Index}");
            }
            else
            {
                _inputDevice.SetActiveId("disabled");
            }
        }

        private void SetAvailableOptions()
        {
            if (_inputDevice.ActiveId != null && _inputDevice.ActiveId.StartsWith("keyboard"))
            {
                this.ShowAll();
                _leftStickController.Hide();
                _rightStickController.Hide();
                _deadZoneLeftBox.Hide();
                _deadZoneRightBox.Hide();
                _triggerThresholdBox.Hide();

                SetCurrentValues();
            }
            else if (_inputDevice.ActiveId != null && _inputDevice.ActiveId.StartsWith("controller"))
            {
                this.ShowAll();
                _leftStickKeyboard.Hide();
                _rightStickKeyboard.Hide();

                SetCurrentValues();
            }
            else
            {
                _settingsBox.Hide();
            }
        }

        private void SetCurrentValues()
        {
            ClearValues();

            SetControllerSpecificFields();

            SetProfiles();

            if (_inputDevice.ActiveId.StartsWith("keyboard") && _inputConfig is NpadKeyboard)
            {
                SetValues(_inputConfig);
            }
            else if (_inputDevice.ActiveId.StartsWith("controller") && _inputConfig is NpadController)
            {
                SetValues(_inputConfig);
            }
        }

        private void SetControllerSpecificFields()
        {
            _leftSideTriggerBox.Hide();
            _rightSideTriggerBox.Hide();

            if (_controllerType.ActiveId == "NpadLeft")
            {
                _leftSideTriggerBox.Show();
            }
            else if (_controllerType.ActiveId == "NpadRight")
            {
                _rightSideTriggerBox.Show();
            }

            switch (_controllerType.ActiveId)
            {
                case "ProController":
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.ProCon.png", 400, 400);
                    break;
                case "NpadLeft":
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.BlueCon.png", 400, 400);
                    break;
                case "NpadRight":
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.RedCon.png", 400, 400);
                    break;
                default:
                    _controllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.JoyCon.png", 400, 400);
                    break;
            }
        }

        private void ClearValues()
        {
            _lStickX.Label                    = "";
            _lStickY.Label                    = "";
            _lStickUp.Label                   = "";
            _lStickDown.Label                 = "";
            _lStickLeft.Label                 = "";
            _lStickRight.Label                = "";
            _lStickButton.Label               = "";
            _dpadUp.Label                     = "";
            _dpadDown.Label                   = "";
            _dpadLeft.Label                   = "";
            _dpadRight.Label                  = "";
            _minus.Label                      = "";
            _l.Label                          = "";
            _zL.Label                         = "";
            _lSl.Label                        = "";
            _lSr.Label                        = "";
            _rStickUp.Label                   = "";
            _rStickDown.Label                 = "";
            _rStickLeft.Label                 = "";
            _rStickRight.Label                = "";
            _rStickButton.Label               = "";
            _a.Label                          = "";
            _b.Label                          = "";
            _x.Label                          = "";
            _y.Label                          = "";
            _plus.Label                       = "";
            _r.Label                          = "";
            _zR.Label                         = "";
            _rSl.Label                        = "";
            _rSr.Label                        = "";
            _controllerDeadzoneLeft.Value     = 0;
            _controllerDeadzoneRight.Value    = 0;
            _controllerTriggerThreshold.Value = 0;
        }

        private void SetValues(object config)
        {
            if (config is NpadKeyboard keyboardConfig)
            {
                if (!_inputDevice.ActiveId.StartsWith("keyboard"))
                {
                    _inputDevice.SetActiveId($"keyboard/{keyboardConfig.Index}");
                }
                
                _controllerType.SetActiveId(keyboardConfig.ControllerType.ToString());

                _lStickUp.Label     = keyboardConfig.LeftJoycon.StickUp.ToString();
                _lStickDown.Label   = keyboardConfig.LeftJoycon.StickDown.ToString();
                _lStickLeft.Label   = keyboardConfig.LeftJoycon.StickLeft.ToString();
                _lStickRight.Label  = keyboardConfig.LeftJoycon.StickRight.ToString();
                _lStickButton.Label = keyboardConfig.LeftJoycon.StickButton.ToString();
                _dpadUp.Label       = keyboardConfig.LeftJoycon.DPadUp.ToString();
                _dpadDown.Label     = keyboardConfig.LeftJoycon.DPadDown.ToString();
                _dpadLeft.Label     = keyboardConfig.LeftJoycon.DPadLeft.ToString();
                _dpadRight.Label    = keyboardConfig.LeftJoycon.DPadRight.ToString();
                _minus.Label        = keyboardConfig.LeftJoycon.ButtonMinus.ToString();
                _l.Label            = keyboardConfig.LeftJoycon.ButtonL.ToString();
                _zL.Label           = keyboardConfig.LeftJoycon.ButtonZl.ToString();
                _lSl.Label          = keyboardConfig.LeftJoycon.ButtonSl.ToString();
                _lSr.Label          = keyboardConfig.LeftJoycon.ButtonSr.ToString();
                _rStickUp.Label     = keyboardConfig.RightJoycon.StickUp.ToString();
                _rStickDown.Label   = keyboardConfig.RightJoycon.StickDown.ToString();
                _rStickLeft.Label   = keyboardConfig.RightJoycon.StickLeft.ToString();
                _rStickRight.Label  = keyboardConfig.RightJoycon.StickRight.ToString();
                _rStickButton.Label = keyboardConfig.RightJoycon.StickButton.ToString();
                _a.Label            = keyboardConfig.RightJoycon.ButtonA.ToString();
                _b.Label            = keyboardConfig.RightJoycon.ButtonB.ToString();
                _x.Label            = keyboardConfig.RightJoycon.ButtonX.ToString();
                _y.Label            = keyboardConfig.RightJoycon.ButtonY.ToString();
                _plus.Label         = keyboardConfig.RightJoycon.ButtonPlus.ToString();
                _r.Label            = keyboardConfig.RightJoycon.ButtonR.ToString();
                _zR.Label           = keyboardConfig.RightJoycon.ButtonZr.ToString();
                _rSl.Label          = keyboardConfig.RightJoycon.ButtonSl.ToString();
                _rSr.Label          = keyboardConfig.RightJoycon.ButtonSr.ToString();
            }
            else if (config is NpadController controllerConfig)
            {
                if (!_inputDevice.ActiveId.StartsWith("controller"))
                {
                    _inputDevice.SetActiveId($"controller/{controllerConfig.Index}");
                }

                _controllerType.SetActiveId(controllerConfig.ControllerType.ToString());

                _lStickX.Label                    = controllerConfig.LeftJoycon.StickX.ToString();
                _lStickY.Label                    = controllerConfig.LeftJoycon.StickY.ToString();
                _lStickButton.Label               = controllerConfig.LeftJoycon.StickButton.ToString();
                _dpadUp.Label                     = controllerConfig.LeftJoycon.DPadUp.ToString();
                _dpadDown.Label                   = controllerConfig.LeftJoycon.DPadDown.ToString();
                _dpadLeft.Label                   = controllerConfig.LeftJoycon.DPadLeft.ToString();
                _dpadRight.Label                  = controllerConfig.LeftJoycon.DPadRight.ToString();
                _minus.Label                      = controllerConfig.LeftJoycon.ButtonMinus.ToString();
                _l.Label                          = controllerConfig.LeftJoycon.ButtonL.ToString();
                _zL.Label                         = controllerConfig.LeftJoycon.ButtonZl.ToString();
                _lSl.Label                        = controllerConfig.LeftJoycon.ButtonSl.ToString();
                _lSr.Label                        = controllerConfig.LeftJoycon.ButtonSr.ToString();
                _rStickX.Label                    = controllerConfig.RightJoycon.StickX.ToString();
                _rStickY.Label                    = controllerConfig.RightJoycon.StickY.ToString();
                _rStickButton.Label               = controllerConfig.RightJoycon.StickButton.ToString();
                _a.Label                          = controllerConfig.RightJoycon.ButtonA.ToString();
                _b.Label                          = controllerConfig.RightJoycon.ButtonB.ToString();
                _x.Label                          = controllerConfig.RightJoycon.ButtonX.ToString();
                _y.Label                          = controllerConfig.RightJoycon.ButtonY.ToString();
                _plus.Label                       = controllerConfig.RightJoycon.ButtonPlus.ToString();
                _r.Label                          = controllerConfig.RightJoycon.ButtonR.ToString();
                _zR.Label                         = controllerConfig.RightJoycon.ButtonZr.ToString();
                _rSl.Label                        = controllerConfig.RightJoycon.ButtonSl.ToString();
                _rSr.Label                        = controllerConfig.RightJoycon.ButtonSr.ToString();
                _controllerDeadzoneLeft.Value     = controllerConfig.DeadzoneLeft;
                _controllerDeadzoneRight.Value    = controllerConfig.DeadzoneRight;
                _controllerTriggerThreshold.Value = controllerConfig.TriggerThreshold;
            }
        }

        private object GetValues()
        {
            if (_inputDevice.ActiveId.StartsWith("keyboard"))
            {
                NpadKeyboard keyboardConfig = new NpadKeyboard
                {
                    Index          = int.Parse(_inputDevice.ActiveId.Split("/")[1]),
                    ControllerType = Enum.Parse<ControllerType>(_controllerType.ActiveId),
                    ControllerId   = _controllerId,
                    LeftJoycon     = new NpadKeyboardLeft(),
                    RightJoycon    = new NpadKeyboardRight(),
                    Hotkeys        = new KeyboardHotkeys
                    {
                        ToggleVsync = Key.Tab //TODO: Make this an option in the GUI
                    }
                };

                Enum.TryParse(_lStickUp.Label,     out keyboardConfig.LeftJoycon.StickLeft);
                Enum.TryParse(_lStickDown.Label,   out keyboardConfig.LeftJoycon.StickDown);
                Enum.TryParse(_lStickLeft.Label,   out keyboardConfig.LeftJoycon.StickLeft);
                Enum.TryParse(_lStickRight.Label,  out keyboardConfig.LeftJoycon.StickRight);
                Enum.TryParse(_lStickButton.Label, out keyboardConfig.LeftJoycon.StickButton);
                Enum.TryParse(_dpadUp.Label,       out keyboardConfig.LeftJoycon.DPadUp);
                Enum.TryParse(_dpadDown.Label,     out keyboardConfig.LeftJoycon.DPadDown);
                Enum.TryParse(_dpadLeft.Label,     out keyboardConfig.LeftJoycon.DPadLeft);
                Enum.TryParse(_dpadRight.Label,    out keyboardConfig.LeftJoycon.DPadRight);
                Enum.TryParse(_minus.Label,        out keyboardConfig.LeftJoycon.ButtonMinus);
                Enum.TryParse(_l.Label,            out keyboardConfig.LeftJoycon.ButtonL);
                Enum.TryParse(_zL.Label,           out keyboardConfig.LeftJoycon.ButtonZl);
                Enum.TryParse(_lSl.Label,          out keyboardConfig.LeftJoycon.ButtonSl);
                Enum.TryParse(_lSr.Label,          out keyboardConfig.LeftJoycon.ButtonSr);

                Enum.TryParse(_rStickUp.Label,     out keyboardConfig.RightJoycon.StickUp);
                Enum.TryParse(_rStickDown.Label,   out keyboardConfig.RightJoycon.StickDown);
                Enum.TryParse(_rStickLeft.Label,   out keyboardConfig.RightJoycon.StickLeft);
                Enum.TryParse(_rStickRight.Label,  out keyboardConfig.RightJoycon.StickRight);
                Enum.TryParse(_rStickButton.Label, out keyboardConfig.RightJoycon.StickButton);
                Enum.TryParse(_a.Label,            out keyboardConfig.RightJoycon.ButtonA);
                Enum.TryParse(_b.Label,            out keyboardConfig.RightJoycon.ButtonB);
                Enum.TryParse(_x.Label,            out keyboardConfig.RightJoycon.ButtonX);
                Enum.TryParse(_y.Label,            out keyboardConfig.RightJoycon.ButtonY);
                Enum.TryParse(_plus.Label,         out keyboardConfig.RightJoycon.ButtonPlus);
                Enum.TryParse(_r.Label,            out keyboardConfig.RightJoycon.ButtonR);
                Enum.TryParse(_zR.Label,           out keyboardConfig.RightJoycon.ButtonZr);
                Enum.TryParse(_rSl.Label,          out keyboardConfig.RightJoycon.ButtonSl);
                Enum.TryParse(_rSr.Label,          out keyboardConfig.RightJoycon.ButtonSr);

                return keyboardConfig;
            }
            
            if (_inputDevice.ActiveId.StartsWith("controller"))
            {
                NpadController controllerConfig = new NpadController
                {
                    Index            = int.Parse(_inputDevice.ActiveId.Split("/")[1]),
                    ControllerType   = Enum.Parse<ControllerType>(_controllerType.ActiveId),
                    ControllerId     = _controllerId,
                    DeadzoneLeft     = (float)_controllerDeadzoneLeft.Value,
                    DeadzoneRight    = (float)_controllerDeadzoneRight.Value,
                    TriggerThreshold = (float)_controllerTriggerThreshold.Value,
                    LeftJoycon       = new NpadControllerLeft(),
                    RightJoycon      = new NpadControllerRight()
                };

                Enum.TryParse(_lStickX.Label,      out controllerConfig.LeftJoycon.StickX);
                Enum.TryParse(_lStickY.Label,      out controllerConfig.LeftJoycon.StickY);
                Enum.TryParse(_lStickButton.Label, out controllerConfig.LeftJoycon.StickButton);
                Enum.TryParse(_dpadUp.Label,       out controllerConfig.LeftJoycon.DPadUp);
                Enum.TryParse(_dpadDown.Label,     out controllerConfig.LeftJoycon.DPadDown);
                Enum.TryParse(_dpadLeft.Label,     out controllerConfig.LeftJoycon.DPadLeft);
                Enum.TryParse(_dpadRight.Label,    out controllerConfig.LeftJoycon.DPadRight);
                Enum.TryParse(_minus.Label,        out controllerConfig.LeftJoycon.ButtonMinus);
                Enum.TryParse(_l.Label,            out controllerConfig.LeftJoycon.ButtonL);
                Enum.TryParse(_zL.Label,           out controllerConfig.LeftJoycon.ButtonZl);
                Enum.TryParse(_lSl.Label,          out controllerConfig.LeftJoycon.ButtonSl);
                Enum.TryParse(_lSr.Label,          out controllerConfig.LeftJoycon.ButtonSr);

                Enum.TryParse(_rStickX.Label,      out controllerConfig.RightJoycon.StickX);
                Enum.TryParse(_rStickY.Label,      out controllerConfig.RightJoycon.StickY);
                Enum.TryParse(_rStickButton.Label, out controllerConfig.RightJoycon.StickButton);
                Enum.TryParse(_a.Label,            out controllerConfig.RightJoycon.ButtonA);
                Enum.TryParse(_b.Label,            out controllerConfig.RightJoycon.ButtonB);
                Enum.TryParse(_x.Label,            out controllerConfig.RightJoycon.ButtonX);
                Enum.TryParse(_y.Label,            out controllerConfig.RightJoycon.ButtonY);
                Enum.TryParse(_plus.Label,         out controllerConfig.RightJoycon.ButtonPlus);
                Enum.TryParse(_r.Label,            out controllerConfig.RightJoycon.ButtonR);
                Enum.TryParse(_zR.Label,           out controllerConfig.RightJoycon.ButtonZr);
                Enum.TryParse(_rSl.Label,          out controllerConfig.RightJoycon.ButtonSl);
                Enum.TryParse(_rSr.Label,          out controllerConfig.RightJoycon.ButtonSr);

                return controllerConfig;
            }

            GtkDialog.CreateErrorDialog("Some fields entered where invalid and therefore your config was not saved.");

            return null;
        }

        private static bool IsAnyKeyPressed(out Key pressedKey, int index = 0)
        {
            KeyboardState keyboardState = Keyboard.GetState(index);

            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (keyboardState.IsKeyDown((OpenTK.Input.Key)key))
                {
                    pressedKey = key;

                    return true;
                }
            }

            pressedKey = default;

            return false;
        }

        private static bool IsAnyButtonPressed(int index, double triggerThreshold, out ControllerInputId pressedButton)
        {
            JoystickState        joystickState        = Joystick.GetState(index);
            JoystickCapabilities joystickCapabilities = Joystick.GetCapabilities(index);

            //Buttons
            for (int i = 0; i != joystickCapabilities.ButtonCount; i++)
            {
                if (joystickState.IsButtonDown(i))
                {
                    Enum.TryParse($"Button{i}", out pressedButton);

                    return true;
                }
            }

            //Axis
            for (int i = 0; i != joystickCapabilities.AxisCount; i++)
            {
                if (joystickState.GetAxis(i) > triggerThreshold)
                {
                    Enum.TryParse($"Axis{i}", out pressedButton);

                    return true;
                }
            }

            //Hats
            for (int i = 0; i != joystickCapabilities.HatCount; i++)
            {
                JoystickHatState hatState = joystickState.GetHat((JoystickHat)i);
                string pos = null;

                if (hatState.IsUp)    pos = "Up";
                if (hatState.IsDown)  pos = "Down";
                if (hatState.IsLeft)  pos = "Left";
                if (hatState.IsRight) pos = "Right";
                if (pos == null)      continue;

                Enum.TryParse($"Hat{i}{pos}", out pressedButton);

                return true;
            }

            pressedButton = default;

            return false;
        }

        [GLib.ConnectBefore]
        private static void Key_Pressed(object sender, KeyPressEventArgs args)
        {
            _pressedKey = args.Event.Key;
        }

        //Events
        private void InputDevice_Changed(object sender, EventArgs args)
        {
            SetAvailableOptions();
        }

        private void Controller_Changed(object sender, EventArgs args)
        {
            SetControllerSpecificFields();
        }

        private void RefreshInputDevicesButton_Pressed(object sender, EventArgs args)
        {
            UpdateInputDeviceList();

            _refreshInputDevicesButton.SetStateFlags(0, true);
        }

        //TODO: Replace events with polling when the keyboard API is implemented in OpenTK.
        private void Button_Pressed(object sender, EventArgs args)
        {
            if (_isWaitingForInput)
            {
                return;
            }

            _isWaitingForInput = true;

            Thread inputThread = new Thread(() =>
            {
                Button button = (ToggleButton)sender;
                Application.Invoke(delegate { KeyPressEvent += Key_Pressed; });

                if (_inputDevice.ActiveId.StartsWith("keyboard"))
                {
                    while (!_pressedKey.HasValue)
                    {
                        if (Mouse.GetState().IsAnyButtonDown || _pressedKey == Gdk.Key.Escape)
                        {
                            Application.Invoke(delegate
                            {
                                button.SetStateFlags(0, true);
                                KeyPressEvent -= Key_Pressed;
                            });

                            _pressedKey        = null;
                            _isWaitingForInput = false;

                            return;
                        }
                    }

                    string key    = _pressedKey.ToString();
                    string capKey = key.First().ToString().ToUpper() + key.Substring(1);
                    _pressedKey   = null;

                    Application.Invoke(delegate
                    {
                        if (Enum.IsDefined(typeof(Key), capKey))
                        {
                            button.Label = capKey;
                        }
                        else if (GtkToOpenTkInput.ContainsKey(key))
                        {
                            button.Label = GtkToOpenTkInput[key];
                        }
                        else
                        {
                            button.Label = "Unknown";
                        }

                        button.SetStateFlags(0, true);
                        KeyPressEvent -= Key_Pressed;
                    });
                }
                else if (_inputDevice.ActiveId.StartsWith("controller"))
                {
                    ControllerInputId pressedButton;

                    int index = int.Parse(_inputDevice.ActiveId.Split("/")[1]);
                    while (!IsAnyButtonPressed(index, _controllerTriggerThreshold.Value, out pressedButton))
                    {
                        if (Mouse.GetState().IsAnyButtonDown || _pressedKey.HasValue)
                        {
                            Application.Invoke(delegate
                            {
                                button.SetStateFlags(0, true);
                                KeyPressEvent -= Key_Pressed;
                            });

                            _pressedKey        = null;
                            _isWaitingForInput = false;

                            return;
                        }
                    }

                    Application.Invoke(delegate
                    {
                        button.Label = pressedButton.ToString();
                        button.SetStateFlags(0, true);
                        KeyPressEvent -= Key_Pressed;
                    });
                }

                _isWaitingForInput = false;
            });
            inputThread.Name = "GUI.InputThread";
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        private void SetProfiles()
        {
            string basePath = System.IO.Path.Combine(new VirtualFileSystem().GetBasePath(), "profiles");
            
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            _profile.RemoveAll();
            _profile.Append("default", "Default");

            foreach (string profile in Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories))
            {
                _profile.Append(System.IO.Path.GetFileName(profile), System.IO.Path.GetFileNameWithoutExtension(profile));
            }
        }

        private void ProfileLoad_Activated(object sender, EventArgs args)
        {
            ((ToggleButton)sender).SetStateFlags(0, true);

            if (_inputDevice.ActiveId == "disabled" || _profile.ActiveId == null) return;

            object config = null;
            int    pos    = _profile.Active;

            if (_profile.ActiveId == "default")
            {
                if (_inputDevice.ActiveId.StartsWith("keyboard"))
                {
                    config = new NpadKeyboard
                    {
                        Index          = 0,
                        ControllerType = ControllerType.NpadPair,
                        ControllerId   = _controllerId,
                        LeftJoycon     = new NpadKeyboardLeft
                        {
                            StickUp     = Key.W,
                            StickDown   = Key.S,
                            StickLeft   = Key.A,
                            StickRight  = Key.D,
                            StickButton = Key.F,
                            DPadUp      = Key.Up,
                            DPadDown    = Key.Down,
                            DPadLeft    = Key.Left,
                            DPadRight   = Key.Right,
                            ButtonMinus = Key.Minus,
                            ButtonL     = Key.E,
                            ButtonZl    = Key.Q,
                            ButtonSl    = Key.Home,
                            ButtonSr    = Key.End
                        },
                        RightJoycon    = new NpadKeyboardRight
                        {
                            StickUp     = Key.I,
                            StickDown   = Key.K,
                            StickLeft   = Key.J,
                            StickRight  = Key.L,
                            StickButton = Key.H,
                            ButtonA     = Key.Z,
                            ButtonB     = Key.X,
                            ButtonX     = Key.C,
                            ButtonY     = Key.V,
                            ButtonPlus  = Key.Plus,
                            ButtonR     = Key.U,
                            ButtonZr    = Key.O,
                            ButtonSl    = Key.PageUp,
                            ButtonSr    = Key.PageDown
                        },
                        Hotkeys        = new KeyboardHotkeys
                        {
                            ToggleVsync = Key.Tab
                        }
                    };
                }
                else if (_inputDevice.ActiveId.StartsWith("controller"))
                {
                    config = new NpadController
                    {
                        Index            = 0,
                        ControllerType   = ControllerType.ProController,
                        ControllerId     = _controllerId,
                        DeadzoneLeft     = 0.05f,
                        DeadzoneRight    = 0.05f,
                        TriggerThreshold = 0.5f,
                        LeftJoycon       = new NpadControllerLeft
                        {
                            StickX      = ControllerInputId.Axis0,
                            StickY      = ControllerInputId.Axis1,
                            StickButton = ControllerInputId.Button8,
                            DPadUp      = ControllerInputId.Hat0Up,
                            DPadDown    = ControllerInputId.Hat0Down,
                            DPadLeft    = ControllerInputId.Hat0Left,
                            DPadRight   = ControllerInputId.Hat0Right,
                            ButtonMinus = ControllerInputId.Button6,
                            ButtonL     = ControllerInputId.Button4,
                            ButtonZl    = ControllerInputId.Axis2,
                            ButtonSl    = ControllerInputId.Button10,
                            ButtonSr    = ControllerInputId.Button11,
                        },
                        RightJoycon      = new NpadControllerRight
                        {
                            StickX      = ControllerInputId.Axis3,
                            StickY      = ControllerInputId.Axis4,
                            StickButton = ControllerInputId.Button9,
                            ButtonA     = ControllerInputId.Button1,
                            ButtonB     = ControllerInputId.Button0,
                            ButtonX     = ControllerInputId.Button3,
                            ButtonY     = ControllerInputId.Button2,
                            ButtonPlus  = ControllerInputId.Button7,
                            ButtonR     = ControllerInputId.Button5,
                            ButtonZr    = ControllerInputId.Axis5,
                            ButtonSl    = ControllerInputId.Button12,
                            ButtonSr    = ControllerInputId.Button13,
                        }
                    };
                }
            }
            else
            {
                string path = System.IO.Path.Combine(new VirtualFileSystem().GetBasePath(), "profiles", _profile.ActiveId);

                if (!File.Exists(path))
                {
                    if (pos >= 0)
                    {
                        _profile.Remove(pos);
                    }

                    return;
                }

                using (Stream stream = File.OpenRead(path))
                {
                    try
                    {
                        config = JsonSerializer.Deserialize<NpadController>(stream, _resolver);
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            config = JsonSerializer.Deserialize<NpadKeyboard>(stream, _resolver);
                        }
                        catch { }
                    }
                }
            }

            SetValues(config);
        }

        private void ProfileAdd_Activated(object sender, EventArgs args)
        {
            ((ToggleButton)sender).SetStateFlags(0, true);

            if (_inputDevice.ActiveId == "disabled") return;

            object        inputConfig   = GetValues();
            ProfileDialog profileDialog = new ProfileDialog();

            if (inputConfig == null) return;

            if (profileDialog.Run() == (int)ResponseType.Ok)
            {
                string path = System.IO.Path.Combine(new VirtualFileSystem().GetBasePath(), "profiles", profileDialog.FileName);
                byte[] data = JsonSerializer.Serialize(inputConfig, _resolver);

                File.WriteAllText(path, Encoding.UTF8.GetString(data, 0, data.Length).PrettyPrintJson());
            }

            profileDialog.Dispose();

            SetProfiles();
        }

        private void ProfileRemove_Activated(object sender, EventArgs args)
        {
            ((ToggleButton)sender).SetStateFlags(0, true);

            if (_inputDevice.ActiveId == "disabled" || _profile.ActiveId == "default" || _profile.ActiveId == null) return;
            
            string path = System.IO.Path.Combine(new VirtualFileSystem().GetBasePath(), "profiles", _profile.ActiveId);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            SetProfiles();
        }

        private void SaveToggle_Activated(object sender, EventArgs args)
        {
            if (_inputConfig == null)
            {
                ConfigurationState.Instance.Hid.InputConfig.Value.Add(GetValues());
            }
            else
            {
                int index = ConfigurationState.Instance.Hid.InputConfig.Value.IndexOf(_inputConfig);

                if (_inputDevice.ActiveId == "disabled")
                {
                    ConfigurationState.Instance.Hid.InputConfig.Value.Remove(_inputConfig);
                }
                else
                {
                    ConfigurationState.Instance.Hid.InputConfig.Value[index] = GetValues();
                }
            }

            Dispose();
        }

        private void CloseToggle_Activated(object sender, EventArgs args)
        {
            Dispose();
        }

        //TODO: Remove this dict when the keyboard API is implemented in OpenTK.
        public readonly Dictionary<string, string> GtkToOpenTkInput = new Dictionary<string, string>()
        {
            { "Alt_L",       "AltLeft"        },
            { "Alt_R",       "AltRight"       },
            { "Control_L",   "ControlLeft"    },
            { "Control_R",   "ControlRight"   },
            { "KP_0",        "Keypad0"        },
            { "KP_1",        "Keypad1"        },
            { "KP_2",        "Keypad2"        },
            { "KP_3",        "Keypad3"        },
            { "KP_4",        "Keypad4"        },
            { "KP_5",        "Keypad5"        },
            { "KP_6",        "Keypad6"        },
            { "KP_7",        "Keypad7"        },
            { "KP_8",        "Keypad8"        },
            { "KP_9",        "Keypad9"        },
            { "KP_Add",      "KeypadAdd"      },
            { "KP_Decimal",  "KeypadDecimal"  },
            { "KP_Divide",   "KeypadDivide"   },
            { "KP_Down",     "Down"           },
            { "KP_Enter",    "KeypadEnter"    },
            { "KP_Left",     "Left"           },
            { "KP_Multiply", "KeypadMultiply" },
            { "KP_Right",    "Right"          },
            { "KP_Subtract", "KeypadSubtract" },
            { "KP_Up",       "Up"             },
            { "Key_0",       "Number0"        },
            { "Key_1",       "Number1"        },
            { "Key_2",       "Number2"        },
            { "Key_3",       "Number3"        },
            { "Key_4",       "Number4"        },
            { "Key_5",       "Number5"        },
            { "Key_6",       "Number6"        },
            { "Key_7",       "Number7"        },
            { "Key_8",       "Number8"        },
            { "Key_9",       "Number9"        },
            { "Meta_L",      "WinLeft"        },
            { "Meta_R",      "WinRight"       },
            { "Next",        "PageDown"       },
            { "Num_Lock",    "NumLock"        },
            { "Page_Down",   "PageDown"       },
            { "Page_Up",     "PageUp"         },
            { "Prior",       "PageUp"         },
            { "Return",      "Enter"          },
            { "Shift_L",     "ShiftLeft"      },
            { "Shift_R",     "ShiftRight"     },
            { "VoidSymbol",  "CapsLock"       },
            { "backslash",   "BackSlash"      },
            { "bracketleft", "BracketLeft"    },
            { "bracketright","BracketRight"   },
            { "downarrow",   "Down"           },
            { "equal",       "Plus"           },
            { "leftarrow",   "Left"           },
            { "quoteleft",   "Grave"          },
            { "rightarrow",  "Right"          },
            { "uparrow",     "Up"             }
        };
    }
}