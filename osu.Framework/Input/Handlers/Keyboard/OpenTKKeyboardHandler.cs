﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using OpenTK.Input;

namespace osu.Framework.Input.Handlers.Keyboard
{
    internal class OpenTKKeyboardHandler : InputHandler
    {
        public override bool IsActive => true;

        public override int Priority => 0;

        private OpenTK.Input.KeyboardState lastState;

        public override bool Initialize(GameHost host)
        {
            Enabled.ValueChanged += enabled =>
            {
                if (enabled)
                {
                    host.Window.KeyDown += handleKeyboardEvent;
                    host.Window.KeyUp += handleKeyboardEvent;
                }
                else
                {
                    host.Window.KeyDown -= handleKeyboardEvent;
                    host.Window.KeyUp -= handleKeyboardEvent;
                }
            };
            Enabled.TriggerChange();
            return true;
        }

        private void handleKeyboardEvent(object sender, KeyboardKeyEventArgs e)
        {
            var state = e.Keyboard;

            if (state.Equals(lastState))
                return;

            lastState = state;

            var newState = new TkKeyboardState(state);
            foreach (var key in Enum.GetValues(typeof(Key)).Cast<Key>())
            {
                PendingInputs.Enqueue(new KeyboardKeyInput { Key = key, IsPressed = newState.IsPressed(key) });
            }
            FrameStatistics.Increment(StatisticsCounterType.KeyEvents);
        }

        private class TkKeyboardState : KeyboardState
        {
            private static readonly IEnumerable<Key> all_keys = Enum.GetValues(typeof(Key)).Cast<Key>();

            public TkKeyboardState(OpenTK.Input.KeyboardState tkState)
            {
                if (tkState.IsAnyKeyDown)
                {
                    foreach (var key in all_keys)
                    {
                        if (tkState.IsKeyDown(key))
                        {
                            Keys.SetPressed(key, true);
                        }
                    }
                }
            }
        }
    }
}
