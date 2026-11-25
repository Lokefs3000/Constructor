using System;
using System.Collections.Generic;
using System.Text;
using Primary.Input;
using Primary.Input.Bindings;
using Primary.Input.Controls;

namespace VoxelizationDemo.Editor.Viewports
{
    internal sealed class MainViewport : Viewport
    {
        private readonly InputScheme _scheme;

        private readonly InputAction _forwardBackwardAction;
        private readonly InputAction _leftRightAction;
        private readonly InputAction _upDownAction;

        private readonly InputAction _speedupAction;

        internal MainViewport()
        {
            _scheme = new InputScheme() { Name = "Camera" };
            {
                _forwardBackwardAction = _scheme.AddAction();
                _forwardBackwardAction.Control = new AxisControl();

                _leftRightAction = _scheme.AddAction();
                _leftRightAction.Control = new AxisControl();

                _upDownAction = _scheme.AddAction();
                _upDownAction.Control = new AxisControl();

                _speedupAction = _scheme.AddAction();
                _speedupAction.Control = new ButtonControl();

                {
                    Composite1D ws = _forwardBackwardAction.AddBinding<Composite1D>();
                    ws.Name = "W/S";

                    Binding wBinding = ws.AddPositiveBinding();
                    wBinding.Name = "W";
                    wBinding.Path = "<Keyboard>/W";

                    Binding sBinding = ws.AddNegativeBinding();
                    sBinding.Name = "S";
                    sBinding.Path = "<Keyboard>/S";
                }

                {
                    Composite1D ad = _leftRightAction.AddBinding<Composite1D>();
                    ad.Name = "A/D";

                    Binding wBinding = ad.AddPositiveBinding();
                    wBinding.Name = "A";
                    wBinding.Path = "<Keyboard>/A";

                    Binding sBinding = ad.AddNegativeBinding();
                    sBinding.Name = "D";
                    sBinding.Path = "<Keyboard>/D";
                }

                {
                    Composite1D eq = _upDownAction.AddBinding<Composite1D>();
                    eq.Name = "E/Q";

                    Binding wBinding = eq.AddPositiveBinding();
                    wBinding.Name = "E";
                    wBinding.Path = "<Keyboard>/E";

                    Binding sBinding = eq.AddNegativeBinding();
                    sBinding.Name = "Q";
                    sBinding.Path = "<Keyboard>/Q";
                }

                {
                    Binding binding = _speedupAction.AddBinding<Binding>();
                    binding.Name = "Shift";
                    binding.Path = "<Keyboard>/LeftShift";
                }
            }
        }
    }
}
