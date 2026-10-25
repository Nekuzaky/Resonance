using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class EffectsPanel
    {
        #region Public

        public readonly VisualElement Element;

        public AudioBus Bus { get; private set; }

        public EffectsPanel()
        {
            Element = Ui.Box("rs-effects");
            Element.RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += Rebuild);
            Element.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= Rebuild);
            Rebuild();
        }

        public void Show(AudioBus bus)
        {
            Bus = bus;
            Rebuild();
        }

        public void Refresh(ResonanceEngine engine)
        {
            if (Bus != null && Changed())
            {
                Rebuild();
                return;
            }
            foreach (var card in _cards)
                card.Refresh(engine);
        }

        #endregion


        #region Private and Protected

        private readonly List<EffectCard> _cards = new();

        private bool Changed()
        {
            if (Bus.effects.Count != _cards.Count)
                return true;
            for (var i = 0; i < _cards.Count; i++)
            {
                if (!ReferenceEquals(Bus.effects[i], _cards[i].Effect))
                    return true;
            }
            return false;
        }

        private void Rebuild()
        {
            Element.Clear();
            _cards.Clear();
            var header = Ui.Box("rs-effects-header");
            header.Add(Ui.GroupTitle(Glyphs.Filter, Bus != null ? $"Effects  ·  {Bus.name}" : "Effects"));
            header.Add(Ui.Spacer());
            if (Bus != null)
            {
                var add = Ui.TextButton(Glyphs.Add, "Add Effect", ShowMenu);
                add.SetEnabled(Bus.effects.Count < BusEffects.MaxEffects);
                header.Add(add);
            }
            Element.Add(header);
            if (Bus == null)
            {
                Element.Add(Ui.Help("Click the effects button of a bus to edit its effect chain: filters, reverb, delay, distortion and a compressor."));
                return;
            }
            Element.Add(Ui.Help(Help(Bus)));
            for (var i = 0; i < Bus.effects.Count && i < BusEffects.MaxEffects; i++)
            {
                if (Bus.effects[i] == null)
                    continue;
                var card = new EffectCard(Bus, i, Rebuild);
                _cards.Add(card);
                Element.Add(card.Element);
            }
        }

        private static string Help(AudioBus bus)
        {
            if (bus.effects.Count == 0)
                return "No effect yet. Effects run on every sound of this bus and of the buses under it, top to bottom, before the parent bus.";
            var output = bus.ResolveOutput() != null ? " The bus also goes to an Audio Mixer group, after these effects." : string.Empty;
            return "Top to bottom, before the parent bus. Filters and distortion keep this order; delay and reverb come after them." + output;
        }

        private void ShowMenu()
        {
            var menu = new GenericMenu();
            foreach (BusEffectType type in Enum.GetValues(typeof(BusEffectType)))
            {
                var chosen = type;
                menu.AddItem(new GUIContent(BusEffects.Label(type)), false, () => Add(chosen));
            }
            menu.ShowAsContext();
        }

        private void Add(BusEffectType type)
        {
            var bus = Bus;
            EffectEditing.Change(bus, "Add bus effect", () => bus.AddEffect(type));
            Rebuild();
        }

        #endregion
    }
}
