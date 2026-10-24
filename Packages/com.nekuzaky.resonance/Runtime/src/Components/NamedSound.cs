using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [Serializable]
    public sealed class NamedSound
    {
        public string name = string.Empty;
        public SoundEvent sound;
    }
}
