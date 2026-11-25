using Primary.Mathematics;
using SDL;
using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.Windows;
using static SDL.SDL3;

namespace Primary.Windowing
{
    public unsafe sealed class Display
    {
        private readonly SDL_DisplayID _id;
        private readonly SDL_PropertiesID _properties;
        private readonly string? _name;

        private bool _isPrimary;

        private bool _hasHdrEnabled;

        private Rect _boundaries;
        private Rect _usableBoundaries;

        internal Display(SDL_DisplayID id)
        {
            _id = id;
            _properties = SDL_GetDisplayProperties(id);
            _name = SDL_GetDisplayName(id);

            _isPrimary = SDL_GetPrimaryDisplay() == id;
            
            _hasHdrEnabled = SDL_GetBooleanProperty(_properties, SDL_PROP_DISPLAY_HDR_ENABLED_BOOLEAN, false);

            fixed (Rect* ptr = &_boundaries)
                SDL_GetDisplayBounds(id, (SDL_Rect*)ptr);
            fixed (Rect* ptr = &_usableBoundaries)
                SDL_GetDisplayBounds(id, (SDL_Rect*)ptr);
        }

        internal void FetchBoundaries(bool onlyUsable)
        {
            if (!onlyUsable)
            {
                fixed (Rect* ptr = &_boundaries)
                    SDL_GetDisplayBounds(_id, (SDL_Rect*)ptr);
            }

            fixed (Rect* ptr = &_usableBoundaries)
                SDL_GetDisplayBounds(_id, (SDL_Rect*)ptr);
        }

        internal void UpdateCachedData()
        {
            _isPrimary = SDL_GetPrimaryDisplay() == _id;
            _hasHdrEnabled = SDL_GetBooleanProperty(_properties, SDL_PROP_DISPLAY_HDR_ENABLED_BOOLEAN, false);
        }

        public Int2 FindCenter(Int2 size)
        {
            return _usableBoundaries.Position + _usableBoundaries.Size / 2 - size / 2;
        }

        public uint Id => (uint)_id;
        public string? Name => _name;

        public bool IsPrimary => _isPrimary;

        public bool HasHDREnabled => _hasHdrEnabled;

        public Rect Boundaries => _boundaries;
        public Rect UsableBoundaries => _usableBoundaries;
    }
}
